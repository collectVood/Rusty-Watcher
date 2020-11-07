using System;
using System.Net.Http;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Globalization;
using System.Text.RegularExpressions;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using WebSocketSharp;
using RustyWatcher.Data;
using RustyWatcher.Rcon;
using RustyWatcher.Steam;
using System.Linq;
using Discord.Rest;

namespace RustyWatcher
{
    public class Websocket
    {
        #region Fields

        #region Discord related

        public DiscordSocketClient Client => Bot.Client;

        public IGuild CurrentGuild => Bot.CurrentGuild;

        public ITextChannel ServerInfoEmbedChannel = null;

        public IUserMessage ServerInfoMessage = null;

        public readonly RequestOptions RequestMode = new RequestOptions() { RetryMode = RetryMode.AlwaysRetry, Timeout = 10 };

        #endregion

        public Bot Bot;

        public WebSocket WS;

        public ServerDataFile Data => Bot.Data;

        public bool IsConnected => WS.ReadyState == WebSocketState.Open && WS.ReadyState != WebSocketState.Closed;
        
        private static readonly HttpClient _httpClient = new HttpClient();

        #endregion

        #region Cached Data

        private string _region = string.Empty;

        private string _serverSeed = string.Empty;

        private string _serverWorldSize = string.Empty;

        private Dictionary<ulong, string> _steamAvatars = new Dictionary<ulong, string>();

        private Queue<Embed> _messageQueue = new Queue<Embed>();

        private HashSet<ulong> _chatLogAwaitingMessages = new HashSet<ulong>();

        #endregion

        #region Callback

        public Dictionary<int, Action<ResponsePacket>> Callbacks = new Dictionary<int, Action<ResponsePacket>>();

        private int _currentIdentifier = 1337;

        #endregion

        #region Constructor

        public Websocket(Bot bot)
        {
            Bot = bot;

            Bot.Websocket = this;
        }

        #endregion

        #region Init

        public async Task StartAsync()
        {
            WS = new WebSocket($"ws://{Data.Rcon.ServerIP}:{Data.Rcon.RconPort}/{Data.Rcon.RconPW}");

            WS.Log.Output = (_, __) => { };

            Log.Info("Websocket: Connecting...", Client);

            try
            {
                WS.OnOpen += async (sender, e) => await OnOpen(sender, e);
                WS.OnClose += async (sender, e) => await OnClose(sender, e);
                WS.OnMessage += async (sender, e) => await OnMessage(sender, e);
                WS.OnError += async (sender, e) => await OnError(sender, e);

                WS.Connect();

                _ = Task.Run(ServerInfo);

                if (Data.Chatlog.Use)
                    _ = Task.Run(MessageDequeue);
            }
            catch (Exception e)
            {
                Log.Error(e, Client);
            } 

            await Task.Delay(-1);
        }

        #endregion

        #region Methods

        public async Task ProcessServerInfo(ResponseServerInfo serverInfo)
        {
            try
            {
                //Status
                string players = string.Empty;
                string queued = string.Empty;
                if (serverInfo.Queued > 0)
                {
                    queued = $"({serverInfo.Queued} Queued)";
                }
                if (serverInfo.Players + serverInfo.Joining > serverInfo.MaxPlayers)
                {
                    players = serverInfo.MaxPlayers.ToString();
                }
                else
                {
                    players = (serverInfo.Players + serverInfo.Joining).ToString();
                }
                    
                await Bot.SetStatus(string.Format(Data.Localization.PlayerStatus, 
                    players, serverInfo.MaxPlayers, queued));

                //Embed
                if (!Data.Serverinfo.ShowEmbed) 
                    return;

                await UpdateOrCreateEmbed(serverInfo, string.Format(Data.Localization.PlayerStatus,
                    players, serverInfo.MaxPlayers, queued));
            }
            catch (Exception e)
            {
                Log.Error(e, Client);
            }
        }

