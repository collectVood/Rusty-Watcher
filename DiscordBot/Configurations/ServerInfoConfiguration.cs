using Newtonsoft.Json;

namespace RustyWatcher.Configurations;

public class ServerInfoConfiguration
{
    [JsonProperty("Server info channel ID")]
    public ulong ChannelId;
    
    [JsonProperty("Show player count in status")]
    public bool ShowPlayerCountStatus = true;   
    
    [JsonProperty("Status message (only if player count is not shown in status)")]
    public string StatusMessage = string.Empty;
    
    [JsonProperty("Get server region data over (https://ipinfo.io)")]
    public bool GetServerRegion;        
    
    [JsonProperty("Show server info in embed")]
    public bool ShowEmbed = true;

    [JsonProperty(PropertyName = "Queue Format (use null if include in players)")]
    public string QueueFormat = "({0} Queued)";
    
    [JsonProperty(PropertyName = "Joining Format (use null if include in players)")]
    public string JoiningFormat = "({0} Joining)";
    
    [JsonProperty(PropertyName = "Player Count Status")]
    public string PlayerStatus = "{0} / {1} {2} {3}";   
    
    [JsonProperty("Server info embed title hyperlink")]
    public string EmbedLink = string.Empty;   
    
    [JsonProperty("Server info embed color (RGB)")]
    public RGBConfiguration EmbedColor = new();

    [JsonIgnore] public string WorldSeed;
    [JsonIgnore] public string WorldSize;
}