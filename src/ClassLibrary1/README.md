# Configuring and Using the New Functionality

## Vectorization and Indexing Settings

The new functionality includes vectorization and indexing settings that need to be configured in the `appsettings.json` file.

### Vectorization Settings

The vectorization settings allow you to enable or disable vectorization and specify the vectorization algorithm to be used.

```json
{
  "Vectorization": {
    "Enabled": true,
    "VectorizationAlgorithm": "your_vectorization_algorithm"
  }
}
```

### Indexing Settings

The indexing settings allow you to enable or disable indexing and specify the indexing policy for Cosmos DB.

```json
{
  "Indexing": {
    "Enabled": true,
    "IndexingPolicy": {
      "Automatic": true,
      "IndexingMode": "Consistent",
      "IncludedPaths": [ "/*" ],
      "ExcludedPaths": [ "/\"_etag\"/?" ]
    }
  }
}
```

## Using the New Functionality

After setting up the vectorization and indexing settings in the `appsettings.json` file, you can use the new functionality to save and track successful Kusto queries.

### Example: Saving a Successful Query

```csharp
string query = "Your Kusto query here";
string result = await KustoHelper.ExecuteKustoQueryAsync(clusterUri, databaseName, query);
```

The `ExecuteKustoQueryAsync` method will automatically call `SaveSuccessfulQuery` to save the query and its result to both a local file and Cosmos DB.

### Example: Retrieving Saved Queries

You can retrieve the saved queries from the local file or Cosmos DB as needed. The saved queries are stored in JSON format, making it easy to parse and use them in your application.

### Additional Information

For more details on configuring and using the new functionality, refer to the documentation provided in the `src/ClassLibrary1` folder.
