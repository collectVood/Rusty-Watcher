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
    }
}
