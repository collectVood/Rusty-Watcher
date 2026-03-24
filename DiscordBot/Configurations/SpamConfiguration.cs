using System;
using Newtonsoft.Json;

namespace RustyWatcher.Configurations;

public class SpamConfiguration
{
    [JsonProperty(PropertyName = "Use")] 
    public bool Use;
        
    [JsonProperty(PropertyName = "Allowed Send Frequency")]
    public TimeSpan AllowedSendFrequency = TimeSpan.FromSeconds(7);
    
    [JsonProperty(PropertyName = "Content Streak Mute Ceiling")]
    public int ContentStreakMuteCeiling = 6;
    
    [JsonProperty(PropertyName = "Ignore Content Streak Length Floor")]
    public int IgnoreContentStreakLengthFloor = 6;
    
    [JsonProperty(PropertyName = "Min Length for Levenshtein Similiarty Decision")]
    public int MinLengthForLevenshtein = 15;
    
    [JsonProperty(PropertyName = "Percentage for Levenshtein to consider the same")]
    public int PercentageForLevenshteinSame = 80;

    [JsonProperty(PropertyName = "Command Format")]
    public string CommandFormat = "mute {SteamId} \"{Username}\" 1h Spam";
    
    [JsonProperty(PropertyName = "Ignore Content Streak Regex")]
    public string IgnoreContentStreakRegex = @"C-?\d+";
}