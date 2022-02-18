using Newtonsoft.Json;

namespace RustyWatcher.Models.Rcon;

public class Packet
{
    [JsonProperty("Identifier")]
    public int Identifier;
    [JsonProperty("Message")]
    public string Message;
    
    public Packet(string message, int identifier = -1)
    {
        Message = message;
        Identifier = identifier;
    }
}

