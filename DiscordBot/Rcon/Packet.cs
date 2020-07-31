using Newtonsoft.Json;

namespace RustyWatcher.Rcon
{
    public class Packet
    {
        public Packet(string message, int identifier = -1)
        {
            Message = message;
            Identifier = identifier;
        }
        [JsonProperty("Identifier")]
        public int Identifier;
        [JsonProperty("Message")]
        public string Message;
    }
}
