using System.Collections.Generic;
using Newtonsoft.Json;

namespace RustyWatcher.Configurations;

public class PopulationConfiguration
{
    [JsonProperty(PropertyName = "Enabled")]
    public bool Enabled = false;
    
    [JsonProperty(PropertyName = "Refresh Delay (seconds)")]
    public float RefreshDelay = 30f;
    
    [JsonProperty(PropertyName = "Dynamic Pops (Key: Pop, Value: Slots)", 
        ObjectCreationHandling = ObjectCreationHandling.Replace)]
    public Dictionary<int, int> DynamicPops = new Dictionary<int, int>()
    {
        { 0, 100 }
    };

    [JsonProperty(PropertyName = "Rcon Command")]
    public string RconCommand = "maxplayers {0}";
}