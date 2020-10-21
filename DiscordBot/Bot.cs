using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Discord;
using Discord.WebSocket;
using RustyWatcher.Data;
using Discord.Webhook;
using Discord.Commands;
using System.Collections.Generic;

namespace RustyWatcher
{
    public class Bot
    {
        #region Fields

        #region Discord related

        public DiscordSocketClient Client;

        public DiscordWebhookClient ChatlogWebhookClient;

        public IGuild CurrentGuild
        {
            get 
            {
                if (_currentGuild == null || string.IsNullOrEmpty(_currentGuild.Name))
                {
                    _currentGuild = Client.GetGuild(Data.Discord.GuildId);

                    if (_currentGuild == null || string.IsNullOrEmpty(_currentGuild.Name))
                        Log.Error("Unable to get current guild", Client);
                }

                return _currentGuild;
            } 
        }
        private IGuild _currentGuild;

        public ITextChannel ChatlogChannel;

        private string _lastUpdateString;

        public readonly RequestOptions RequestMode = new RequestOptions() { RetryMode = RetryMode.AlwaysRetry, Timeout = 10 };

        #endregion

        public ServerDataFile Data;

        public Websocket Websocket;
        
        #endregion

        #region Constructor

        public Bot(ServerDataFile data)
        {
            Data = data;
        }

        #endregion

        #region Init

        public async Task CreateBot()
        {
            Client = new DiscordSocketClient();

            try
            {
                if (Data.Chatlog.Use)
                    ChatlogWebhookClient = new DiscordWebhookClient(Data.Chatlog.WebhookUrl, new Discord.Rest.DiscordRestConfig());

                await Client.LoginAsync(TokenType.Bot, Data.Discord.Token);
                await Client.StartAsync();
            }
            catch (Exception e)
            {
                if (e.Message.Contains("Unauthorized") || e.Message.Contains("401")) 
                    Log.Warning("Unauthorized! Set up the config otherwise bot cannot connect!", Client);
                else 
                    Log.Error(e, Client);

                Console.ReadKey();
            }

            Client.Log += OnLog;
            Client.Ready += OnReady;
            Client.Disconnected += OnDisconnected;
            Client.MessageReceived += OnMessage;
            Client.ReactionAdded += OnReactionAdded;

            await Task.Delay(-1);
        }

        #endregion

        #region Discord Events

        private Task OnLog(LogMessage log)
        {
            Log.Info(log.Message, Client);
            return Task.CompletedTask;
        }

        private Task OnDisconnected(Exception _)
        {
            Log.Info("OnDisconnected triggered", Client);
            //Websocket?.Close();

            return Task.CompletedTask;
        }

        private Task OnReady()
        {
            _ = Setup();

            return Task.CompletedTask;
        }

        private Task OnMessage(SocketMessage msg)
        {
            if (msg.Author.IsBot)
            {
                return Task.CompletedTask;
            }

            int argPos = 0;
            var message = msg as SocketUserMessage;

            var prefixed = message.HasStringPrefix(Data.Chatlog.CommandPrefix, ref argPos) ||
                message.HasMentionPrefix(Client.CurrentUser, ref argPos);

            _ = ProcessMessage(msg, prefixed, argPos);

            return Task.CompletedTask;
        }

        public Task OnReactionAdded(Cacheable<IUserMessage, ulong> cacheable, 
            ISocketMessageChannel messageChannel, SocketReaction socketReaction)
        {
            if (socketReaction == null || !socketReaction.User.IsSpecified || socketReaction.User.Value.IsBot) 
                return Task.CompletedTask;

            _ = ProcessReactionAdd(cacheable, socketReaction);

            return Task.CompletedTask;
        }


        #endregion

        #region Methods

