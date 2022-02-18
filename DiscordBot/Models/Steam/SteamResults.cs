using System.Collections.Generic;
using Newtonsoft.Json;

namespace RustyWatcher.Models.Steam;

public class SteamResults
{
    [JsonProperty("players")]
    public List<SteamPlayer> Players { get; set; }
}