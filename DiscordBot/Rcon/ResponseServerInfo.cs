using Newtonsoft.Json;

namespace DiscordBot.Rcon
{
    public class ResponseServerInfo
    {
        [JsonProperty("Hostname")]
        public string Hostname;
        [JsonProperty("MaxPlayers")]
        public int MaxPlayers;
        [JsonProperty("Players")]
        public int Players;
        [JsonProperty("Queued")]
        public int Queued;
        [JsonProperty("Joining")]
        public int Joining;
        [JsonProperty("EntityCount")]
        public int EntityCount;
        [JsonProperty("GameTime")]
        public string GameTime;
        [JsonProperty("Uptime")]
        public int Uptime;
        [JsonProperty("Map")]
        public string Map;
        [JsonProperty("Framerate")]
        public string Framerate;
        [JsonProperty("Memory")]
        public int Memory;
        [JsonProperty("Collections")]
        public int Collections;
        [JsonProperty("NetworkIn")]
        public int NetworkIn;
        [JsonProperty("NetworkOut")]
        public int NetworkOut;
        [JsonProperty("Restarting")]
        public bool Restarting;
        [JsonProperty("SaveCreatedTime")]
        public string SaveCreatedTime;
    }
}
