using Newtonsoft.Json;

namespace RustyWatcher.Configurations;

public class RconConfiguration
{
    [JsonProperty("Server IP")]
    public string ServerIP = string.Empty;
    
    [JsonProperty("Server Direct IP")]
    public string ServerDirectIP = string.Empty;
    
    [JsonProperty("Rcon Port")]
    public string RconPort = string.Empty;
    
    [JsonProperty("Rcon Password")]
    public string RconPW = string.Empty;
    
    [JsonProperty("Server Port (Optional only used in ServerInfo Embed)")]
    public string ServerPort = string.Empty;

    [JsonProperty("Reconnect delay (seconds)")]
    public int ReconnectDelay = 10;
    
    [JsonProperty("Timeout Commands (seconds)")]
    public int TimeoutCommands = 3;
}