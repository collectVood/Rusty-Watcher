using System;
using Newtonsoft.Json;

namespace RustyWatcher.Configurations;

public class BalancingConfiguration
{
    [JsonProperty("Use")]
    public bool Use;

    [JsonProperty("Min Avg Fps % Differ to Consider Spike (i.e. 0.5 would mean if current fps is 50% lower than avg it will consider a spike)")]
    public float MinAvgFpsDiffer = 0.25f;

    [JsonProperty("Spike Run Commands", ObjectCreationHandling = ObjectCreationHandling.Replace)] 
    public string[] SpikeRunCommands = {"abc"};
    
    [JsonProperty("Spike Restoke Run Commands", ObjectCreationHandling = ObjectCreationHandling.Replace)] 
    public string[] SpikeRestoreRunCommands = {"cba"};

    [JsonProperty("Spike Message")] 
    public string SpikeMessage = "<@&708414690157658195> Lag Spike Detected <t:{0}>.\nAvg Fps: `{1}` - Spike Fps: `{2}` (*running spike commands*)";
    
    [JsonProperty("Spike Revert Message")] 
    public string SpikeRevertMessage = "Reset Spike <t:{0}> (*ran restore commands*)";

    [JsonProperty("Spike Discord Webhook Url")] 
    public string SpikeDiscordWebhook = string.Empty;

    [JsonProperty("Spike Reset")] 
    public TimeSpan SpikeReset = TimeSpan.FromSeconds(20f);
}