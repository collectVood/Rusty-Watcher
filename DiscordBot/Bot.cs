using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace DiscordBot
{
    public class Bot
    {
        public DiscordSocketClient _client;
        public Program.ServerData _data;
        
        public Bot(Program.ServerData data)
        {
            _data = data;
        }

        public async Task CreateBot()
        {
            _client = new DiscordSocketClient();

            try
            {
                await _client.LoginAsync(TokenType.Bot, _data.Token);
                await _client.StartAsync();
            }
            catch (Exception e)
            {
                if (e.Message.Contains("Unauthorized") || e.Message.Contains("401")) Log.Warning("Unauthorized! Set up the config otherwise bot cannot connect!", _client);
                else Log.Error(e.Message, e.StackTrace, _client);

                Console.ReadKey();
            }

            _client.Log += OnLog;
            _client.Ready += OnReady;
            _client.Disconnected += OnDisconnected;

            await new Websocket(_data, _client).StartAsync();

            await Task.Delay(-1);
        }

        private Task OnLog(LogMessage log)
        {
            Log.Info(log.Message, _client);
            return Task.CompletedTask;
        }

        private Task OnDisconnected(Exception _) => Task.CompletedTask;

        private async Task OnReady() => await _client.SetGameAsync("Connecting...", null);
    }
}
