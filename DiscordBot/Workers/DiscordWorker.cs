using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.Webhook;
using Discord.WebSocket;
using Newtonsoft.Json;
using RustyWatcher.Configurations;
using RustyWatcher.Controllers;
using RustyWatcher.Extensions;
using RustyWatcher.Models.Rcon;
using RustyWatcher.Models.Steam;
using Serilog;

namespace RustyWatcher.Workers;

public class DiscordWorker
{
    #region Fields

    private static readonly ILogger _logger = Log.ForContext<DiscordWorker>(); 
    
    private readonly Connector _connector;
    private readonly ServerConfiguration _configuration;
    private readonly DiscordSocketClient _client;
    private readonly HttpClient _httpClient = new();
    
    private DiscordWebhookClient _webhookClient;

    #region Cached Stuff
    
    private readonly Emoji _tickEmoji = new("✅");
    private readonly List<ulong> _awaitingConfirmationMessages = new();
    private readonly Queue<Embed> _receivedMessageQueue = new();
    private readonly Dictionary<ulong, string> _steamAvatars = new();
    private string _region;
    private string _lastUpdateString;
    
    #endregion
    
    private const string FAILED_TO_SEND = "Unable to send to WebSocket (*make sure the server is connected*).";
    
    #endregion
    
    public DiscordWorker(Connector connector, ServerConfiguration configuration)
    {
        _connector = connector;
        _configuration = configuration;
        
        _client = new DiscordSocketClient(new DiscordSocketConfig()
        {
            GatewayIntents = GatewayIntents.DirectMessageTyping | GatewayIntents.DirectMessageReactions | GatewayIntents.DirectMessages | GatewayIntents.GuildMessageTyping 
                             | GatewayIntents.GuildMessageReactions | GatewayIntents.GuildMessages | GatewayIntents.GuildVoiceStates | GatewayIntents.GuildWebhooks 
                             | GatewayIntents.GuildIntegrations | GatewayIntents.GuildEmojis | GatewayIntents.GuildBans | GatewayIntents.Guilds | GatewayIntents.GuildMembers,
            ConnectionTimeout = -1,
            DefaultRetryMode = RetryMode.AlwaysRetry
        });

        InitializeWebhook();
        
        Task.Run(InitializeBot);
        Task.Run(ReceivedMessageDequeue);
    }

    #region Init

    private void InitializeWebhook()
    {
        try
        {
            _webhookClient = new DiscordWebhookClient(_configuration.Chatlog.WebhookUrl, new DiscordRestConfig());
        }
        catch (Exception e)
        {
            _logger.Error(e, "{0} Failed to Initialize Webhook", GetTag());
        }
    }
    
    private async Task InitializeBot()
    {
        try
        {
            await _client.LoginAsync(TokenType.Bot, _configuration.Discord.Token);
            await _client.StartAsync();
        }
        catch (Exception e)
        {
            _logger.Error(e, "{0} Failed to Initialize Bot", GetTag());
        }

        _client.Log += OnLog;
        _client.Ready += OnReady;
        _client.Disconnected += OnDisconnected;
        _client.MessageReceived += OnMessageReceived;
        _client.ReactionAdded += OnReactionAdded;
        
        await Task.Delay(-1);
    }
    
    #endregion

    #region Events

    private Task OnReactionAdded(Cacheable<IUserMessage, ulong> cacheableUser, Cacheable<IMessageChannel, ulong> cacheableChannel, SocketReaction socketReaction)
    {
        _ = ProcessReactionAdded(cacheableUser, cacheableChannel, socketReaction);
        return Task.CompletedTask;
    }

    private Task OnMessageReceived(SocketMessage message)
    {
        if (message.Author.IsBot)
        {
            return Task.CompletedTask;
        }

        var argPos = 0;
        var msg = message as SocketUserMessage;
        
        var prefixed = msg.HasStringPrefix(_configuration.Chatlog.CommandPrefix, ref argPos) ||
                       msg.HasMentionPrefix(_client.CurrentUser, ref argPos);

        _ = ProcessMessage(message, prefixed, argPos);
        return Task.CompletedTask;
    }

