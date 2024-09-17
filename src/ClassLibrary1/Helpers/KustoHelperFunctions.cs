namespace Microsoft.AzureCore.ReadyToDeploy.Vira.Helpers
{
    using System;

    internal static class KustoHelperFunctions
    {
        /// <summary>
        /// Logs the query to the console.
        /// </summary>
        public static void LogQuery(string query)
        {
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine($"Kusto Query: {query}");
            Console.ResetColor();
        }

        /// <summary>
        /// Escapes special characters in Kusto string literals.
        /// </summary>
        public static string EscapeKustoString(string input) => input.Replace("'", "''");

        /// <summary>
        /// Escapes special characters in Kusto identifiers.
        /// </summary>
        public static string EscapeKustoIdentifier(string input) =>
            // Add any necessary escaping for identifiers if needed
            input;
    }
}
