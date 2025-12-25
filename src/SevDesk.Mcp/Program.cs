using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using SevDesk.Mcp.Client;
using SevDesk.Mcp.Configuration;

var builder = Host.CreateApplicationBuilder(args);

// Configuration
var config = AppConfig.LoadFromEnvironment();
builder.Services.AddSingleton(config);

// Logging
builder.Logging.AddFilter("System.Net.Http.HttpClient", LogLevel.Warning);
builder.Logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);

// HttpClient
builder.Services.AddHttpClient<SevDeskClient>()
    .AddStandardResilienceHandler();

// MCP Server
builder.Services.AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly()
    .WithResourcesFromAssembly()
    .WithPromptsFromAssembly();

var app = builder.Build();

await app.RunAsync();
