namespace Microsoft.AzureCore.ReadyToDeploy.Vira.Helpers
{
    using System;
    using System.Text.Json;

    /// <summary>
    /// Provides methods for logging various types of messages with color coding.
    /// </summary>
    internal static class LoggerHelper
    {
        /// <summary>
        /// Logs a function call with the given arguments.
        /// </summary>
        public static void LogFunctionCall(string functionName, params string[] args) => LogMessage($"# Assistant (function call): {functionName}({string.Join(", ", args)})", ConsoleColor.DarkRed);

        /// <summary>
        /// Logs JSON data without truncation.
        /// </summary>
        public static void LogJson(string label, string jsonData)
        {
            try
            {
                var parsedJson = JsonSerializer.Deserialize<JsonElement>(jsonData);
                string formattedJson = string.Empty;

                formattedJson = JsonSerializer.Serialize(parsedJson, new JsonSerializerOptions { WriteIndented = true });


                LogMessage($"\t{label}: {formattedJson}", ConsoleColor.DarkMagenta);
            }
            catch (JsonException)
            {
                LogMessage($"\t{label}: {jsonData}", ConsoleColor.DarkMagenta);
            }
        }

        /// <summary>
        /// Logs a message with a specific color.
        /// </summary>
        public static void LogMessage(string message, ConsoleColor color)
        {
            var maxLength = 1200; // 10 lines at 120
            var sampleLength = 240; // 2 lines at 120

            if (message.Length > maxLength)
            {
                // Truncate the message and add an ellipsis to indicate truncation
                message = string.Concat(message.AsSpan(0, sampleLength), "...");
            }

            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ResetColor();
        }

        /// <summary>
        /// Logs an error message with details.
        /// </summary>
        public static void LogError(string message) => LogMessage($"Error: {message}", ConsoleColor.Red);
    }
}