        public async Task ProcessMessage(ResponseMessage msg)
        {
            if (!Data.Chatlog.Use)
                return;

            try
            {
                var embedBuilder = new EmbedBuilder();

                string avatarLink = await GetAvatarlink(msg.UserID);

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
                    string colour = Bot.GetCompleteHex(msg.Color.Substring(1));
                    embedBuilder.WithColor(Convert.ToUInt32($"0x{colour}", 16));
                }

                if (msg.UserID == 0)
                {
                    embedBuilder.WithColor(Data.Chatlog.ServerMessageColour.ToColor());
                }

                var channel = (RustChannelType)msg.Channel;
                string additionalInfo = (msg.UserID == 0 ? string.Empty : $" • {msg.UserID}");

                embedBuilder.WithFooter(channel.ToString() + additionalInfo);
                embedBuilder.WithCurrentTimestamp();

                if (!Data.Chatlog.ShowTeamChat && (RustChannelType)msg.Channel == RustChannelType.Team) 
                    return;

                _messageQueue.Enqueue(embedBuilder.Build());
            }
            catch (Exception e)
            {
                Log.Error(e, Client);
            }
        }

        public async Task UpdateOrCreateEmbed(ResponseServerInfo serverInfo, string status)
        {
            try
            {
                if (CurrentGuild == null)
                    return;

                if (ServerInfoMessage == null)
                {
                    if (ServerInfoEmbedChannel == null)
                    {
                        ServerInfoEmbedChannel = await CurrentGuild.GetTextChannelAsync(Data.Serverinfo.ChannelId);
                        if (ServerInfoEmbedChannel == null)
                        {
                            Log.Warning("Unable to find the server info channel!");
                            return;
                        }
                    }
                    var messages = await ServerInfoEmbedChannel.GetMessagesAsync(10).FlattenAsync();
                    foreach (var message in messages)
                    {
                        if (message.Author.Id == Client.CurrentUser.Id)
                            ServerInfoMessage = message as IUserMessage;
                    }                   
                }

                await GetRegion();
                
                var embedBuilder = new EmbedBuilder();
                               
                embedBuilder.WithTitle(string.Format(Data.Localization.EmbedTitle, _region, serverInfo.Hostname));
                embedBuilder.WithUrl(Data.Serverinfo.EmbedLink);
                embedBuilder.WithDescription(string.Format(Data.Localization.EmbedDescription, Data.Rcon.ServerIP, Data.Rcon.ServerPort));
                
                var fields = new List<EmbedFieldBuilder>
                {
                    new EmbedFieldBuilder()
                    {
                        Name = Data.Localization.EmbedFieldPlayer.EmbedName,
                        Value = string.Format(Data.Localization.EmbedFieldPlayer.EmbedValue, status),
                        IsInline = Data.Localization.EmbedFieldPlayer.EmbedInline
                    },
                    new EmbedFieldBuilder()
                    {
                        Name = Data.Localization.EmbedFieldFPS.EmbedName,
                        Value = string.Format(Data.Localization.EmbedFieldFPS.EmbedValue, serverInfo.Framerate),
                        IsInline = Data.Localization.EmbedFieldFPS.EmbedInline
                    },
                    new EmbedFieldBuilder()
                    {
                        Name = Data.Localization.EmbedFieldEntities.EmbedName,
                        Value = string.Format(Data.Localization.EmbedFieldEntities.EmbedValue, serverInfo.EntityCount),
                        IsInline = Data.Localization.EmbedFieldEntities.EmbedInline
                    },
                    new EmbedFieldBuilder()
                    {
                        Name = Data.Localization.EmbedFieldGametime.EmbedName,
                        Value = string.Format(Data.Localization.EmbedFieldGametime.EmbedValue, serverInfo.GameTime),
                        IsInline = Data.Localization.EmbedFieldGametime.EmbedInline
                    },                       
                    new EmbedFieldBuilder()
                    {                        
                        Name = Data.Localization.EmbedFieldUptime.EmbedName,
                        Value = string.Format(Data.Localization.EmbedFieldUptime.EmbedValue, TimeSpan.FromSeconds(serverInfo.Uptime)),
                        IsInline = Data.Localization.EmbedFieldUptime.EmbedInline
                    },                
                };

                if (!string.IsNullOrEmpty(_serverSeed) &&
                    !string.IsNullOrEmpty(_serverWorldSize))
                {
                    string mapLink = string.Empty;
                    if (serverInfo.Map == "Procedural Map")
                        mapLink = $"http://playrust.io/map/?Procedural%20Map_{_serverWorldSize}_{_serverSeed}"; 
                    else if (serverInfo.Map == "Barren")
                        mapLink = $"http://playrust.io/map/?Barren_{_serverWorldSize}_{_serverSeed}";

                    if (!string.IsNullOrEmpty(mapLink))
                    {
                        fields.Add(new EmbedFieldBuilder()
                        {
                            Name = Data.Localization.EmbedFieldMap.EmbedName,
                            Value = string.Format(Data.Localization.EmbedFieldMap.EmbedValue, mapLink),
                            IsInline = Data.Localization.EmbedFieldUptime.EmbedInline
                        });
                    }
                }
                                 
                embedBuilder.WithFields(fields);

                var wipeDate = DateTime.ParseExact(serverInfo.SaveCreatedTime, "MM/dd/yyyy HH:mm:ss", CultureInfo.InvariantCulture);
                string footer = $"{wipeDate.ToString("MMM")} {wipeDate.Day} {wipeDate.Year}";
                var difference = DateTime.Now - wipeDate;
                if (difference.Days > 0) footer += $" ({difference.Days} days ago)";

                embedBuilder.WithFooter(new EmbedFooterBuilder()
                {
                    Text = string.Format(Data.Localization.EmbedFooter, footer)
                });

                embedBuilder.WithColor(Data.Serverinfo.EmbedColor.Red, Data.Serverinfo.EmbedColor.Green, Data.Serverinfo.EmbedColor.Blue);

                if (ServerInfoMessage == null)
                {
                    //Create message
                    ServerInfoMessage = await ServerInfoEmbedChannel.SendMessageAsync(null, false, embedBuilder.Build(), RequestMode);
                }
                else
                {
                    //Edit message
                    await ServerInfoMessage.ModifyAsync((properties) =>
                    {
                        properties.Embed = embedBuilder.Build();
                    });
                }
            }
            catch (Exception e)
            {
                Log.Error(e, Client);
            }
        }

