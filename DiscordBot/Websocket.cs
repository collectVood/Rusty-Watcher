using System;
using System.Threading.Tasks;
using WebSocketSharp;
using Newtonsoft.Json;
using Discord;
using Discord.WebSocket;

namespace DiscordBot
{
    #region Rcon Class

    public class Packet
    {
        public Packet(string message)
        {
            Message = message;
        }
        [JsonProperty("Identifier")]
        public int Identifier = 1;
        [JsonProperty("Message")]
        public string Message;
        [JsonProperty("Name")]
        public string Name = "WebRcon";
    }

    #region ServerInfo

    public class ServerInfoData
    {
        [JsonProperty("Message")]
        public string MessageContent { get; set; }
        [JsonProperty("Identifier")]
        public int Identifier { get; set; }
        [JsonProperty("Type")]
        public string Type { get; set; }
        [JsonProperty("Stacktrace")]
        public object Stacktrace { get; set; }
    }

    public class ServerInfoMessage
    {
        [JsonProperty("Hostname")]
        public string Hostname { get; set; }
        [JsonProperty("MaxPlayers")]
        public int MaxPlayers { get; set; }
        [JsonProperty("Players")]
        public int Players { get; set; }
        [JsonProperty("Queued")]
        public int Queued { get; set; }
        [JsonProperty("Joining")]
        public int Joining { get; set; }
        [JsonProperty("EntityCount")]
        public int EntityCount { get; set; }
        [JsonProperty("GameTime")]
        public string GameTime { get; set; }
        [JsonProperty("Uptime")]
        public int Uptime { get; set; }
        [JsonProperty("Map")]
        public string Map { get; set; }
        [JsonProperty("Framerate")]
        public string Framerate { get; set; }
        [JsonProperty("Memory")]
        public int Memory { get; set; }
        [JsonProperty("Collections")]
        public int Collections { get; set; }
        [JsonProperty("NetworkIn")]
        public int NetworkIn { get; set; }
        [JsonProperty("NetworkOut")]
        public int NetworkOut { get; set; }
        [JsonProperty("Restarting")]
        public bool Restarting { get; set; }
        [JsonProperty("SaveCreatedTime")]
        public string SaveCreatedTime { get; set; }

    }

    #endregion

    #endregion

    public class Websocket
    {
        public WebSocket _ws;
        public Program.ServerData _data;
        public DiscordSocketClient _client;    
        public string lastUpdateString;

        public Websocket(Program.ServerData data, DiscordSocketClient client)
        {
            _data = data;
            _client = client;           
        }

        public async Task StartAsync()
        {
            _ws = new WebSocket($"ws://{_data.RconIP}:{_data.RconPort}/{_data.RconPW}");
            
            _ws.Log.Level = LogLevel.Trace;

            Log.Info("Websocket: Connecting...", _client);

            try
            {
                _ws.Connect();
                ServerInfo();

                await TryReconnect();
            }
            catch (Exception)
            {
            } 
            
            _ws.OnOpen += async (sender, e) =>
            {               
                Log.Info($"Websocket: Connected", _client);
                await SetStatus("Connecting...");
            };
                          
           _ws.OnMessage += async (sender, e) =>
            {               
                try
                {
                    Log.Debug(_ws.Ping("serverinfo").ToString(), _client);
                    Log.Warning("Received message");
                    var data = JsonConvert.DeserializeObject<ServerInfoData>(e.Data);
                    var serverInfo = JsonConvert.DeserializeObject<ServerInfoMessage>(data.MessageContent);
                    
                    if (serverInfo.Queued > 0)
                    {
                        if (serverInfo.Players + serverInfo.Joining > serverInfo.MaxPlayers)
                        {
                            await SetStatus(serverInfo.MaxPlayers + "/" + serverInfo.MaxPlayers + $" (Queued: {serverInfo.Queued})");
                            return;
                        }
                        await SetStatus(serverInfo.Players + serverInfo.Joining + "/" + serverInfo.MaxPlayers + $" (Queued: {serverInfo.Queued})");
                        return;
                    }
                    if (serverInfo.Players + serverInfo.Joining > serverInfo.MaxPlayers)
                    {
                        await SetStatus(serverInfo.MaxPlayers + "/" + serverInfo.MaxPlayers);
                        return;
                    }
                    await SetStatus(serverInfo.Players + serverInfo.Joining + "/" + serverInfo.MaxPlayers);
                }
                catch (Exception)
                {
                }
            };

            _ws.OnError += (sender, e) => Log.Error($"Websocket Error: {e.Message}", e.Exception.StackTrace, _client);

            _ws.OnClose += async (sender, e) =>
            {
                Log.Warning(e.Reason, _client);
                Log.Info("Websocket: Connection closed", _client);

                await TryReconnect(true);
            };

            await Task.Delay(-1);
        }

        public void SendMessage(string message)
        {
            Packet packet = new Packet(message);
            var msg = JsonConvert.SerializeObject(packet);
            _ws.Send(msg);
        }

        public async Task SetStatus(string status)
        {
            Log.Warning(status);
            if (status == lastUpdateString) return;
            lastUpdateString = status;

            if (status == "Offline") await _client.SetStatusAsync(UserStatus.DoNotDisturb);
            else if (_client.Status != UserStatus.Online) await _client.SetStatusAsync(UserStatus.Online);
            await _client.SetGameAsync(status, null);
            Log.Debug($"Websocket: {status}", _client);
        }

        public async Task TryReconnect(bool isClose = false)
        {
            if (!isClose)
            {
                while (!_ws.IsConnected)
                {
                    Log.Info("Websocket: Reconnecting", _client);
                    try { _ws.Connect(); }
                    catch { }
                    await Task.Delay(Program._data.ReconnectDelay * 1000);
                }
            }
            else
            {
                Log.Info("Websocket: Reconnecting", _client);
                try { _ws.Connect(); }
                catch { }
                await Task.Delay(Program._data.ReconnectDelay * 1000);
            }
        }

        public async Task ServerInfo()
        {
            while (true)
            {
                await Task.Delay(Program._data.DiscordDelay * 1000);
                if (_ws.IsConnected) SendMessage("serverinfo");
                else await SetStatus("Offline");
            }
        }
    }
}
