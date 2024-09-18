namespace Microsoft.AzureCore.ReadyToDeploy.Vira.Plugins
{
    using System;
    using System.ComponentModel;
    using System.IO;
    using System.Threading.Tasks;

    using Microsoft.SemanticKernel;

    /// <summary>
    /// Represents a plugin for logging user feedback, bugs, and internal errors or kernel improvements by appending them to files on disk.
    /// </summary>
    public class MetaPlugin : PluginBase
    {
        public override string Help() => throw new NotImplementedException();

        [KernelFunction("read_file")]
        [Description("Reads the content of a specified file and returns it as a string. Used to help determine what updates can be made to files to help resolve issues.")]
        [return: Description("Returns the content of the file as a string.")]
        public async Task<string> ReadFileAsync(
        [Description("The full path of the file to read, including the filename and extension.")] string filePath,
        CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNullOrWhiteSpace(filePath);
            LogFunctionCall("KustoPluginVNext.ReadFileAsync", filePath);

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("The specified file was not found.", filePath);
            }

            // Read the file contents asynchronously
            return await File.ReadAllTextAsync(filePath, cancellationToken);
        }
    }
}
