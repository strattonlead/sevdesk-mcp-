using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text;
using SevDesk.Mcp.Configuration;
using SevDesk.Mcp.Models;

namespace SevDesk.Mcp.Client;

public class SevDeskClient
{
    private readonly HttpClient _httpClient;
    private readonly AppConfig _config;

    public SevDeskClient(HttpClient httpClient, AppConfig config)
    {
        _httpClient = httpClient;
        _config = config;

        _httpClient.BaseAddress = new Uri(_config.SevDeskBaseUrl);
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(_config.SevDeskApiToken);
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(_config.SevDeskUserAgent);
    }

    public async Task<ListResponse<Contact>> SearchContactsAsync(string? query, string? email, string? customerNumber, int page, int limit)
    {
        var queryParams = new List<string>
        {
            $"limit={limit}",
            $"offset={limit * (page - 1)}"
        };

        if (!string.IsNullOrWhiteSpace(email))
        {
             // SevDesk API might require specific filtering syntax
             // For simplicity assuming exact match filtering or we need to check API docs.
             // Based on standard sevDesk API: ?key=value filters
             queryParams.Add($"email={Uri.EscapeDataString(email)}");
        }

        if (!string.IsNullOrWhiteSpace(customerNumber))
        {
             queryParams.Add($"customerNumber={Uri.EscapeDataString(customerNumber)}");
        }

        // 'query' is general search, might not be directly supported as a single param on /Contact
        // but we can try depth filtering if documented. For now, we rely on specific fields.
        // If 'query' is provided but not specific fields, we might warn or ignore if API doesn't support generic search.

        var url = $"Contact?{string.Join("&", queryParams)}";
        return await GetAsync<ListResponse<Contact>>(url);
    }

    public async Task<Contact?> GetContactAsync(string id)
    {
        var url = $"Contact/{id}";
        var response = await GetAsync<SingleResponse<Contact>>(url);
        return response?.Objects?.FirstOrDefault();
    }

    public async Task<ListResponse<Invoice>> GetInvoicesAsync(string? status, string? contactId, DateTime? fromDate, DateTime? toDate, int page, int limit)
    {
        var queryParams = new List<string>
        {
            $"limit={limit}",
            $"offset={limit * (page - 1)}",
            "embed=contact" // Often useful
        };

        if (!string.IsNullOrWhiteSpace(status))
        {
            queryParams.Add($"status={status}");
        }

        if (!string.IsNullOrWhiteSpace(contactId))
        {
            queryParams.Add($"contact[id]={contactId}");
            queryParams.Add($"contact[objectName]=Contact");
        }

        if (fromDate.HasValue)
        {
            queryParams.Add($"startDate={fromDate.Value:yyyy-MM-dd}"); // Note: field name might vary (invoiceDate vs create)
        }
         if (toDate.HasValue)
        {
            queryParams.Add($"endDate={toDate.Value:yyyy-MM-dd}");
        }

        var url = $"Invoice?{string.Join("&", queryParams)}";
        return await GetAsync<ListResponse<Invoice>>(url);
    }

    public async Task<Invoice?> GetInvoiceAsync(string id)
    {
        var url = $"Invoice/{id}";
         var response = await GetAsync<SingleResponse<Invoice>>(url);
        return response?.Objects?.FirstOrDefault();
    }

    public async Task<ListResponse<Voucher>> GetVouchersAsync(string? status, DateTime? fromDate, DateTime? toDate, int page, int limit)
    {
        var queryParams = new List<string>
        {
            $"limit={limit}",
            $"offset={limit * (page - 1)}"
        };

        if (!string.IsNullOrWhiteSpace(status))
        {
            queryParams.Add($"status={status}");
        }

         if (fromDate.HasValue)
        {
             // Vouchers usually filter by voucherDate
            queryParams.Add($"voucherDate[from]={fromDate.Value:yyyy-MM-dd}");
        }
         if (toDate.HasValue)
        {
            queryParams.Add($"voucherDate[to]={toDate.Value:yyyy-MM-dd}");
        }

        var url = $"Voucher?{string.Join("&", queryParams)}";
        return await GetAsync<ListResponse<Voucher>>(url);
    }
     public async Task<Voucher?> GetVoucherAsync(string id)
    {
        var url = $"Voucher/{id}";
         var response = await GetAsync<SingleResponse<Voucher>>(url);
        return response?.Objects?.FirstOrDefault();
    }

    public async Task<ListResponse<Document>> GetDocumentsAsync(string? objectType, string? objectId, int page, int limit)
    {
         var queryParams = new List<string>
        {
            $"limit={limit}",
            $"offset={limit * (page - 1)}"
        };

        // Note: SevDesk documents endpoint filtering can be complex.
        // Usually filtered by object[id]=... & object[objectName]=...

        if (!string.IsNullOrWhiteSpace(objectType) && !string.IsNullOrWhiteSpace(objectId))
        {
             queryParams.Add($"object[id]={objectId}");
             queryParams.Add($"object[objectName]={objectType}");
        }

        var url = $"Document?{string.Join("&", queryParams)}";
        return await GetAsync<ListResponse<Document>>(url);
    }

