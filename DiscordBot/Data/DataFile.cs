using System.Collections.Generic;
using Newtonsoft.Json;

namespace DiscordBot.Data
{
    public class DataFile
    {
        [JsonProperty("Discord refresh (seconds)")]
        public int DiscordDelay = 30;
        [JsonProperty("Reconnect delay (seconds)")]
        public int ReconnectDelay = 10;        
        [JsonProperty("Create output file")]
        public bool CreateOutputfile = false;
        [JsonProperty("Servers", ObjectCreationHandling = ObjectCreationHandling.Replace)]
        public List<ServerDataFile> ServerData = new List<ServerDataFile> { new ServerDataFile() };
        [JsonProperty("Localization")]
        public LocalizationDataFile Localization = new LocalizationDataFile();
        [JsonProperty("Debug")]
        public bool Debug = false;
    }
}
