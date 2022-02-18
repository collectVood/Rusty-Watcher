using System.Diagnostics;
using System.IO;

namespace RustyWatcher.Helpers;

public static class Utilities
{
    public static string GetBasePath()
    {
        using var processModule = Process.GetCurrentProcess().MainModule;
        return Path.GetDirectoryName(processModule?.FileName);
    }
}


