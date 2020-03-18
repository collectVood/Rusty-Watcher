using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Discord;
using Discord.WebSocket;
using DiscordBot.Data;

namespace DiscordBot
{
    public class Bot
    {
        #region Fields

        #region Discord related

        public DiscordSocketClient Client;

        public IGuild CurrentGuild;

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
                await Client.LoginAsync(TokenType.Bot, Data.Discord.Token);
                await Client.StartAsync();
            }
            catch (Exception e)
            {
                if (e.Message.Contains("Unauthorized") || e.Message.Contains("401")) Log.Warning("Unauthorized! Set up the config otherwise bot cannot connect!", Client);
                else Log.Error(e, Client);

                Console.ReadKey();
            }

            Client.Log += OnLog;
            Client.Ready += OnReady;
            Client.Disconnected += OnDisconnected;

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
            Websocket?.Close();

            return Task.CompletedTask;
        }

        private async Task OnReady()
        {
            if (Data.Settings.ShowPlayerCountStatus) await Client.SetGameAsync("Connecting...", null);
            else await Client.SetGameAsync(Data.Settings.StatusMessage, null);

            GetCurrentGuild();

            await new Websocket(this).StartAsync();
        }

        #endregion

        #region Methods

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

        public void GetCurrentGuild()
        {
            try
            {
                if (CurrentGuild == null || string.IsNullOrEmpty(CurrentGuild.Name))
                {
                    CurrentGuild = Client.GetGuild(Data.Discord.GuildId);
                    if (CurrentGuild == null || string.IsNullOrEmpty(CurrentGuild.Name))
                    {
                        Log.Warning("Couldn't get guild! ID : " + Data.Discord.GuildId, Client);
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
