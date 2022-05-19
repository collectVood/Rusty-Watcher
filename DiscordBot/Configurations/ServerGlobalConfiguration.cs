using Newtonsoft.Json;

namespace RustyWatcher.Configurations;

public class ServerGlobalConfiguration
{
    [JsonProperty("Enabled")]
    public bool Enabled = false;
    
    [JsonProperty("Discord")]
    public DiscordConfiguration Discord = new();
    
    [JsonProperty("Serverinfo Global")]
    public ServerInfoGlobalConfiguration ServerInfoGlobal = new();
}