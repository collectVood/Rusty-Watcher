using Newtonsoft.Json;

namespace DiscordBot.Data
{
    public class ServerDataFile
    {
        [JsonProperty("Discord")]
        public DiscordDataFile Discord = new DiscordDataFile();
        [JsonProperty("Rcon")]
        public RconDataFile Rcon = new RconDataFile();        
        [JsonProperty("Settings")]
        public SettingsDataFile Settings = new SettingsDataFile();
        [JsonProperty("Localization")]
        public LocalizationDataFile Localization = new LocalizationDataFile();
    }
}
