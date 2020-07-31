using Newtonsoft.Json;

namespace RustyWatcher.Data
{
    public class RconDataFile
    {
        [JsonProperty("Server IP")]
        public string ServerIP = string.Empty;
        [JsonProperty("Rcon Port")]
        public string RconPort = string.Empty;
        [JsonProperty("Rcon Password")]
        public string RconPW = string.Empty;
        [JsonProperty("Server Port (Optional only used in ServerInfo Embed)")]
        public string ServerPort = string.Empty;
    }
}
