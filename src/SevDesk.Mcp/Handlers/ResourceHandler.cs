using System.Text.Json;
using SevDesk.Mcp.Client;
using SevDesk.Mcp.Configuration;
using SevDesk.Mcp.Models;

namespace SevDesk.Mcp.Handlers;

public class ResourceHandler
{
    private readonly SevDeskClient _client;
    private readonly AppConfig _config;

    public ResourceHandler(SevDeskClient client, AppConfig config)
    {
        _client = client;
        _config = config;
    }

    public List<McpResource> ListResources()
    {
        return new List<McpResource>
        {
             // We can expose some static resources or specific "recent" lists if feasible,
             // but per requirements, we should not enumerate full datasets.
             // We expose patterns via documentation or simple entry points.
             // Requirements say: "A small curated set of “entry resources” (e.g., documentation URIs, examples)"
        };
    }

    public async Task<McpReadResourceResult> ReadResourceAsync(string uri)
    {
        if (!Uri.TryCreate(uri, UriKind.Absolute, out var parsedUri) || parsedUri.Scheme != "sevdesk")
        {
            throw new Exception("Invalid URI scheme. Must be sevdesk://");
        }

        // URI patterns:
        // sevdesk://contacts/{id}
        // sevdesk://invoices/{id}
        // sevdesk://vouchers/{id}

        var path = parsedUri.Host + parsedUri.PathAndQuery; // Host acts as the first segment in sevdesk://contacts/123 -> host=contacts, path=/123
        // Actually, for custom schemes, it depends on implementation.
        // sevdesk://contacts/123 -> Host "contacts", Path "/123"

        var type = parsedUri.Host;
        var id = parsedUri.AbsolutePath.TrimStart('/');

        object? result = null;

        switch (type.ToLowerInvariant())
        {
            case "contacts":
                result = await _client.GetContactAsync(id);
                break;
            case "invoices":
                result = await _client.GetInvoiceAsync(id);
                break;
            case "vouchers":
                 result = await _client.GetVoucherAsync(id);
                break;
            default:
                throw new Exception($"Unknown resource type: {type}");
        }

        if (result == null)
        {
             throw new Exception("Resource not found");
        }

        return new McpReadResourceResult
        {
            Contents = new List<McpResourceContent>
            {
                new McpResourceContent
                {
                    Uri = uri,
                    MimeType = "application/json",
                    Text = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true })
                }
            }
        };
    }
}
