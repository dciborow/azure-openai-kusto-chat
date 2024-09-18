var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.Microsoft_AzureCore_ReadyToDeploy_ViraUI_Server>("microsoft-azurecore-readytodeploy-viraui-server");

builder.Build().Run();
