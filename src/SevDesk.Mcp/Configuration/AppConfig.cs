namespace SevDesk.Mcp.Configuration;

public class AppConfig
{
    public required string SevDeskApiToken { get; set; }
    public string SevDeskBaseUrl { get; set; } = "https://my.sevdesk.de/api/v1";
    public string McpMode { get; set; } = "stdio";
    public int McpHttpPort { get; set; } = 8080;
    public bool AllowWriteTools { get; set; } = false;
    public int HttpTimeoutSeconds { get; set; } = 30;
    public int DefaultPageSize { get; set; } = 50;
    public int MaxPageSize { get; set; } = 100;
    public string LogLevel { get; set; } = "Information";
    public string SevDeskUserAgent { get; set; } = "sevdesk-mcp/1.0";

    public static AppConfig LoadFromEnvironment()
    {
        var token = Environment.GetEnvironmentVariable("SEVDESK_API_TOKEN");
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new InvalidOperationException("SEVDESK_API_TOKEN environment variable is required.");
        }

        var config = new AppConfig
        {
            SevDeskApiToken = token,
            SevDeskBaseUrl = Environment.GetEnvironmentVariable("SEVDESK_BASE_URL") ?? "https://my.sevdesk.de/api/v1",
            McpMode = Environment.GetEnvironmentVariable("MCP_MODE") ?? "stdio",
            AllowWriteTools = bool.TryParse(Environment.GetEnvironmentVariable("ALLOW_WRITE_TOOLS"), out var allowWrite) && allowWrite,
            LogLevel = Environment.GetEnvironmentVariable("LOG_LEVEL") ?? "Information",
            SevDeskUserAgent = Environment.GetEnvironmentVariable("SEVDESK_USER_AGENT") ?? "sevdesk-mcp/1.0"
        };

        if (int.TryParse(Environment.GetEnvironmentVariable("MCP_HTTP_PORT"), out var port))
        {
            config.McpHttpPort = port;
        }

        if (int.TryParse(Environment.GetEnvironmentVariable("HTTP_TIMEOUT_SECONDS"), out var timeout))
        {
            config.HttpTimeoutSeconds = timeout;
        }

        if (int.TryParse(Environment.GetEnvironmentVariable("DEFAULT_PAGE_SIZE"), out var defaultPageSize))
        {
            config.DefaultPageSize = defaultPageSize;
        }

        if (int.TryParse(Environment.GetEnvironmentVariable("MAX_PAGE_SIZE"), out var maxPageSize))
        {
            config.MaxPageSize = maxPageSize;
        }

        config.Validate();

        return config;
    }

    private void Validate()
    {
        if (DefaultPageSize <= 0) throw new InvalidOperationException("DEFAULT_PAGE_SIZE must be positive.");
        if (MaxPageSize <= 0) throw new InvalidOperationException("MAX_PAGE_SIZE must be positive.");
        if (DefaultPageSize > MaxPageSize) throw new InvalidOperationException("DEFAULT_PAGE_SIZE must be less than or equal to MAX_PAGE_SIZE.");
        if (HttpTimeoutSeconds < 5 || HttpTimeoutSeconds > 120) throw new InvalidOperationException("HTTP_TIMEOUT_SECONDS must be between 5 and 120.");

        var validModes = new[] { "stdio", "http" };
        if (!validModes.Contains(McpMode.ToLowerInvariant()))
        {
             throw new InvalidOperationException($"MCP_MODE must be one of: {string.Join(", ", validModes)}");
        }
    }
}