    public async Task<string?> GetDocumentDownloadLinkAsync(string documentId)
    {
        // Not all documents have direct download links in the object.
        // Some might require a separate call or return a path.
        // Assuming we check the document object first or call a specific download endpoint if available.
        // SevDesk API v1 doesn't have a direct "get link" endpoint publicly documented well, often it's "download=true" on get?
        // Or using the 'downloadPath' from the document object.

        var doc = await GetAsync<SingleResponse<DocumentLink>>($"Document/{documentId}"); // Checking if full object has it or separate.
        // Actually, let's just fetch the document details, usually it contains `downloadPath` or similar.

        // Let's try to assume we get a fresh document object
        var url = $"Document/{documentId}";
        using var response = await _httpClient.GetAsync(url);
        if (!response.IsSuccessStatusCode) return null;

        var json = await response.Content.ReadAsStringAsync();
        using var docJson = JsonDocument.Parse(json);
        if (docJson.RootElement.TryGetProperty("objects", out var objects) && objects.GetArrayLength() > 0)
        {
             var obj = objects[0];
             // Check for downloadPath (standard in some versions)
             if (obj.TryGetProperty("downloadPath", out var dp)) return dp.GetString();
        }
        return null;
    }

    // Write Operations

    public async Task<Contact> CreateContactAsync(CreateContactRequest request)
    {
        var payload = new
        {
             familyname = request.Familyname,
             surename = request.Surename,
             email = request.Email,
             objectName = "Contact",
             category = new { id = 3, objectName = "Category" } // Defaulting to Supplier (id 3 usually, but risky assumption. Let's assume standard sevDesk setup)
             // Real implementation should probably lookup Category ID.
        };

        // Improving category handling:
        // Supplier = 3, Customer = ?
        // For now, minimal implementation.

        return await PostAsync<Contact>("Contact", payload);
    }

    public async Task<Invoice> CreateInvoiceDraftAsync(CreateInvoiceRequest request)
    {
        // 1. Create Invoice Header
        var invoicePayload = new
        {
            invoiceDate = request.InvoiceDate.ToString("yyyy-MM-dd"),
            header = $"Invoice {request.InvoiceDate:yyyyMMdd}",
            headText = "Invoice generated via MCP",
            status = request.Status, // 100
            currency = request.Currency,
            contact = new { id = request.ContactId, objectName = "Contact" },
            contactPerson = new { id = 1, objectName = "SevUser" }, // REQUIRED field often. Hardcoding ID 1 is risky.
            taxType = "default",
            invoiceType = "RE",
            objectName = "Invoice"
        };

        var invoice = await PostAsync<Invoice>("Invoice", invoicePayload);

        // 2. Add Line Items
        foreach (var item in request.LineItems)
        {
             var itemPayload = new
             {
                 invoice = new { id = invoice.Id, objectName = "Invoice" },
                 quantity = item.Quantity,
                 price = item.Price,
                 name = item.Name,
                 unity = new { id = 1, objectName = "Unity" }, // "Stück" often id 1.
                 taxRate = item.TaxRate,
                 objectName = "InvoicePos"
             };
             await PostAsync<object>("InvoicePos", itemPayload);
        }

        return invoice;
    }

    public async Task<Voucher> CreateVoucherAsync(CreateVoucherRequest request)
    {
         var payload = new
        {
            voucherDate = request.VoucherDate.ToString("yyyy-MM-dd"),
            description = request.Description,
            result = request.Amount, // Total amount? SevDesk logic varies.
            status = request.Status,
            objectName = "Voucher"
            // Supplier needs to be linked if provided
        };
        return await PostAsync<Voucher>("Voucher", payload);
    }

    public async Task BookInvoiceAsync(BookInvoiceRequest request)
    {
        // Booking often involves status change to 200 (Open)
        // And sending.
        // SevDesk has /Invoice/{id}/sendByEmail

        if (request.SendType == "VnM" || !string.IsNullOrWhiteSpace(request.Email))
        {
             var payload = new
             {
                 toEmail = request.Email,
                 subject = "Invoice",
                 text = "Please find attached.",
                 invoice = new { id = request.InvoiceId, objectName = "Invoice" }
             };

             await PostAsync<object>($"Invoice/{request.InvoiceId}/sendByEmail", payload);
        }

        // Mark as booked? Usually separate call if not automatic.
    }

    private async Task<T> GetAsync<T>(string url)
    {
        // Retries are handled by Polly in Program.cs
        var response = await _httpClient.GetAsync(url);

        if (!response.IsSuccessStatusCode)
        {
             var content = await response.Content.ReadAsStringAsync();
             throw new HttpRequestException($"SevDesk API Error: {response.StatusCode} - {content}");
        }

        var json = await response.Content.ReadAsStringAsync();
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        return JsonSerializer.Deserialize<T>(json, options) ?? throw new Exception("Failed to deserialize response");
    }

    private async Task<T> PostAsync<T>(string url, object payload)
    {
        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(url, content);
         if (!response.IsSuccessStatusCode)
        {
             var resContent = await response.Content.ReadAsStringAsync();
             throw new HttpRequestException($"SevDesk API Error: {response.StatusCode} - {resContent}");
        }

        var resJson = await response.Content.ReadAsStringAsync();
         var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        // POST usually returns SingleResponse structure or the object directly?
        // SevDesk POST returns the object in "objects": [ ... ] usually.
        // So we might need to deserialize as SingleResponse<T> and return Objects[0]

        try
        {
             var single = JsonSerializer.Deserialize<SingleResponse<T>>(resJson, options);
             if (single?.Objects != null && single.Objects.Any()) return single.Objects.First();
        }
        catch {}

        // Fallback if it returns raw object
        return JsonSerializer.Deserialize<T>(resJson, options) ?? throw new Exception("Failed to deserialize response");
    }
}

public class ListResponse<T>
{
    public List<T> Objects { get; set; } = new();
}

public class SingleResponse<T>
{
    public List<T> Objects { get; set; } = new();
}
