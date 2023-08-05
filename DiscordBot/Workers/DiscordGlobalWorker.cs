using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using RustyWatcher.Configurations;
using RustyWatcher.Controllers;
using RustyWatcher.Extensions;
using RustyWatcher.Models.Rcon;
using Serilog;

namespace RustyWatcher.Workers;

public class DiscordGlobalWorker
{
    #region Fields

    private static readonly ILogger _logger = Log.ForContext<DiscordGlobalWorker>(); 
    
    private readonly List<Connector> _connectors;
    private readonly ServerGlobalConfiguration _configuration;
    private readonly DiscordSocketClient _client;
    
    #region Cached Stuff
    
    private string _lastUpdateString;
    private readonly Dictionary<int, CommandConfiguration> _identifierToCommand = new();
    private readonly Dictionary<ulong, List<ResponsePacketBundle>> _responsePacketBundles = new ();
    
    #endregion
    
    private const string FAILED_TO_SEND = "⚡ Unable to send to WebSocket (*make sure the server is connected*).";
    
    private const string SLASH_RUN = "run-all";
    private const string SLASH_RECONNECT = "reconnect-all";
    
    #endregion
    
    public DiscordGlobalWorker(List<Connector> connectors, ServerGlobalConfiguration configuration)
    {
        _connectors = connectors;
        _configuration = configuration;
        
        _client = new DiscordSocketClient(new DiscordSocketConfig()
        {
            GatewayIntents = GatewayIntents.DirectMessageTyping | GatewayIntents.DirectMessageReactions | GatewayIntents.DirectMessages | GatewayIntents.GuildMessageTyping 
                             | GatewayIntents.GuildMessageReactions | GatewayIntents.GuildMessages | GatewayIntents.GuildVoiceStates | GatewayIntents.GuildWebhooks 
                             | GatewayIntents.GuildIntegrations | GatewayIntents.GuildEmojis | GatewayIntents.GuildBans | GatewayIntents.Guilds | GatewayIntents.GuildMembers,
            ConnectionTimeout = -1,
            DefaultRetryMode = RetryMode.RetryTimeouts | RetryMode.Retry502
        });
        
        Task.Run(InitializeBot);
    }

    #region Init

    private async Task InitializeBot()
    {
        try
        {
            await _client.LoginAsync(TokenType.Bot, _configuration.Discord.Token);
            await _client.StartAsync();

            Task.Run(StatusLoop);
        }
        catch (Exception e)
        {
            _logger.Error(e, "{0} Failed to Initialize Bot", GetTag());
        }

        _client.Log += OnLog;
        _client.Ready += OnReady;
        _client.SlashCommandExecuted += OnSlashCommandExecuted;
        
        await Task.Delay(-1);
    }

    #endregion
    
    #region Events
    
