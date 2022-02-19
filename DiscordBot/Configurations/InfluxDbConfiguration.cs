using Newtonsoft.Json;

namespace RustyWatcher.Configurations;

public class InfluxDbConfiguration
{
    [JsonProperty("Use")] 
    public bool Use;
    
    [JsonProperty("Address")]
    public string Address = string.Empty;

    [JsonProperty("Port")] 
    public string Port = string.Empty;
    
    [JsonProperty("Username")]
    public string Username = string.Empty;
    
    [JsonProperty("Password")]
    public string Password = string.Empty; 
    
    [JsonProperty("Database")]
    public string Database = string.Empty;
}