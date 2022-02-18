using Discord;
using Newtonsoft.Json;

namespace RustyWatcher.Configurations;

public class RGBConfiguration
{
    [JsonProperty("Red")]
    public int Red = 44;
    [JsonProperty("Green")]
    public int Green = 47;
    [JsonProperty("Blue")]
    public int Blue = 51;

    public Color ToDiscordColor()
    {
        return new Color(Red, Green, Blue);
    }
}