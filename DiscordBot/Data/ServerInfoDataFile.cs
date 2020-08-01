using Newtonsoft.Json;

namespace RustyWatcher.Data
{
    public class ServerInfoDataFile
    {
        [JsonProperty("Server info channel ID")]
        public ulong ChannelId = 0;
        [JsonProperty("Show player count in status")]
        public bool ShowPlayerCountStatus = true;             
        [JsonProperty("Status message (only if player count is not shown in status)")]
        public string StatusMessage = string.Empty;
        [JsonProperty("Get server region data over (https://ipinfo.io)")]
        public bool GetServerRegion = false;        
        [JsonProperty("Show server info in embed")]
        public bool ShowEmbed = true;
        [JsonProperty("Server info embed title hyperlink")]
        public string EmbedLink = string.Empty;          
        [JsonProperty("Server info embed color (RGB)")]
        public RGB EmbedColor = new RGB();   
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
