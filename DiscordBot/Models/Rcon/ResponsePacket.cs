using Newtonsoft.Json;

namespace RustyWatcher.Models.Rcon;

public class ResponsePacket
{
    [JsonProperty("Message")]
    public string MessageContent;
    
    [JsonProperty("Identifier")]
    public int Identifier;
    
    [JsonProperty("Type")]
    public string Type;
    
    [JsonProperty("Stacktrace")]
    public object Stacktrace;
}

