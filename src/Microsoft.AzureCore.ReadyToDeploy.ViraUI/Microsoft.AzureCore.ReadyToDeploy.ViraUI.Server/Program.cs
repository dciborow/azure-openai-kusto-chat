
namespace Microsoft.AzureCore.ReadyToDeploy.ViraUI.Server;

using Microsoft.AzureCore.ReadyToDeploy.Vira;
using Microsoft.AzureCore.ReadyToDeploy.ViraUI.Server.Hubs;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.AddServiceDefaults();

        builder.Services.AddSignalR();

        // Add services to the container.
        builder.Services.AddControllers();
        builder.Services.AddCors();

        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        var app = builder.Build();

        app.MapDefaultEndpoints();
        app.MapHub<ChatHub>("/hub");

        app.UseDefaultFiles();
        app.UseStaticFiles();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();

        app.UseAuthorization();


        app.MapControllers();

        app.MapFallbackToFile("/index.html");

        app.Run();
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers();
        services.AddCors(); // Enable CORS
                            // Register ClearwaterChatService as a singleton or scoped service
        services.AddSingleton<ClearwaterChatService>(provider
            => new ClearwaterChatService("gpt-4o-mini", "https://gpt-review.openai.azure.com"));
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseCors(builder =>
            builder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader());

        app.UseRouting();
        app.UseEndpoints(endpoints => endpoints.MapControllers());
    }
}
