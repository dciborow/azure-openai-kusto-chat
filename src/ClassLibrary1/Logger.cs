namespace Microsoft.AzureCore.ReadyToDeploy.Vira
{
    using System;
    using System.Text.Json;

    internal static class Logger
    {
        /// <summary>
        /// Logs a function call with the given arguments.
        /// </summary>
        public static void LogFunctionCall(string functionName, params string[] args) => LogMessage($"Assistant (function call): {functionName}({string.Join(", ", args)})", ConsoleColor.DarkRed);

        /// <summary>
        /// Logs JSON data with configurable truncation length.
        /// </summary>
        public static void LogJson(string label, object data, int truncateLength = 200)
        {
            string jsonString = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            string output = jsonString.Length > truncateLength ? jsonString.Substring(0, truncateLength) + "..." : jsonString;

            LogMessage($"\t{label}: {output}", ConsoleColor.DarkMagenta);
        }

        /// <summary>
        /// Logs a message with a specific color.
        /// </summary>
        public static void LogMessage(string message, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ResetColor();
        }

        /// <summary>
        /// Logs the query to the console.
        /// </summary>
        internal static void LogQuery(string query)
        {
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine($"Assistant (Query): {query}");
            Console.ResetColor();
        }
    }
}
