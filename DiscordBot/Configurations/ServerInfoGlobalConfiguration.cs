using Newtonsoft.Json;

namespace RustyWatcher.Configurations;

public class ServerInfoGlobalConfiguration
{
    [JsonProperty(PropertyName = "Queue Format (use null if include in players)")]
    public string QueueFormat = "({0} Queued)";
    
    [JsonProperty(PropertyName = "Joining Format (use null if include in players)")]
    public string JoiningFormat = "({0} Joining)";
    
    [JsonProperty(PropertyName = "Player Count Status")]
    public string PlayerStatus = "{0} / {1} {2} {3}";
}