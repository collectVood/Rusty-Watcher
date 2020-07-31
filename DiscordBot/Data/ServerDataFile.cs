using Newtonsoft.Json;

namespace RustyWatcher.Data
{
    public class ServerDataFile
    {
        [JsonProperty("Discord")]
        public DiscordDataFile Discord = new DiscordDataFile();
        [JsonProperty("Rcon")]
        public RconDataFile Rcon = new RconDataFile();
        [JsonProperty("Chatlog")]
        public ChatlogDataFile Chatlog = new ChatlogDataFile();
        [JsonProperty("Serverinfo")]
        public ServerInfoDataFile Serverinfo = new ServerInfoDataFile();
        [JsonProperty("Localization")]
        public LocalizationDataFile Localization = new LocalizationDataFile();
    }
}
