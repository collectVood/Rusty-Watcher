using System;
using System.Net.Http;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Globalization;
using System.Text.RegularExpressions;
using Discord;
using Discord.WebSocket;
using DiscordBot.Data;
using DiscordBot.Rcon;
using Newtonsoft.Json;
using WebSocketSharp;

namespace DiscordBot
{
    public class Websocket
    {
        #region Fields

        #region Discord related

        public DiscordSocketClient Client => Bot.Client;

        public bool StartupRequired = true;

        public IGuild CurrentGuild => Bot.CurrentGuild;

        public ITextChannel ServerInfoEmbedChannel = null;

        public IUserMessage ServerInfoMessage = null;

        #endregion

        public Bot Bot;

        public WebSocket WS;

        public ServerDataFile Data => Bot.Data;

        public bool IsConnected => WS.ReadyState == WebSocketState.Open && WS.ReadyState != WebSocketState.Closed;
        
        private static readonly HttpClient HttpClient = new HttpClient();

        #endregion

        #region Cached Data

        private string Region = string.Empty;

        private string ServerSeed = string.Empty;

        private string ServerWorldSize = string.Empty;

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
            if (!StartupRequired) return;

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

                StartupRequired = false;

                ServerInfo();
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
                else players = (serverInfo.Players + serverInfo.Joining).ToString();

                await Bot.SetStatus(string.Format(Data.Localization.PlayerStatus, 
                    players, serverInfo.MaxPlayers, queued));

                //Embed
                if (!Data.Settings.ShowServerInfoEmbed) return;
                await UpdateOrCreateEmbed(serverInfo, string.Format(Data.Localization.PlayerStatus,
                    players, serverInfo.MaxPlayers, queued));
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
                if (ServerInfoMessage == null)
                {
                    Bot.GetCurrentGuild();

                    if (ServerInfoEmbedChannel == null)
                    {
                        ServerInfoEmbedChannel = await CurrentGuild.GetTextChannelAsync(Data.Discord.ServerInfoChannelID);
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
                               
                embedBuilder.WithTitle(string.Format(Data.Localization.EmbedTitle, Region, serverInfo.Hostname));
                embedBuilder.WithUrl(Data.Settings.ServerInfoEmbedLink);
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
                    }
                };

                if (!string.IsNullOrEmpty(ServerSeed) &&
                    !string.IsNullOrEmpty(ServerWorldSize))
                {
                    string mapLink = string.Empty;
                    if (serverInfo.Map == "Procedural Map")
                        mapLink = $"http://playrust.io/map/?Procedural%20Map_{ServerWorldSize}_{ServerSeed}"; 
                    else if (serverInfo.Map == "Barren")
                        mapLink = $"http://playrust.io/map/?Barren_{ServerWorldSize}_{ServerSeed}";

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

                embedBuilder.WithColor(Data.Settings.ServerInfoEmbedColor.Red, Data.Settings.ServerInfoEmbedColor.Green, Data.Settings.ServerInfoEmbedColor.Blue);

                if (ServerInfoMessage == null)
                {
                    //Create message
                    ServerInfoMessage = await ServerInfoEmbedChannel.SendMessageAsync(null, false, embedBuilder.Build());
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

        public async Task GetRegion()
        {
            if (!Data.Settings.GetServerRegion) return;
            if (!string.IsNullOrEmpty(Region)) return;

            try
            {
                var response = await HttpClient.GetAsync($"https://ipinfo.io/{Data.Rcon.ServerIP}/country");
                if (!response.IsSuccessStatusCode)
                {
                    Log.Debug("Region request failed : Code " + response.StatusCode, Client);
                    return;
                }

                string region = await response.Content.ReadAsStringAsync();
                string final = Regex.Replace(region.ToLower(), @"\t|\n|\r", string.Empty);

                Log.Debug("Received region : " + final, Client);
                Region = $":flag_{final}:";
            }
            catch (Exception e)
            {
                Log.Error(e, Client);
            }
        }

        public void SendMessage(string message, PacketIdentifier identifier)
        {
            Packet packet = new Packet(message, (int)identifier);
            var msg = JsonConvert.SerializeObject(packet);
            if (!IsConnected) return;

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
                    if (!IsConnected) WS.Connect(); 
                }
                catch { }
            }
        }

        public async Task ServerInfo()
        {
            while (true)
            {
                if (IsConnected) SendMessage("serverinfo", PacketIdentifier.ServerInfo);
                else await Bot.SetStatus("Offline");
                await Task.Delay(Program.Data.DiscordDelay * 1000);
            }
        }        
        
        public void GetMapSize() =>
            SendMessage("worldsize", PacketIdentifier.WorldSize);
                
        public void GetMapSeed() =>
            SendMessage("seed", PacketIdentifier.WorldSeed);


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
                if (!Bot.TryParseJson(args.Data, out ResponsePacket result)) return;
                switch (result.Identifier)
                {
                    case (int)PacketIdentifier.ServerInfo:
                        {
                            if (!Bot.TryParseJson(result.MessageContent, out ResponseServerInfo serverInfo)) return;
                            await ProcessServerInfo(serverInfo);
                            break;
                        }                    
                    case (int)PacketIdentifier.WorldSeed:
                        {
                            ServerSeed = Regex.Replace(Regex.Replace(result.MessageContent, "\\W", string.Empty), "[a-zA-Z]", string.Empty);
                            break;
                        }                    
                    case (int)PacketIdentifier.WorldSize:
                        {
                            ServerWorldSize = Regex.Replace(Regex.Replace(result.MessageContent, "\\W", string.Empty), "[a-zA-Z]" ,string.Empty);
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
