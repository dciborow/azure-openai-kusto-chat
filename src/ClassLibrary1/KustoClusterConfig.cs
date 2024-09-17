namespace Microsoft.AzureCore.ReadyToDeploy.Vira
{
    /// <summary>
    /// Represents a configuration for a Kusto cluster, including its name, URI, associated database, and description.
    /// </summary>
    public class KustoClusterConfig
    {
        public string Name { get; }
        public string Uri { get; }
        public string DatabaseName { get; }
        public string Description { get; }

        public KustoClusterConfig(string name, string uri, string databaseName, string description)
        {
            Name = name;
            Uri = uri;
            DatabaseName = databaseName;
            Description = description;
        }
    }
}