        public async Task OnDiscordCommand(SocketMessage msg, string cmd)
        {
            var guildUser = msg.Author as IGuildUser;
            var allUserRoles = new List<ulong>(guildUser.RoleIds);

            if (!Bot.HasEqualValue<ulong>(Data.Chatlog.CanUseCommandsRoleIds, allUserRoles))
            {
                await msg.Channel.SendMessageAsync("Missing permissions to use commands.");
                return;
            }

            if (!IsConnected)
            {
                await msg.Channel.SendMessageAsync("Server was unable to be reached.");
                return;
            }

            //var identifier = CurrentIdentifier++;
            SendDiscordCommand(cmd, msg);
        }

        public async Task OnDiscordMessage(SocketMessage msg)
        {
            if (!Data.Chatlog.ChatlogConfirmation)
            {
                if (!IsConnected)
                {
                    await msg.Channel.SendMessageAsync("Server was unable to be reached.");
                    return;
                }

                SendDiscordMessage(msg.Author.Id, msg.Content,
                    msg.Author.Username);
                return;
            }

            var rMsg = (RestUserMessage)await msg.Channel.GetMessageAsync(msg.Id);
            await rMsg.AddReactionAsync(new Emoji("✅"));

            _chatLogAwaitingMessages.Add(msg.Id);
        }

        public async Task OnReactionAdded(SocketReaction reaction, IUserMessage msg)
        {
            if (!_chatLogAwaitingMessages.Contains(msg.Id)) 
                return;

            if (!IsConnected)
            {
                await msg.Channel.SendMessageAsync("Server was unable to be reached.");
                return;
            }
            
            _chatLogAwaitingMessages.Remove(msg.Id);

            SendDiscordMessage(msg.Author.Id, msg.Content,
                msg.Author.Username);

            Log.Debug(msg.Content, Client);
        }

        #endregion

        #region Helpers

