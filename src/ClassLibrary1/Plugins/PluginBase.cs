namespace Microsoft.AzureCore.ReadyToDeploy.Vira.Plugins
{
    using System;
    using System.Text.Json;

    /// <summary>
    /// Provides a base implementation for plugins, including common functionalities like logging and error handling.
    /// </summary>
    public abstract class PluginBase : IPlugin
    {
        /// <summary>
        /// Provides help information about the plugin's functionalities. Must be implemented by derived classes.
        /// </summary>
        /// <returns>A JSON-formatted string detailing available functions and their usage.</returns>
        public abstract string Help();

        /// <summary>
        /// Logs the invocation of a function along with its arguments.
        /// </summary>
        /// <param name="functionName">The name of the function being called.</param>
        /// <param name="arguments">A variable number of arguments passed to the function.</param>
        protected static void LogFunctionCall(string functionName, params string[] arguments)
        {
            // Implement your logging logic here. For example:
            // Logger.LogFunctionCall(functionName, arguments);
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine($"[Function Call] {functionName} called with arguments: {string.Join(", ", arguments)}");
            Console.ResetColor();
        }

        /// <summary>
        /// Logs JSON-formatted data with an associated label.
        /// </summary>
        /// <param name="label">A label describing the JSON data.</param>
        /// <param name="jsonData">The JSON data to log.</param>
        protected static void LogJson(string label, string jsonData)
        {
            // Implement your logging logic here. For example:
            // Logger.LogJson(label, jsonData);
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"[{label}] {jsonData}");
            Console.ResetColor();
        }

        /// <summary>
        /// Logs an error message with context.
        /// </summary>
        /// <param name="context">Contextual information about where the error occurred.</param>
        /// <param name="message">The error message to log.</param>
        protected static void LogError(string context, string message)
        {
            // Implement your logging logic here. For example:
            // Logger.LogError(context, message);
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[Error - {context}] {message}");
            Console.ResetColor();
        }

        /// <summary>
        /// Logs an informational message with context.
        /// </summary>
        /// <param name="context">Contextual information about where the log is coming from.</param>
        /// <param name="message">The informational message to log.</param>
        protected static void LogInfo(string context, string message)
        {
            // Implement your logging logic here. For example:
            // Logger.LogInfo(context, message);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[Info - {context}] {message}");
            Console.ResetColor();
        }

        /// <summary>
        /// Creates a standardized error response in JSON format.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <returns>A JSON string representing the error response.</returns>
        protected static string CreateErrorResponse(string message) =>
            JsonSerializer.Serialize(new { error = true, message }, new JsonSerializerOptions { WriteIndented = true });
    }
}
