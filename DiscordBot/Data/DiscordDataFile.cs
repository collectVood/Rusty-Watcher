using Newtonsoft.Json;

namespace DiscordBot.Data
{
    public class DiscordDataFile
    {
        [JsonProperty("Bot Token")]
        public string Token = string.Empty;        
        [JsonProperty("Guild ID")]
        public ulong GuildId = 0;
        [JsonProperty("Server info channel ID")]
        public ulong ServerInfoChannelID = 0;        
        [JsonProperty("Activity type (0 = Playing, 1 = Streaming, 2 = Listening, 3 = Watching)")]
        public int ActivityType = 0;
    }
}
