# ClassLibrary1

## Save and Track Successful Kusto Queries

This library now includes functionality to save and track successful Kusto queries. The queries can be stored locally or in CosmosDB based on the configuration.

### Configuration

To configure the storage type, update the `appsettings.json` file with the desired storage type and settings.

#### Local Storage

To store queries locally, set the `StorageType` to `local` and provide the file path.

```json
{
  "StorageType": "local",
  "LocalStorage": {
    "FilePath": "queryHistory.json"
  }
}
```

#### CosmosDB Storage

To store queries in CosmosDB, set the `StorageType` to `cosmosdb` and provide the connection details.

```json
{
  "StorageType": "cosmosdb",
  "CosmosDB": {
    "ConnectionString": "your-cosmosdb-connection-string",
    "DatabaseName": "your-database-name",
    "ContainerName": "your-container-name"
  }
}
```

### Usage

The `SaveSuccessfulQuery` method in `Utilities.cs` is used to save successful queries. This method is called after a successful query execution in `ClearwaterChatService.cs` and `Plugins.cs`.

### Example

Here is an example of how to use the `SaveSuccessfulQuery` method:

```csharp
await KustoHelper.SaveSuccessfulQuery(query, result);
```

This will save the query and its result based on the configured storage type.
