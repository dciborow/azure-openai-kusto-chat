namespace Microsoft.AzureCore.ReadyToDeploy.Vira.Plugins
{
    /// <summary>
    /// Represents the configuration details of a Kusto cluster.
    /// </summary>
    public class KustoClusterConfig
    {
        public string Key { get; }
        public string Uri { get; }
        public string DatabaseName { get; }
        public string Description { get; }

        public KustoClusterConfig(string key, string uri, string databaseName, string description)
        {
            Key = key;
            Uri = uri;
            DatabaseName = databaseName;
            Description = description;
        }
    }
}