    private Task OnDisconnected(Exception arg)
    {
        return Task.CompletedTask;
    }

    private Task OnReady()
    {
        return Task.CompletedTask;
    }

    private Task OnLog(LogMessage message)
    {
        if (message.Exception != null)
            _logger.Error(message.Exception,  "{0} " + message.Message, GetTag());
        else
            _logger.Information( "{0} " + message.Message, GetTag());
        
        return Task.CompletedTask;
    }

    #endregion

    #region Methods

    private async Task ProcessMessage(SocketMessage message, bool prefixed, int argPos)
    {
        if (message.Channel.Id == _configuration.Chatlog.ChannelId && !prefixed)
        {
            if (!_configuration.Chatlog.ChatlogConfirmation)
            {
                var success = _connector.SendMessageRcon(message.Content, message.Author.Username, message.Author.Id);
                if (!success)
                    await message.Channel.SendMessageAsync(FAILED_TO_SEND);

                return;
            }

            // Queue up for once reaction is added
            await message.AddReactionAsync(_tickEmoji);
            _awaitingConfirmationMessages.Add(message.Id);
            return;
        }

        if (prefixed) // Command here
        {
            // Missing perms
            if (message.Author.HasAnyRole(_configuration.Chatlog.CanUseCommandsRoleIds))
            {
                await message.Channel.SendMessageAsync("Missing permissions to use commands.");
                return;
            }

            var success = _connector.SendCommandRcon(message.Content.TrimWhitespaces().Substring(argPos), message.Channel.Id, 
                (response, channelId) =>
            { 
                Task.Run(() => ProcessCommandRconCallback(response, channelId));
            });

            if (!success)
                await message.Channel.SendMessageAsync(FAILED_TO_SEND);
        }
    }

    private async Task ProcessReactionAdded(Cacheable<IUserMessage, ulong> cacheableUser, Cacheable<IMessageChannel, ulong> cacheableChannel, 
        SocketReaction socketReaction)
    {
        if (!_awaitingConfirmationMessages.Contains(cacheableUser.Id) || socketReaction.Emote.Name != _tickEmoji.Name)
            return;

        var message = await cacheableUser.GetOrDownloadAsync();
        if (message.Author.Id != socketReaction.UserId) // Not message author who is trying to approve it
            return;
        
        var success = _connector.SendMessageRcon(message.Content, message.Author.Username, message.Author.Id);
        if (!success)
        {
            await message.Channel.SendMessageAsync(FAILED_TO_SEND);
            return;
        }
        
        _awaitingConfirmationMessages.Remove(cacheableUser.Id);
    }

    public async Task ProcessCommandRconCallback(ResponsePacket response, ulong channelId)
    {
        var cleanContent = Regex.Replace(response.MessageContent, @"<.+?>", string.Empty);

        var embedBuilder = new EmbedBuilder();
        embedBuilder.WithAuthor("SERVER", _client.CurrentUser.GetAvatarUrl() ?? null);
        embedBuilder.WithColor(_configuration.Chatlog.ServerMessageColour.ToDiscordColor());
        embedBuilder.WithCurrentTimestamp();

        var channel = await _client.GetChannelAsync(channelId);
        if (channel is not ITextChannel textChannel)
        {
            _logger.Warning("{0} Failed to Process Rcon Package as Channel was not an ITextChannel", GetTag());
            return;
        }
        
        // Basic pagination
        if (cleanContent.Length > 2000)
        {
            var splitText = cleanContent.SplitInParts(2000);

            for (int i = 0; i < splitText.Length; i++)
            {
                embedBuilder.WithFooter(i + "/" + (splitText.Length - 1));

                var text = splitText[i];
                if (string.IsNullOrEmpty(text))
                    continue;

                embedBuilder.WithDescription(text);
                await textChannel.SendMessageAsync(null, false, embedBuilder.Build());
                await Task.Delay(1000);
            }

            return;
        }

        if (string.IsNullOrEmpty(cleanContent))
            return;

        embedBuilder.WithDescription(cleanContent);

        await textChannel.SendMessageAsync(null, false, embedBuilder.Build());
    }

