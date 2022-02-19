using Newtonsoft.Json;

namespace RustyWatcher.Models.Rcon;

public class ResponseJoinLeave
{
    [JsonProperty("UserId")]
    public string UserId;
    
    [JsonProperty("Nitro")]
    public bool Nitro;
}