namespace Microsoft.AzureCore.ReadyToDeploy.Vira
{
    using System;

    namespace Microsoft.AzureCore.ReadyToDeploy.Vira
    {
        using System.Threading.Tasks;

        public class LargeRequestHelper
        {
            private readonly int MaxTokenSize = 100000;  // Maximum token size per GPT request
            private readonly int MaxRowsPerRequest = 500;  // Maximum rows to send in one request
            private string _fullOutput = string.Empty;  // Buffer to store the final output

            /// <summary>
            /// Processes a large data set in batches and interacts with GPT to determine how much more can be processed.
            /// </summary>
            /// <param name="query">The Kusto query to execute.</param>
            /// <param name="clusterUri">The URI of the Kusto cluster.</param>
            /// <param name="databaseName">The Kusto database name.</param>
            /// <returns>Returns the final processed output as a string.</returns>
            public async Task<string> ProcessLargeDataSetAsync(string query, string clusterUri, string databaseName)
            {
                int pageSize = MaxRowsPerRequest;
                int pageIndex = 0;
                bool continueProcessing = true;

                while (continueProcessing)
                {
                    // Fetch the next batch of data
                    string batchResult = await KustoHelper.ExecuteKustoQueryForClusterAsync(clusterUri, databaseName, query, true, pageSize, pageIndex);

                    if (string.IsNullOrEmpty(batchResult))
                        break;  // No more data, end the loop

                    // Append to the full output
                    _fullOutput += batchResult;

                    // Check if GPT can handle more data
                    bool canProcessMore = AskGPTIfMoreDataCanBeProcessed(_fullOutput);

                    if (!canProcessMore)
                    {
                        // Ask user if they want to continue processing more chunks
                        continueProcessing = AskUserIfTheyWantToContinue();
                    }
                    else
                    {
                        // Increase page index to fetch the next batch
                        pageIndex++;
                    }
                }

                return _fullOutput;
            }

            /// <summary>
            /// Asks GPT whether it can handle more rows based on the current data size.
            /// </summary>
            private bool AskGPTIfMoreDataCanBeProcessed(string currentOutput) =>
                // Here, send the currentOutput to GPT and ask if it can handle more data.
                // For simplicity, let's simulate GPT's decision-making:
                currentOutput.Length < MaxTokenSize;

            /// <summary>
            /// Asks the user if they want to continue processing more chunks.
            /// </summary>
            private bool AskUserIfTheyWantToContinue()
            {
                // Notify the user about the current batch, chunks processed, and ask if they want to continue
                Console.WriteLine($"Processed {_fullOutput.Length} tokens so far. Do you want to process more? (y/n)");
                string input = Console.ReadLine();
                return input?.ToLower() == "y";
            }
        }
    }
}
