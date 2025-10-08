using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace RustyWatcher.Models.Rcon;

public class ResponsePacket
{
    [JsonProperty("Message")]
    public string MessageContent;
    
    [JsonProperty("Identifier")]
    public int Identifier;
    
    [JsonProperty("Type")]
    [JsonConverter(typeof(StringEnumConverter))]
    public LogType Type;
    
    [JsonProperty("Stacktrace")]
    public object Stacktrace;
}

