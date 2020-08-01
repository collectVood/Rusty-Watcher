using Newtonsoft.Json;

namespace RustyWatcher.Rcon
{
    public class ResponseMessage
    {
        [JsonProperty("Channel")]
        public int Channel;
        [JsonProperty("Message")]
        public string Content;
        [JsonProperty("UserId")]
        public ulong UserID;
        [JsonProperty("Username")]
        public string Username;
        [JsonProperty("Color")]
        public string Color;
        [JsonProperty("Time")]
        public uint Time;
    }
}
