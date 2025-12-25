using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using SevDesk.Mcp.Client;
using SevDesk.Mcp.Configuration;
using SevDesk.Mcp.Models;

namespace SevDesk.Mcp.Handlers;

[McpServerResourceType]
public class SevDeskResources
{
    private readonly SevDeskClient _client;
    private readonly AppConfig _config;

    public SevDeskResources(SevDeskClient client, AppConfig config)
    {
        _client = client;
        _config = config;
    }

    [McpServerResource("sevdesk://contacts/{id}", Description = "Get a contact by ID")]
    public async Task<string> GetContactResource(string id)
    {
        var result = await _client.GetContactAsync(id);
        if (result == null) throw new Exception("Contact not found");
        return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
    }

    [McpServerResource("sevdesk://invoices/{id}", Description = "Get an invoice by ID")]
    public async Task<string> GetInvoiceResource(string id)
    {
        var result = await _client.GetInvoiceAsync(id);
        if (result == null) throw new Exception("Invoice not found");
        return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
    }

    [McpServerResource("sevdesk://vouchers/{id}", Description = "Get a voucher by ID")]
    public async Task<string> GetVoucherResource(string id)
    {
        var result = await _client.GetVoucherAsync(id);
        if (result == null) throw new Exception("Voucher not found");
        return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
    }
}
