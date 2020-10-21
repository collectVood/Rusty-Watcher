using System.Collections.Generic;
using Newtonsoft.Json;

namespace RustyWatcher.Data
{
    public class DataFile
    {
        [JsonProperty("Discord refresh (seconds)")]
        public int DiscordDelay = 30;
        [JsonProperty("Reconnect delay (seconds)")]
        public int ReconnectDelay = 10;        
        [JsonProperty("Create output file")]
        public bool CreateOutputfile = false;
        [JsonProperty("Steam API Key (for avatars)")]
        public string SteamAPIKey = string.Empty;
        [JsonProperty("Servers", ObjectCreationHandling = ObjectCreationHandling.Replace)]
        public List<ServerDataFile> ServerData = new List<ServerDataFile> { new ServerDataFile() };
        [JsonProperty("Staff Discord & SteamIds (Key: DiscordId; Value: SteamId)")]
        public Dictionary<ulong, ulong> DiscordSteamIds = new Dictionary<ulong, ulong>
        {
            { 0, 0 },
        };
        [JsonProperty("Debug")]
        public bool Debug = false;
    }
}