        public async Task GetRegion()
        {
            if (!Data.Serverinfo.GetServerRegion) 
                return;

            if (!string.IsNullOrEmpty(_region)) 
                return;

            try
            {
                var response = await _httpClient.GetAsync($"https://ipinfo.io/{Data.Rcon.ServerIP}/country");
                if (!response.IsSuccessStatusCode)
                {
                    Log.Debug("Region request failed : Code " + response.StatusCode, Client);
                    return;
                }

                string region = await response.Content.ReadAsStringAsync();
                string final = Regex.Replace(region.ToLower(), @"\t|\n|\r", string.Empty);

                Log.Debug("Received region : " + final, Client);
                _region = $":flag_{final}:";
            }
            catch (Exception e)
            {
                Log.Error(e, Client);
            }
        }

        public async Task<string> GetAvatarlink(ulong userId)
        {
            string avatarLink = string.Empty;

            if (userId == 0)
                return avatarLink;

            if (!_steamAvatars.TryGetValue(userId, out avatarLink))
            {
                try
                {
                    if (string.IsNullOrEmpty(Program.Data.SteamAPIKey))
                        return avatarLink;

                    var response = await _httpClient.GetStringAsync(
                        $"http://api.steampowered.com/ISteamUser/GetPlayerSummaries/v0002/?key={Program.Data.SteamAPIKey}&steamids={userId}");

                    var json = JsonConvert.DeserializeObject<SteamRootObject>(response);
                    _steamAvatars.Add(userId, avatarLink = json.Results.Players[0].Avatar);
                }
                catch (Exception e)
                {
                    avatarLink = string.Empty;
                    Log.Error(e, Client);
                }
            }

            return avatarLink;
        }

        public void SendMessage(string message, int identifier)
        {
            Packet packet = new Packet(message, identifier);
            var msg = JsonConvert.SerializeObject(packet);

            if (!IsConnected)
            {
                Log.Debug("Trying to send message but websocket not connected!", Client);
                return;
            }
                
            WS.Send(msg);
        }
      
        public async Task TryReconnect()
        {
            if (!IsConnected)
            {
                await Task.Delay(Program.Data.ReconnectDelay * 1000);
                Log.Info("Websocket: Reconnecting...", Client);
                try
                {
                    if (!IsConnected) 
                        WS.Connect(); 
                }
                catch { }
            }
        }

        public async Task ServerInfo()
        {
            while (true)
            {
                if (IsConnected) 
                    SendMessage("serverinfo", (int)PacketIdentifier.ServerInfo);
                else 
                    await Bot.SetStatus("Offline");

                await Task.Delay(Program.Data.DiscordDelay * 1000);
            }
        }

        private readonly int _maxAmountOfMessagesPerEmbed = 10;

        public async Task MessageDequeue()
        {
            Log.Debug("Message dequeue process started!", Client);

            while (true)
            {
                try
                {
                    int messagesToDequeueAmount = _messageQueue.Count;
                    if (_messageQueue.Count > _maxAmountOfMessagesPerEmbed)
                        messagesToDequeueAmount = _maxAmountOfMessagesPerEmbed;

                    var embeds = new List<Embed>();
                    for (int i = 0; i < messagesToDequeueAmount; i++)
                    {
                        embeds.Add(_messageQueue.Dequeue());
                    }

                    if (embeds.Count < 1)
                        goto end;

                    await Bot.ChatlogWebhookClient.SendMessageAsync(null, false, embeds);

                    end:
                    await Task.Delay(1000);
                }
                catch (Exception e)
                {
                    Log.Error(e, Client);
                }
            }
        }

        public void GetMapSize() =>
            SendMessage("worldsize", (int)PacketIdentifier.WorldSize);
                
        public void GetMapSeed() =>
            SendMessage("seed", (int)PacketIdentifier.WorldSeed);

        public void SendDiscordCommand(string cmd, SocketMessage msg = null)
        {
            if (msg != null)
            {
                _currentIdentifier++;

                Callbacks.Add(_currentIdentifier, async (ResponsePacket packet) =>
                {
                    await HandleCallback(packet, msg.Channel);

                    Callbacks.Remove(_currentIdentifier);
                });
            }

            SendMessage(cmd, _currentIdentifier);
        }

