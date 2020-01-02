using System;
using Discord.WebSocket;

namespace DiscordBot
{
    internal class Log
    {
        public static string GetName(DiscordSocketClient client) => client?.CurrentUser != null ? client.CurrentUser.Username : "Unknown";

        public static void Info(string message, DiscordSocketClient client = null)
        {
            Console.WriteLine($"[{DateTime.Now}] {GetName(client)} - " + message);
        }

        public static void Debug(string message, DiscordSocketClient client = null)
        {
            if (!Program._data.Debug) return;
            Console.WriteLine($"[DEBUG - {DateTime.Now}] {GetName(client)} - " + message);
        }

        public static void Error(string message, string stacktrace, DiscordSocketClient client = null)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[{DateTime.Now}] {GetName(client)} -  Error: " + message + " || Stacktrace: " + stacktrace);
            Console.ResetColor();
        }

        public static void Warning(string message, DiscordSocketClient client = null)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[{DateTime.Now}] {GetName(client)} - Warning: " + message);
            Console.ResetColor();
        }
    }
}