    public async Task ProcessMessage(ResponseMessage msg)
    {
        if (!_configuration.Chatlog.Use)
            return;

        _logger.Debug("{0} New Message from {1}: {2}", GetTag(), msg.Username, msg.Content);
        
        try
        {
            var embedBuilder = new EmbedBuilder();
            var avatarLink = await GetAvatarLink(msg.UserID);
            embedBuilder.WithAuthor(msg.Username, avatarLink, msg.UserID != 0 ? $"https://steamcommunity.com/profiles/{msg.UserID}" : null);

            var message = msg.Content;

            //Keep format in case BetterChat is used
            if (message.Contains(":"))
            {
                var messageArray = message.Split(':');
                if (messageArray.Length > 1)
                {
                    if (messageArray[0].Contains(msg.Username))
                        message = messageArray[1];
                }                      
            }
                    
            embedBuilder.WithDescription(message);
                
            if (msg.Color.Length > 1)
            {
                var colour = msg.Color.Substring(1).GetCompleteHex();
                embedBuilder.WithColor(Convert.ToUInt32($"0x{colour}", 16));
            }

            if (msg.UserID == 0)
                embedBuilder.WithColor(_configuration.Chatlog.ServerMessageColour.ToDiscordColor());
            
            var channel = (RustChannelType)msg.Channel;
            var additionalInfo = (msg.UserID == 0 ? string.Empty : $" • {msg.UserID}");

            embedBuilder.WithFooter(channel + additionalInfo);
            embedBuilder.WithCurrentTimestamp();

            if (!_configuration.Chatlog.ShowTeamChat && (RustChannelType)msg.Channel == RustChannelType.Team) 
                return;

            _receivedMessageQueue.Enqueue(embedBuilder.Build());
        }
        catch (Exception e)
        {
            _logger.Error(e, "{0} DiscordWorker.ProcessMessage(ResponseMessage)", GetTag());
        }
    }

    public async Task ProcessMessage(ResponseServerInfo serverInfo)
    {
        try
        {
            //Status
            var players = string.Empty;
            var joining = string.Empty;
            var queued = string.Empty;
            
            if (serverInfo.Joining > 0 && !string.IsNullOrEmpty(_configuration.ServerInfo.JoiningFormat))
                joining = string.Format(_configuration.ServerInfo.JoiningFormat, serverInfo.Joining);
            
            if (serverInfo.Queued > 0 && !string.IsNullOrEmpty(_configuration.ServerInfo.QueueFormat))
                queued = string.Format(_configuration.ServerInfo.QueueFormat, serverInfo.Queued);

            // Include Joining in Player Count
            players = string.IsNullOrEmpty(_configuration.ServerInfo.JoiningFormat)
                ? (serverInfo.Players + serverInfo.Joining).ToString()
                : serverInfo.Players.ToString();
            
            var status = string.Format(_configuration.ServerInfo.PlayerStatus,
                players, serverInfo.MaxPlayers, joining, queued);
            
            await SetStatus(status);

            //Embed
            if (!_configuration.ServerInfo.ShowEmbed) 
                return;
            
            await UpdateOrCreateEmbed(serverInfo, status);
        }
        catch (Exception e)
        {
            _logger.Error(e, "{0} Failed DiscordWorker.ProcessMessage(ResponseServerInfo)", GetTag());
        }
    }
    
    public async Task SetStatus(string status, bool fail = false)
    {
        if (status == _lastUpdateString) 
            return;

        _lastUpdateString = status;

        if (_configuration.ServerInfo.ShowPlayerCountStatus)
        {
            if (fail) 
                await _client.SetStatusAsync(UserStatus.DoNotDisturb);
            else if (_client.Status != UserStatus.Online) 
                await _client.SetStatusAsync(UserStatus.Online);

            await _client.SetGameAsync(status, null, Enum.Parse<ActivityType>(_configuration.Discord.ActivityType.ToString()));
        }
        else
        {
            _lastUpdateString = _configuration.ServerInfo.StatusMessage;
            await _client.SetGameAsync(_configuration.ServerInfo.StatusMessage, null,
                Enum.Parse<ActivityType>(_configuration.Discord.ActivityType.ToString()));
        }
    }
    
