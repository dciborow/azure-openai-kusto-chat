
# Vira Kusto Plugin and LargeRequestHelper

## Overview

This project provides a set of Semantic Kernel plugins and utilities to integrate Kusto queries with GPT-powered AI assistants. It includes functionality for processing large data sets, handling large Kusto query responses, and interacting with various Kusto clusters. The **LargeRequestHelper** ensures the system can handle large responses efficiently by batching requests and querying GPT for next steps.

## Prerequisites

1. **.NET SDK**: You need .NET 6.0 or higher installed on your system. You can download it [here](https://dotnet.microsoft.com/download).
2. **Azure Subscription**: Ensure you have access to Azure resources and permissions to query the specified Kusto clusters.
3. **Kusto SDK**: This project depends on the **Azure Data Explorer** Kusto SDK. Make sure the `Kusto.Data` and `Kusto.Net.Client` libraries are installed.
4. **OpenAI/Azure OpenAI Access**: You need an API key and access to the OpenAI services that your assistant will leverage. 

## Setup

### 1. Clone the Repository

```bash
az login

git clone https://github.com/azure-core/vira-garage.git
cd src/applications/SimpleViraChat
```

### 2. Install Dependencies

To restore the necessary dependencies, run the following command:

```bash
dotnet restore
```


### 3. Running the Project

You can run the project by executing the following command:

```bash
dotnet run
```

Try this series of prompts.
```
I have some questons... 1) What tables in Safefly's Kusto contain information about Safefly requests? 2) What can you tell me about the most recent records for the SafeFly request 4254 from the r2d partition? 3)  Can you use the ADO Build ID there to look up more information about the build?
```

#### 3.1 Single Script
```bash
az login

### 1. Clone the Repository
git clone https://github.com/azure-core/vira-garage.git
cd src/applications/SimpleViraChat

### 2. Install Dependencies
dotnet restore

### 3. Running the Project
dotnet run

# Now, try these prompts one at a time or all together:
#
# I have some questons...
# - What tables in Safefly's Kusto contain information about Safefly requests?`
# - What can you tell me about the most recent records for the SafeFly request 4254 from the r2d partition?`
# - `Can you use the ADO Build ID there to look up more information about the build?`
#
# I have some questons... 1) What tables in Safefly's Kusto contain information about Safefly requests? 2) What can you tell me about the most recent records for the SafeFly request 4254 from the r2d partition? 3)  Can you use the ADO Build ID there to look up more information about the build?
```

### 4. Development

#### 4.1 Kusto Cluster Configuration

The project connects to multiple Kusto clusters through a **Dictionary** of cluster configurations. These configurations are set up in the `KustoPlugin` class.

```csharp
_clusters = new Dictionary<string, KustoClusterConfig>
{
    { "safefly", new KustoClusterConfig("SafeFly Cluster", "https://safeflycluster.westus.kusto.windows.net/", "safefly", "Deployment requests linked with Build ID") },
    { "copilot", new KustoClusterConfig("Copilot Dev Feedback", "https://az-copilot-kusto.eastus.kusto.windows.net/", "copilotDevFeedback", "Copilot Risk reports for SafeFly requests") },
    { "azuredevops", new KustoClusterConfig("Azure DevOps", "https://1es.kusto.windows.net/", "AzureDevOps", "Builds, Pull Requests, Commits, and Work Items") }
};
```

Feel free to modify the `KustoPlugin` with the clusters you plan to query by adding additional entries in the dictionary.

#### 4.2 Azure Credentials

The project uses **Azure Chained Token Credential** to authenticate and access Kusto clusters. This is set up in the `KustoHelper` class.

```csharp
internal static TokenCredential CreateChainedCredential()
    => new ChainedTokenCredential(
        new VisualStudioCredential(),
        new VisualStudioCodeCredential(),
        new AzureCliCredential(),
        new DefaultAzureCredential()
    );
```

Make sure you are logged in to Azure using one of these methods or configure a service principal.

To authenticate with the Azure CLI, run the following:

```bash
az login
```


### 5. Testing the Kusto Queries

To test the integration, you can use the provided `KustoPlugin` to run queries against the clusters you've configured.

Example Kusto query for pulling data from a table:

```csharp
[KernelFunction("query_kusto_database")]
public async Task<string> QueryKustoDatabaseAsync(string clusterKey, string query)
{
    // Call KustoHelper to execute a query for the specific cluster and database
    var result = await KustoHelper.ExecuteKustoQueryForClusterAsync(cluster.Uri, cluster.DatabaseName, query);
    return result ?? "No results found.";
}
```

The `QueryKustoDatabaseAsync` function runs a query for the specified cluster and returns the results in JSON format.

### 6. Handling Large Responses

The **LargeRequestHelper** is designed to help when dealing with large Kusto query results. It batches the requests to ensure GPT can handle responses efficiently.

To process a large dataset (e.g., for summarizing PR descriptions):

```csharp
public async Task<string> ProcessPRDescriptionsInBatchesAsync(string buildId, string clusterKey = "azuredevops")
{
    string query = $@"
        PullRequest
        | where BuildId == '{buildId}'
        | project Description";

    return await _largeRequestHelper.ProcessLargeDataSetAsync(query, cluster.Uri, cluster.DatabaseName);
}
```

The **LargeRequestHelper** ensures that large queries are handled in manageable chunks.

## License

This project is licensed under the MIT License. See the `LICENSE` file for more details.
