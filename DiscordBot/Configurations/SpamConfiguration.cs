using System;
using Newtonsoft.Json;

namespace RustyWatcher.Configurations;

public class SpamConfiguration
{
    [JsonProperty(PropertyName = "Allowed Send Frequency")]
    public TimeSpan AllowedSendFrequency = TimeSpan.FromSeconds(7);
    
    [JsonProperty(PropertyName = "Content Streak Mute Ceiling")]
    public int ContentStreakMuteCeiling = 6;
    
    [JsonProperty(PropertyName = "Ignore Content Streak Length Floor")]
    public int IgnoreContentStreakLengthFloor = 6;
    
    [JsonProperty(PropertyName = "Ignore Content Streak Regex")]
    public string IgnoreContentStreakRegex = @"C-?\d+";
}