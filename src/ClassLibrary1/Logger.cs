namespace Microsoft.AzureCore.ReadyToDeploy.Vira.Plugins
{
    using System;
    using System.Text.Json;

    using global::Azure.Core;
    using global::Azure.Identity;

    internal static class Logger
    {
        /// <summary>
        /// Logs a function call with the given arguments.
        /// </summary>
        public static void LogFunctionCall(string functionName, params string[] args) => LogMessage($"Assistant (function call): {functionName}({string.Join(", ", args)})", ConsoleColor.DarkRed);

        /// <summary>
        /// Logs JSON data without truncation.
        /// </summary>
        public static void LogJson(string label, string jsonData)
        {
            try
            {
                var parsedJson = JsonSerializer.Deserialize<JsonElement>(jsonData);
                string formattedJson = JsonSerializer.Serialize(parsedJson, new JsonSerializerOptions { WriteIndented = true });
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
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ResetColor();
        }

        /// <summary>
        /// Logs an error message with details.
        /// </summary>
        public static void LogError(string message) => LogMessage($"Error: {message}", ConsoleColor.Red);
    }

    public static class CredentialHelper
    {
        /// <summary>
        /// Creates a ChainedTokenCredential to authenticate with Azure.
        /// </summary>
        internal static TokenCredential CreateChainedCredential()
            => new ChainedTokenCredential(
                new VisualStudioCredential(),
                new VisualStudioCodeCredential(),
                new AzureCliCredential(),
                new DefaultAzureCredential()
            );
    }
}
