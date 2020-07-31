using System;
using Discord.WebSocket;

namespace RustyWatcher
{
    internal class Log
    {
        public static string GetName(DiscordSocketClient client) => client?.CurrentUser != null ? client.CurrentUser.Username : "Unknown";

        public static void Info(string message, DiscordSocketClient client = null)
        {
            string output = $"[{DateTime.Now}] {GetName(client)} - " + message;

            Console.WriteLine(output);
            SaveToOutput(output);
        }

        public static void Debug(string message, DiscordSocketClient client = null)
        {
            if (!Program.Data.Debug) return;
            string output = $"[DEBUG - {DateTime.Now}] {GetName(client)} - " + message;

            Console.WriteLine(output);
            SaveToOutput(output);
        }

        public static void Error(string message, DiscordSocketClient client = null)
        {
            string output = $"[{DateTime.Now}] {GetName(client)} -  Error: " + message;

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(output);
            Console.ResetColor();

            SaveToOutput(output);
        }

        public static void Error(Exception exception, DiscordSocketClient client = null)
        {
            string output = $"[{DateTime.Now}] {GetName(client)} -  Error: " + exception.Message + " Stacktrace : " + exception.StackTrace
                + (exception.InnerException != null ? "\nInner Exception" + exception.InnerException.Message + " Stacktrace : " + exception.InnerException.StackTrace : string.Empty);

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(output);
            Console.ResetColor();

            SaveToOutput(output);
        }

        public static void Warning(string message, DiscordSocketClient client = null)
        {
            string output = $"[{DateTime.Now}] {GetName(client)} - Warning: " + message;

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(output);
            Console.ResetColor();

            SaveToOutput(output);
        }

        public static void SaveToOutput(string output)
        {
            if (Program.OutputText == null) return;

            Program.OutputText.WriteLine(output);
            Program.OutputText.Flush();
        }
    }
}
