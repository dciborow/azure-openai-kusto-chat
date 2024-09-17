namespace Microsoft.AzureCore.ReadyToDeploy.Vira.Plugins
{
    using System;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Microsoft.Azure.Cosmos;

    /// <summary>
    /// Provides a base implementation for plugins, including common functionalities like logging and error handling.
    /// </summary>
    public abstract class PluginBase : IPlugin
    {
        /// <summary>
        /// Initializes the plugin. Can be overridden by derived classes for custom initialization.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public virtual Task InitializeAsync() =>
            // Default implementation (can be overridden)
            Task.CompletedTask;

        /// <summary>
        /// Executes the plugin's primary function. Must be implemented by derived classes.
        /// </summary>
        /// <param name="parameters">Parameters required for execution.</param>
        /// <returns>A task representing the asynchronous operation, returning the result as a string.</returns>
        public abstract Task<string> ExecuteAsync(params string[] parameters);

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
        protected void LogFunctionCall(string functionName, params string[] arguments)
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
        protected void LogJson(string label, string jsonData)
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
        protected void LogError(string context, string message)
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
        protected void LogInfo(string context, string message)
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
        protected string CreateErrorResponse(string message) =>
            JsonSerializer.Serialize(new { error = true, message }, new JsonSerializerOptions { WriteIndented = true });

        /// <summary>
        /// Vectorizes the given data.
        /// </summary>
        /// <param name="data">The data to vectorize.</param>
        /// <returns>The vectorized data.</returns>
        protected string VectorizeData(string data)
        {
            // Implement your vectorization logic here
            return data;
        }

        /// <summary>
        /// Creates an index in Cosmos DB for optimized search queries.
        /// </summary>
        /// <param name="cosmosDbConnectionString">The Cosmos DB connection string.</param>
        /// <param name="databaseName">The name of the database.</param>
        /// <param name="containerName">The name of the container.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        protected async Task CreateCosmosDbIndexAsync(string cosmosDbConnectionString, string databaseName, string containerName)
        {
            using (var cosmosClient = new CosmosClient(cosmosDbConnectionString))
            {
                var database = await cosmosClient.CreateDatabaseIfNotExistsAsync(databaseName);
                var container = await database.Database.CreateContainerIfNotExistsAsync(containerName, "/id");

                var indexingPolicy = new IndexingPolicy
                {
                    Automatic = true,
                    IndexingMode = IndexingMode.Consistent,
                    IncludedPaths = { new IncludedPath { Path = "/*" } },
                    ExcludedPaths = { new ExcludedPath { Path = "/\"_etag\"/?" } }
                };

                await container.Container.ReplaceContainerAsync(new ContainerProperties
                {
                    Id = containerName,
                    PartitionKeyPath = "/id",
                    IndexingPolicy = indexingPolicy
                });
            }
        }
    }
}
