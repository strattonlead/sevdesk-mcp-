using System.Text.Json;
using SevDesk.Mcp.Models;
using System.Text;

namespace SevDesk.Mcp.Server;

public class McpServer
{
    private readonly Stream _inputStream;
    private readonly Stream _outputStream;
    private readonly IServiceProvider _serviceProvider;
    private readonly JsonSerializerOptions _jsonOptions;

    public event Func<JsonRpcRequest, Task<object?>> OnRequest;

    public McpServer(Stream inputStream, Stream outputStream, IServiceProvider serviceProvider)
    {
        _inputStream = inputStream;
        _outputStream = outputStream;
        _serviceProvider = serviceProvider;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(_inputStream, Encoding.UTF8);

        while (!cancellationToken.IsCancellationRequested && !reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(line)) continue;

            try
            {
                var request = JsonSerializer.Deserialize<JsonRpcRequest>(line, _jsonOptions);
                if (request != null)
                {
                    // Handle Request
                    object? result = null;
                    JsonRpcError? error = null;

                    try
                    {
                        if (OnRequest != null)
                        {
                            result = await OnRequest(request);
                        }
                        else
                        {
                            error = new JsonRpcError { Code = -32601, Message = "Method not found" };
                        }
                    }
                    catch (Exception ex)
                    {
                         error = new JsonRpcError { Code = -32000, Message = ex.Message, Data = ex.StackTrace };
                    }

                    if (request.Id != null)
                    {
                        var response = new JsonRpcResponse
                        {
                            Jsonrpc = "2.0",
                            Result = result,
                            Error = error,
                            Id = request.Id
                        };

                        var responseJson = JsonSerializer.Serialize(response, _jsonOptions);
                        await SendLineAsync(responseJson);
                    }
                }
            }
            catch (JsonException)
            {
                 // Invalid JSON
                  var errorResponse = new JsonRpcResponse
                        {
                            Jsonrpc = "2.0",
                            Error = new JsonRpcError { Code = -32700, Message = "Parse error" },
                            Id = null
                        };
                   var responseJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                   await SendLineAsync(responseJson);
            }
        }
    }

    private async Task SendLineAsync(string line)
    {
        var bytes = Encoding.UTF8.GetBytes(line + "\n");
        await _outputStream.WriteAsync(bytes);
        await _outputStream.FlushAsync();
    }
}
