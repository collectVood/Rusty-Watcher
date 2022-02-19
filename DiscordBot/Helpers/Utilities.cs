using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace RustyWatcher.Helpers;

public static class Utilities
{
    public static string GetBasePath()
    {
        using var processModule = Process.GetCurrentProcess().MainModule;
        return Path.GetDirectoryName(processModule?.FileName);
    }

    public static string GetVersionString()
    {            
        var assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version;
        return $"v({assemblyVersion.Major}.{assemblyVersion.Minor}.{assemblyVersion.Build})";
    }
}


