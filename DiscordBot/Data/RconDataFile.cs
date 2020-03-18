using Newtonsoft.Json;

namespace DiscordBot.Data
{
    public class RconDataFile
    {
        [JsonProperty("Server IP")]
        public string ServerIP = string.Empty;
        [JsonProperty("Rcon Port")]
        public string RconPort = string.Empty;
        [JsonProperty("Server Port")]
        public string ServerPort = string.Empty;
        [JsonProperty("Rcon Password")]
        public string RconPW = string.Empty;
    }
}