        public void SendDiscordMessage(ulong discordUserId, string message, string username)
        {
            var steamId = Program.TryGetSteamId(discordUserId);

            var msgContent = new ResponseMessage()
            {
                Channel = 0,
                Content = message,
                Username = username,
                UserID = steamId,
                Color = Data.Chatlog.DefaultNameColor,
                Time = (uint)DateTime.UtcNow.Subtract(DateTime.UnixEpoch).TotalSeconds
            };

            message = JsonConvert.SerializeObject(msgContent);
            message = "discordsay " + message;

            SendMessage(message, (int)PacketIdentifier.DiscordMessage);
        }
           
        public async Task HandleCallback(ResponsePacket packet, 
            ISocketMessageChannel channel)
        {
            var cleanContent = Regex.Replace(packet.MessageContent, @"<.+?>", string.Empty);
            
            var embedBuilder = new EmbedBuilder();
            embedBuilder.WithAuthor("SERVER", Client?.CurrentUser?.GetAvatarUrl(), null);
            embedBuilder.WithColor(Bot.Data.Chatlog.ServerMessageColour.ToColor());
            embedBuilder.WithCurrentTimestamp();

            if (cleanContent.Length > 2000)
            {
                var splitText = cleanContent.SplitInParts(2000);

                for (int i = 0; i < splitText.Count(); i++)
                {
                    embedBuilder.WithFooter(i + "/" + (splitText.Count() - 1));

                    var text = splitText.ElementAt(i);
                    if (text.IsNullOrEmpty())
                        continue;

                    embedBuilder.WithDescription(text);

                    await channel.SendMessageAsync(null, false, embedBuilder.Build(), RequestMode);

                    await Task.Delay(1000);
                }

                return;
            }

            if (cleanContent.IsNullOrEmpty())
                return;

            embedBuilder.WithDescription(cleanContent);

            await channel.SendMessageAsync(null, false, embedBuilder.Build(), RequestMode);
        }

        #endregion

        #region Websocket Events

        public async Task OnOpen(object sender, EventArgs e)
        {
            Log.Info($"Websocket: Connected!", Client);
            await Bot.SetStatus("Connecting...");

            if (IsConnected)
            {
                GetMapSeed();
                GetMapSize();
            }
        }

        public async Task OnClose(object sender, CloseEventArgs e)
        {
            await Bot.SetStatus("Offline");
            _ = TryReconnect();
        }

        public async Task OnMessage(object sender, MessageEventArgs args)
        {
            try
            {
                if (!Bot.TryParseJson(args.Data, out ResponsePacket result))
                    return;

                if (result == null)
                    return;

                if (Callbacks.ContainsKey(result.Identifier))
                {
                    Callbacks[result.Identifier].Invoke(result);
                    return;
                }

                Log.Debug("Received message with idenfitier : " + result.Identifier, Client);

                switch (result.Identifier)
                {
                    case (int)PacketIdentifier.ServerInfo:
                        {
                            if (!Bot.TryParseJson<ResponseServerInfo>(result.MessageContent, out var serverInfo))
                                return;
                            await ProcessServerInfo(serverInfo);
                            break;
                        }                    
                    case (int)PacketIdentifier.WorldSeed:
                        {
                            _serverSeed = Regex.Replace(Regex.Replace(result.MessageContent, "\\W", string.Empty), "[a-zA-Z]", string.Empty);
                            break;
                        }                    
                    case (int)PacketIdentifier.WorldSize:
                        {
                            _serverWorldSize = Regex.Replace(Regex.Replace(result.MessageContent, "\\W", string.Empty), "[a-zA-Z]" ,string.Empty);
                            break;
                        }
                    case (int)PacketIdentifier.DiscordCommand:
                        {
                            await HandleCallback(result, Bot.ChatlogChannel as ISocketMessageChannel);
                            break;
                        }
                    default:
                        {                           
                            if (!Bot.TryParseJson<ResponseMessage>(result.MessageContent, out var message))
                                return;

                            if (message  == null || message.Content.IsNullOrEmpty())
                                return;
                            
                            await ProcessMessage(message);
                            break;
                        }
                }                              
            }
            catch (Exception e)
            {
                Log.Error(e, Client);
            }
        }

        public async Task OnError(object sender, ErrorEventArgs e)
        {
            if (e.Exception == null) return;
            Log.Error(e.Exception, Client);
        }

        #endregion
    }
}