    #endregion
    
    #region Helpers

    private async Task ReceivedMessageDequeue()
    {
        _logger.Debug("{0} Message dequeue process started!", GetTag());

        const int maxAmountOfMessagesPerEmbed = 10;

        while (true)
        {
            try
            {
                int messagesToDequeueAmount = _receivedMessageQueue.Count;
                if (_receivedMessageQueue.Count > maxAmountOfMessagesPerEmbed)
                    messagesToDequeueAmount = maxAmountOfMessagesPerEmbed;

                var embeds = new List<Embed>();
                for (int i = 0; i < messagesToDequeueAmount; i++)
                {
                    embeds.Add(_receivedMessageQueue.Dequeue());
                }

                if (embeds.Count < 1)
                    goto end;

                await _webhookClient?.SendMessageAsync(null, false, embeds);
                
                end:
                    await Task.Delay(1000);
            }
            catch (Exception e)
            {
                _logger.Error(e, "{0} DiscordWorker.ReceivedMessageDequeue()", GetTag());
            }
        }
    }
    
    private async Task UpdateOrCreateEmbed(ResponseServerInfo serverInfo, string status)
    {
        try
        {
            var serverInfoChannel = await _client.GetChannelAsync(_configuration.ServerInfo.ChannelId) as ITextChannel;
            if (serverInfoChannel == null)
                return;

            var messages = await serverInfoChannel.GetMessagesAsync(10).FlattenAsync();
            IUserMessage serverInfoMessage = null;
            
            foreach (var message in messages)
            {
                if (message.Author.Id != _client.CurrentUser.Id)
                    continue;
                
                serverInfoMessage = message as IUserMessage;
                break;
            }
            
            await GetRegion();

            #region Embed Generation

            var embedBuilder = new EmbedBuilder();
                           
            embedBuilder.WithTitle(string.Format(_configuration.Localization.EmbedTitle, _region, serverInfo.Hostname));
            embedBuilder.WithUrl(_configuration.ServerInfo.EmbedLink);
            embedBuilder.WithDescription(string.Format(_configuration.Localization.EmbedDescription, _configuration.Rcon.ServerIP, _configuration.Rcon.ServerPort));
            
            var fields = new List<EmbedFieldBuilder>
            {
                new()
                {
                    Name = _configuration.Localization.EmbedFieldPlayer.EmbedName,
                    Value = string.Format(_configuration.Localization.EmbedFieldPlayer.EmbedValue, status),
                    IsInline = _configuration.Localization.EmbedFieldPlayer.EmbedInline
                },
                new()
                {
                    Name = _configuration.Localization.EmbedFieldFPS.EmbedName,
                    Value = string.Format(_configuration.Localization.EmbedFieldFPS.EmbedValue, serverInfo.Framerate),
                    IsInline = _configuration.Localization.EmbedFieldFPS.EmbedInline
                },
                new()
                {
                    Name = _configuration.Localization.EmbedFieldEntities.EmbedName,
                    Value = string.Format(_configuration.Localization.EmbedFieldEntities.EmbedValue, serverInfo.EntityCount),
                    IsInline = _configuration.Localization.EmbedFieldEntities.EmbedInline
                },
                new()
                {
                    Name = _configuration.Localization.EmbedFieldGametime.EmbedName,
                    Value = string.Format(_configuration.Localization.EmbedFieldGametime.EmbedValue, serverInfo.GameTime),
                    IsInline = _configuration.Localization.EmbedFieldGametime.EmbedInline
                },                       
                new()
                {                        
                    Name = _configuration.Localization.EmbedFieldUptime.EmbedName,
                    Value = string.Format(_configuration.Localization.EmbedFieldUptime.EmbedValue, TimeSpan.FromSeconds(serverInfo.Uptime)),
                    IsInline = _configuration.Localization.EmbedFieldUptime.EmbedInline
                },                
            };

            if (!string.IsNullOrEmpty(_configuration.ServerInfo.WorldSeed) &&
                !string.IsNullOrEmpty(_configuration.ServerInfo.WorldSize))
            {
                fields.Add(new EmbedFieldBuilder()
                {
                    Name = _configuration.Localization.EmbedFieldMap.EmbedName,
                    Value = string.Format(_configuration.Localization.EmbedFieldMap.EmbedValue, 
                        $"https://rustmaps.com/map/{_configuration.ServerInfo.WorldSize}_{_configuration.ServerInfo.WorldSeed}"),
                    IsInline = _configuration.Localization.EmbedFieldUptime.EmbedInline
                });
            }
                             
            embedBuilder.WithFields(fields);

            var wipeDate = DateTime.ParseExact(serverInfo.SaveCreatedTime, "MM/dd/yyyy HH:mm:ss", CultureInfo.InvariantCulture);
            var footer = $"{wipeDate.ToString("MMM")} {wipeDate.Day} {wipeDate.Year}";
            var difference = DateTime.Now - wipeDate;
            if (difference.Days > 0) footer += $" ({difference.Days} days ago)";

            embedBuilder.WithFooter(new EmbedFooterBuilder()
            {
                Text = string.Format(_configuration.Localization.EmbedFooter, footer)
            });

            embedBuilder.WithColor(_configuration.ServerInfo.EmbedColor.Red, _configuration.ServerInfo.EmbedColor.Green,
                _configuration.ServerInfo.EmbedColor.Blue);
            
            #endregion
            
            // Create New
            if (serverInfoMessage == null)
            {
                await serverInfoChannel.SendMessageAsync(null, false, embedBuilder.Build());
            }
            else // Edit existing
            {
                await serverInfoMessage.ModifyAsync((properties) =>
                {
                    properties.Embed = embedBuilder.Build();
                });
            }
        }
        catch (Exception e)
        {
            _logger.Error(e, "{0} Failed DiscordWorker.UpdateOrCreateEmbed()", GetTag());
        }
    }
    
