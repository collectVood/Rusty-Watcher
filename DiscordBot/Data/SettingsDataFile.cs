using Newtonsoft.Json;

namespace DiscordBot.Data
{
    public class SettingsDataFile
    {
        [JsonProperty("Show player count in status")]
        public bool ShowPlayerCountStatus = true;             
        [JsonProperty("Status message (only if player count is not shown in status)")]
        public string StatusMessage = string.Empty;
        [JsonProperty("Get server region data over (https://ipinfo.io)")]
        public bool GetServerRegion = false;        
        [JsonProperty("Show server info in embed")]
        public bool ShowServerInfoEmbed = true;
        [JsonProperty("Server info embed link")]
        public string ServerInfoEmbedLink = string.Empty;          
        [JsonProperty("Server info embed color (rgb)")]
        public RGB ServerInfoEmbedColor = new RGB();   
    }

    public class RGB
    {
        [JsonProperty("Red")]
        public int Red = 44;
        [JsonProperty("Green")]
        public int Green = 47;
        [JsonProperty("Blue")]
        public int Blue = 51;
    }
}
