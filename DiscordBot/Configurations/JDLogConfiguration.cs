using Newtonsoft.Json;

namespace RustyWatcher.Configurations;

public class JDLogConfiguration
{
    [JsonProperty(PropertyName = "Use")] 
    public bool Use;
    
    [JsonProperty(PropertyName = "Join Format")]
    public string JoinFormat = "Player {0} (`{1}`) just joined.";    
    
    [JsonProperty(PropertyName = "Disconnect Format")]
    public string LeaveFormat = "Player {0} (`{1}`) just disconnected.";
    
    [JsonProperty("Embed color (RGB)")]
    public RGBConfiguration EmbedColor = new();
}