    private string GetTag()
    {
        return "[" + _connector.GetName() + "-DISCORD]";
    }

    private async Task GetRegion()
    {
        if (!_configuration.ServerInfo.GetServerRegion) 
            return;

        if (!string.IsNullOrEmpty(_region)) 
            return;

        try
        {
            var response = await _httpClient.GetAsync($"https://ipinfo.io/{_configuration.Rcon.ServerIP}/country");
            if (!response.IsSuccessStatusCode)
            {
                _logger.Debug("{0} Region request failed : Code {1}", GetTag(), response.StatusCode);
                return;
            }

            var region = await response.Content.ReadAsStringAsync();
            var final = Regex.Replace(region.ToLower(), @"\t|\n|\r", string.Empty);

            _logger.Debug("{0} Received region : {1}", GetTag(), final);
            _region = $":flag_{final}:";
        }
        catch (Exception e)
        {
            _logger.Error(e, "{0} Failed DiscordWorker.GetRegion()", GetTag());
        }
    }
    
    private async Task<string> GetAvatarLink(ulong userId)
    {
        var avatarLink = string.Empty;

        if (userId == 0)
            return avatarLink;

        if (_steamAvatars.TryGetValue(userId, out avatarLink)) 
            return avatarLink;
        
        try
        {
            if (string.IsNullOrEmpty(Configuration.Instance.SteamAPIKey))
                return avatarLink;

            var response = await _httpClient.GetStringAsync(
                $"https://api.steampowered.com/ISteamUser/GetPlayerSummaries/v0002/?key={Configuration.Instance.SteamAPIKey}&steamids={userId}");

            var json = JsonConvert.DeserializeObject<SteamRootObject>(response);
            _steamAvatars.Add(userId, avatarLink = json.Results.Players[0].Avatar);
        }
        catch (Exception e)
        {
            avatarLink = string.Empty;
            _logger.Error(e, GetTag() + " DiscordWorker.GetAvatarLink()");
        }

        return avatarLink;
    }
    
    #endregion
}