    private Task OnReady()
    {
        Task.Run(SetupSlashCommands);
        
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

    private async Task OnSlashCommandExecuted(SocketSlashCommand command)
    {
        const string awaitingResponse = "⚙️ *Awaiting Response...*";
        const string noPermissionsResponse = "⛔ **NO PERMISSIONS.**";
        
        switch (command.Data.Name)
        {
            case SLASH_RUN:
            {
                var commandType = (long)command.Data.Options.First().Value;
                if (!_identifierToCommand.TryGetValue((int)commandType, out var commandConfiguration))
                    return;

                // Confirm if user has permissions to use
                if (commandConfiguration.RolesIds != null && !command.User.HasAnyRole(commandConfiguration.RolesIds))
                {
                    await command.RespondAsync(noPermissionsResponse, null, false, true);
                    return;
                }
                
                var commandArguments = (string)command.Data.Options.ElementAt(1).Value;
                commandArguments = string.IsNullOrEmpty(commandConfiguration.Name) ? commandArguments : commandConfiguration.Name + " " + commandArguments;
                
                _logger.Debug("{0} Slash Command Executed '{1}'", GetTag(), commandArguments);
                
                var oneSucceeded = false;
                foreach (var connector in _connectors)
                {
                    var success = connector.SendCommandRcon(commandArguments,
                        (response) =>
                        {
                            Task.Run(() => PreProcessGlobalCommandRconCallback(connector, command.Id, response, command));
                        });

                    if (success)
                        oneSucceeded = true;
                }
                
                if (!oneSucceeded)
                    await command.RespondAsync(FAILED_TO_SEND);
                else
                    await command.RespondAsync(awaitingResponse);
                
                break;
            }
            case SLASH_RECONNECT:
            {
                // Confirm if user has permissions to use
                if (!command.User.HasAnyRole(_configuration.Discord.AdministrativeCommandRoleIds))
                {
                    await command.RespondAsync(noPermissionsResponse, null, false, true);
                    return;
                }
                
                await command.RespondAsync("✅️ **RECONNECTING ALL**");

                foreach (var connector in _connectors)
                {
                    Task.Run(() => connector.ReconnectRcon());
                }
                break;
            }
        }
    }
    
    #endregion

    #region Methods
    
    private void PreProcessGlobalCommandRconCallback(Connector connector, ulong identifier, ResponsePacket? response, SocketSlashCommand command)
    {
        if (!_responsePacketBundles.TryGetValue(identifier, out var packets))
            _responsePacketBundles[identifier] = packets = new List<ResponsePacketBundle>();
        
        packets.Add(new ResponsePacketBundle(connector, response));

        if (packets.Count < _connectors.Count) // Keep on waiting
            return;

        // received all packets
        _responsePacketBundles.Remove(identifier);
        
        Task.Run(() => ProcessGlobalCommandRconCallback(packets, command));
    }
    
    private async Task ProcessGlobalCommandRconCallback(List<ResponsePacketBundle> responses, SocketSlashCommand command)
    {
        const string timedOutRcon = "```⚡ No response from the Server.```";
        const string doneString = "✅️ **DONE**";

        var embeds = new Embed[responses.Count];
        for (var i = 0; i < responses.Count; i++)
        {
            var embedBuilder = new EmbedBuilder();
            var response = responses[i];
            
            embedBuilder.WithAuthor($"SERVER ({response.Connector.GetName()})", response.Connector.GetDiscordAvatarUrl());
            embedBuilder.WithColor(response.Connector.GetDiscordMessageColor());
            embedBuilder.WithCustomFooter();
            
            if (response.Packet == null)
            {
                embedBuilder.WithDescription(timedOutRcon);
            }
            else
            {
                var cleanContent = Regex.Replace(response.Packet.MessageContent, @"<.+?>", string.Empty);
                
                // Basic pagination (skip this for now, and only show first part, later on maybe make multi response with interaction buttons etc)
                if (cleanContent.Length > 2000)
                {
                    cleanContent = cleanContent.SplitInParts(2000)[0];
                }

                if (string.IsNullOrEmpty(cleanContent))
                    return;

                embedBuilder.WithDescription("```" + cleanContent + "```");
            }
            
            embeds[i] = embedBuilder.Build();
        }

        await command.ModifyOriginalResponseAsync(properties =>
        {
            properties.Content = doneString;
            properties.Embeds = embeds;
        });
    }
    
    #endregion
    
    #region Helpers

    private async Task SetupSlashCommands()
    {
        var typeBuilder = new SlashCommandOptionBuilder()
            .WithName("type")
            .WithDescription("Declare the type of command you want to run")
            .WithRequired(true)
            .WithType(ApplicationCommandOptionType.Integer);

        var currentIdentifier = 1;
        foreach (var customCommand in _configuration.Discord.Commands)
        {
            typeBuilder.AddChoice(customCommand.DisplayName, currentIdentifier);
            _identifierToCommand[currentIdentifier] = customCommand;
            
            currentIdentifier++;
        }
        
        var runSlashCommandBuilder = new SlashCommandBuilder()
            .WithName(SLASH_RUN)
            .WithDescription("Run a Command on the all Servers.")
            .AddOption(typeBuilder)
            .AddOption("arguments", ApplicationCommandOptionType.String, "The arguments for the Command", true);
        
        var reconnectSlashCommandBuilder = new SlashCommandBuilder()
            .WithName(SLASH_RECONNECT)
            .WithDescription("Reconnect all Servers to the WebSocket.");
        
        try
        {
            await _client.Rest.CreateGuildCommand(runSlashCommandBuilder.Build(), _configuration.Discord.GuildId);
            await _client.Rest.CreateGuildCommand(reconnectSlashCommandBuilder.Build(), _configuration.Discord.GuildId);
            
            _logger.Debug("{0} Registered Slash Commands for Guild {1}", GetTag(), _configuration.Discord.GuildId);
        }
        catch (Exception e)
        {
            _logger.Error(e, "{0} Failed DiscordGlobalWorker.SetupSlashCommands()");
        }
    }
    
    private string GetTag()
    {
        return "[GLOBAL-DISCORD]";
    }
    
    private async Task SetStatus(string status, bool fail = false)
    {
        if (status == _lastUpdateString) 
            return;

        _lastUpdateString = status;
        
        if (fail)
            await _client.SetStatusAsync(UserStatus.DoNotDisturb);
        else if (_client.Status != UserStatus.Online) 
            await _client.SetStatusAsync(UserStatus.Online);

        await _client.SetGameAsync(status, null, Enum.Parse<ActivityType>(_configuration.Discord.ActivityType.ToString()));
    }

    private async Task StatusLoop()
    {
        while (true)
        {
            await Task.Delay(Configuration.Instance.UpdateDelay * 1000);
            
            var maxPlayers = 0;
            var players = 0;
            var joining = 0;
            var queued = 0;

            foreach (var connector in _connectors)
            {
                var serverInfo = connector.GetLastServerInfo();
                if (serverInfo == null)
                    continue;
                
                maxPlayers += serverInfo.MaxPlayers;
                players += serverInfo.Players;
                joining += serverInfo.Joining;
                queued += serverInfo.Queued;
            }

            if (maxPlayers == 0 && players == 0) // skip if all serverinfo failed
                continue;
            
            //Status
            var playersString = string.Empty;
            var joiningString = string.Empty;
            var queuedString = string.Empty;
        
            if (joining > 0 && !string.IsNullOrEmpty(_configuration.ServerInfoGlobal.JoiningFormat))
                joiningString = string.Format(_configuration.ServerInfoGlobal.JoiningFormat, joining);
            
            if (queued > 0 && !string.IsNullOrEmpty(_configuration.ServerInfoGlobal.QueueFormat))
                queuedString = string.Format(_configuration.ServerInfoGlobal.QueueFormat, queued);

            // Include Joining in Player Count
            playersString = string.IsNullOrEmpty(_configuration.ServerInfoGlobal.JoiningFormat)
                ? (players + joining).ToString()
                : players.ToString();

            _ = SetStatus(string.Format(_configuration.ServerInfoGlobal.PlayerStatus,
                playersString, maxPlayers, joiningString, queuedString));
        }
    }
    
    #endregion
    
    private class ResponsePacketBundle
    {
        public readonly Connector Connector;
        public readonly ResponsePacket? Packet;

        public ResponsePacketBundle(Connector connector, ResponsePacket? packet)
        {
            Connector = connector;
            Packet = packet;
        }
    }
}