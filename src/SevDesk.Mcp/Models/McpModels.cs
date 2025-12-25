namespace SevDesk.Mcp.Models;

public record JsonRpcRequest
{
    public required string Jsonrpc { get; init; } = "2.0";
    public required string Method { get; init; }
    public object? Params { get; init; }
    public object? Id { get; init; }
}

public record JsonRpcResponse
{
    public required string Jsonrpc { get; init; } = "2.0";
    public object? Result { get; init; }
    public JsonRpcError? Error { get; init; }
    public object? Id { get; init; }
}

public record JsonRpcError
{
    public required int Code { get; init; }
    public required string Message { get; init; }
    public object? Data { get; init; }
}

// MCP Specific Models
public record McpInitializeParams
{
    public required string ProtocolVersion { get; init; }
    public required ClientCapabilities Capabilities { get; init; }
    public required ClientInfo ClientInfo { get; init; }
}

public record ClientCapabilities
{
    public object? Roots { get; init; }
    public object? Sampling { get; init; }
}

public record ClientInfo
{
    public required string Name { get; init; }
    public required string Version { get; init; }
}

public record McpInitializeResult
{
    public required string ProtocolVersion { get; init; }
    public required ServerCapabilities Capabilities { get; init; }
    public required ServerInfo ServerInfo { get; init; }
}

public record ServerCapabilities
{
    public object? Logging { get; init; }
    public object? Tools { get; init; }
    public object? Resources { get; init; }
}

public record ServerInfo
{
    public required string Name { get; init; }
    public required string Version { get; init; }
}

public record McpTool
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required object InputSchema { get; init; }
}

public record McpResource
{
    public required string Uri { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public string? MimeType { get; init; }
}

public record McpCallToolRequest
{
    public required string Name { get; init; }
    public Dictionary<string, object>? Arguments { get; init; }
}

public record McpCallToolResult
{
    public List<McpContent> Content { get; init; } = new();
    public bool IsError { get; init; }
}

public record McpContent
{
    public required string Type { get; init; }
    public string? Text { get; init; }
    // For images or blobs
    public string? Data { get; init; }
    public string? MimeType { get; init; }
}

public record McpReadResourceResult
{
     public List<McpResourceContent> Contents { get; init; } = new();
}

public record McpResourceContent
{
    public required string Uri { get; init; }
    public string? MimeType { get; init; }
    public string? Text { get; init; }
    public string? Blob { get; init; }
}
