using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AzureCore.ReadyToDeploy.Vira.Plugins;
using Microsoft.AzureCore.ReadyToDeploy.Vira.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.AzureCore.ReadyToDeploy.Vira.Tests;

[TestClass]
public class KustoPluginTests
{

    [TestCleanup]
    public void Cleanup()
    {
        // restore default delegate
        KustoHelper.ExecuteAdminCommandAsyncFunc = typeof(KustoHelper)
            .GetMethod("RealExecuteAdminCommandAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
            .CreateDelegate(typeof(Func<string, string, string, CancellationToken, Task<string>>)) as Func<string, string, string, CancellationToken, Task<string>>;
    }

    [DataTestMethod]
    [DataRow(".create function f(){print 1}")]
    [DataRow(".alter function f(){print 1}")]
    public async Task CreateKustoFunctionAsync_AllowsCreateAndAlter(string command)
    {
        // Arrange
        var plugin = new KustoPlugin();
        KustoHelper.ExecuteAdminCommandAsyncFunc = (clusterUri, db, cmd, ct) =>
            Task.FromResult("{ \"success\": true, \"message\": \"Command executed successfully.\" }");

        // Act
        string result = await plugin.CreateKustoFunctionAsync(command, CancellationToken.None);

        // Assert
        Assert.IsFalse(string.IsNullOrEmpty(result), "Response should not be empty");
        Assert.IsTrue(result.Contains("success"), $"Unexpected response: {result}");
    }
}