        private async Task Setup()
        {
            if (Data.Serverinfo.ShowPlayerCountStatus)
            {
                await SetStatus("Connecting...");
            }
            else
            {
                await Client.SetGameAsync(Data.Serverinfo.StatusMessage, null);
            }

            if (Data.Chatlog.Use)
            {
                await GetChatlogChannel();
            }

            if (Websocket == null)
            {
                await new Websocket(this).StartAsync();
            }
            else if (!Websocket.IsConnected)
            {
                _ = Websocket.TryReconnect();
            }
        }

        private async Task ProcessMessage(SocketMessage msg, bool prefixed, int argPos)
        {
            if (msg.Channel.Id == ChatlogChannel.Id && !prefixed)
            {
                await Websocket.OnDiscordMessage(msg);
                return;
            }

            if (prefixed)
            {
                await Websocket.OnDiscordCommand(msg, msg.Content.TrimWhitespaces().Substring(argPos));
            }
        }

        private async Task ProcessReactionAdd(Cacheable<IUserMessage, ulong> cacheable,
            SocketReaction socketReaction)
        {
            try
            {
                var reactor = socketReaction.User.Value;

                var msg = await cacheable.GetOrDownloadAsync();
                if (msg == null)
                    return;

                if (socketReaction.Channel.Id == ChatlogChannel?.Id &&
                    socketReaction.Emote.Name == "✅")
                {
                    if (msg.Author.Id != socketReaction.UserId)
                        return;

                    await Websocket.OnReactionAdded(socketReaction, msg);
                }
            }
            catch (Exception e)
            {
                Log.Error(e, Client);
            }
        }

        public async Task SetStatus(string status)
        {
            if (status == _lastUpdateString) 
                return;

            _lastUpdateString = status;

            if (Data.Serverinfo.ShowPlayerCountStatus)
            {
                if (status == "Offline") 
                    await Client.SetStatusAsync(UserStatus.DoNotDisturb);
                else if (Client.Status != UserStatus.Online) 
                    await Client.SetStatusAsync(UserStatus.Online);

                await Client.SetGameAsync(status, null, Enum.Parse<ActivityType>(Data.Discord.ActivityType.ToString()));
            }
            else
            {
                _lastUpdateString = Data.Serverinfo.StatusMessage;
                await Client.SetGameAsync(Data.Serverinfo.StatusMessage, null,
                    Enum.Parse<ActivityType>(Data.Discord.ActivityType.ToString()));
            }

        }

        public bool HasEqualValue<T>(List<T> list1, List<T> list2)
        {
            foreach (T elem in list1)
            {
                if (list2.Contains(elem))
                    return true;
            }

            return false;
        }

        public string GetCompleteHex(string input)
        {
            var colour = string.Empty;
            if (input.Length < 6)
            {
                var multiplier = 6 / input.Length;
                foreach (char c in input)
                {
                    for (int i = 0; i < multiplier; i++)
                    {
                        colour += c;
                    }
                }
            }
            else 
                colour = input;

            return colour;
        }

        public bool TryParseJson<T>(string obj, out T result)
        {
            try
            {
                JsonSerializerSettings settings = new JsonSerializerSettings();
                settings.MissingMemberHandling = MissingMemberHandling.Error;

                result = JsonConvert.DeserializeObject<T>(obj, settings);
                return true;
            }
            catch (Exception)
            {
                result = default(T);
                return false;
            }
        }

        public async Task GetChatlogChannel()
        {
            try
            {
                if (CurrentGuild == null)
                    return;

                if (ChatlogChannel == null || string.IsNullOrEmpty(ChatlogChannel.Name))
                {
                    ChatlogChannel = await CurrentGuild.GetTextChannelAsync(Data.Chatlog.ChannelId, CacheMode.AllowDownload, RequestMode);
                    if (ChatlogChannel == null || string.IsNullOrEmpty(ChatlogChannel.Name))
                    {
                        Log.Warning("Couldn't get chat log channel! ID : " + Data.Chatlog.ChannelId, Client);
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(e, Client);
            }
        }

        #endregion
    }
}
