namespace Microsoft.AzureCore.ReadyToDeploy.Vira.Helpers
{
    using global::Azure.Core;
    using global::Azure.Identity;

    /// <summary>
    /// Creates a ChainedTokenCredential to authenticate with Azure.
    /// </summary>
    public static class CredentialHelper
    {
        /// <summary>
        /// Creates a ChainedTokenCredential to authenticate with Azure.
        /// </summary>
        internal static TokenCredential CreateChainedCredential()
            => new ChainedTokenCredential(
                new VisualStudioCredential(),
                new VisualStudioCodeCredential(),
                new AzureCliCredential(),
                new DefaultAzureCredential()
            );
    }
}
