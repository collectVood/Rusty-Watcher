using Newtonsoft.Json;

namespace RustyWatcher.Models.Steam;

public class SteamRootObject
{
    [JsonProperty("response")]
    public SteamResults Results { get; set; }
}