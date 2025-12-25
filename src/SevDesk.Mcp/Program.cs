using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SevDesk.Mcp.Client;
using SevDesk.Mcp.Configuration;
using SevDesk.Mcp.Models;
using SevDesk.Mcp.Server;
using SevDesk.Mcp.Handlers;

var services = new ServiceCollection();

try
{
    var config = AppConfig.LoadFromEnvironment();
    services.AddSingleton(config);

    services.AddLogging(builder =>
    {
        builder.AddFilter("System.Net.Http.HttpClient", LogLevel.Warning);
        builder.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);
    });

    services.AddHttpClient<SevDeskClient>()
        .AddStandardResilienceHandler(); // Adds standard resilience policies (retry, circuit breaker, timeout, rate limiter)

    services.AddScoped<ToolHandler>();
    services.AddScoped<ResourceHandler>();

    services.AddSingleton<McpServer>(sp =>
    {
        var inputStream = Console.OpenStandardInput();
        var outputStream = Console.OpenStandardOutput();
        return new McpServer(inputStream, outputStream, sp);
    });

    var serviceProvider = services.BuildServiceProvider();
    var server = serviceProvider.GetRequiredService<McpServer>();

    // We need to resolve scoped services within the request context if possible,
    // but for this simple console app, we can just resolve them from the root provider or create a scope per request.
    // Since everything is effectively singleton in this context (config, client), we can just use the provider.
    // However, best practice is to create a scope.

    server.OnRequest += async (request) =>
    {
        using var scope = serviceProvider.CreateScope();
        var toolHandler = scope.ServiceProvider.GetRequiredService<ToolHandler>();
        var resourceHandler = scope.ServiceProvider.GetRequiredService<ResourceHandler>();
        var appConfig = scope.ServiceProvider.GetRequiredService<AppConfig>();

        switch (request.Method)
        {
            case "initialize":
                return new McpInitializeResult
                {
                    ProtocolVersion = "2024-11-05",
                    Capabilities = new ServerCapabilities
                    {
                        Tools = new object(),
                        Resources = new object()
                    },
                    ServerInfo = new ServerInfo
                    {
                         Name = "sevdesk-mcp",
                         Version = "1.0.0"
                    }
                };
            case "tools/list":
                return new
                {
                    tools = toolHandler.ListTools()
                };
            case "tools/call":
                 if (request.Params is JsonElement paramsElem && paramsElem.TryGetProperty("name", out var nameProp))
                 {
                     var toolName = nameProp.GetString();
                     var args = new Dictionary<string, object>();

                     if (paramsElem.TryGetProperty("arguments", out var argsProp) && argsProp.ValueKind == JsonValueKind.Object)
                     {
                         // Convert JsonElement object to Dictionary<string, object>
                         foreach (var property in argsProp.EnumerateObject())
                         {
                             // This is a simplification. Ideally we need recursive conversion.
                             // But for now we just pass the JsonElement down or handle it in the handler.
                             // Actually, the handler logic I wrote expects the dictionary values to be JsonElement or primitives.
                             // Let's rely on JsonElement in the handler or deserialization.
                             // But `McpCallToolRequest` defined Arguments as Dictionary<string, object>.
                             // Let's improve deserialization or just pass the raw JsonElement if we change the interface.

                             // Better approach: Deserialize arguments to Dictionary<string, object> using JsonSerializer
                             // However, System.Text.Json deserializes `object` as `JsonElement`.

                             args[property.Name] = property.Value;
                         }
                     }
                     return await toolHandler.CallToolAsync(toolName, args);
                 }
                 throw new Exception("Tool name missing");

            case "resources/list":
                 return new
                 {
                     resources = resourceHandler.ListResources()
                 };
            case "resources/read":
                 if (request.Params is JsonElement resParams && resParams.TryGetProperty("uri", out var uriProp))
                 {
                     return await resourceHandler.ReadResourceAsync(uriProp.GetString()!);
                 }
                 throw new Exception("URI missing");
            case "ping":
                return new { };
            default:
                throw new Exception("Method not supported");
        }
    };

    await server.RunAsync();

}
catch (Exception ex)
{
    Console.Error.WriteLine($"Critical error: {ex.Message}");
    Environment.Exit(1);
}
