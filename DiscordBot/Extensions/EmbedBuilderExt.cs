using Discord;
using RustyWatcher.Helpers;

namespace RustyWatcher.Extensions;

public static class EmbedBuilderExt
{
    public static void WithCustomFooter(this EmbedBuilder builder)
    {
        builder.WithFooter($"RustyWatcher {Utilities.GetVersionString()}");
        builder.WithCurrentTimestamp();
    }
}