using System.ComponentModel;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using SevDesk.Mcp.Client;
using SevDesk.Mcp.Configuration;
using SevDesk.Mcp.Models;

namespace SevDesk.Mcp.Handlers;

[McpServerToolType]
public class SevDeskTools
{
    private readonly SevDeskClient _client;
    private readonly AppConfig _config;
    private readonly ILogger<SevDeskTools> _logger;

    public SevDeskTools(SevDeskClient client, AppConfig config, ILogger<SevDeskTools> logger)
    {
        _client = client;
        _config = config;
        _logger = logger;
    }

    [McpServerTool("sevdesk.contacts.search", Description = "Search contacts by query, email, or customer number")]
    public async Task<string> SearchContacts(
        [Description("General search term")] string? query = null,
        [Description("Filter by email")] string? email = null,
        [Description("Filter by customer number")] string? customerNumber = null,
        [Description("Page number (1-based)")] int page = 1,
        [Description("Items per page")] int limit = 50)
    {
        limit = ClampLimit(limit);
        page = Math.Max(1, page);
        var result = await _client.SearchContactsAsync(query, email, customerNumber, page, limit);
        return Json(result);
    }

    [McpServerTool("sevdesk.contacts.get", Description = "Get a contact by ID")]
    public async Task<string> GetContact([Description("The ID of the contact")] string id)
    {
        var result = await _client.GetContactAsync(id);
        if (result == null) throw new Exception("Contact not found");
        return Json(result);
    }

    [McpServerTool("sevdesk.invoices.list", Description = "List invoices with filtering")]
    public async Task<string> ListInvoices(
        [Description("Filter by status (e.g. '100' for draft, '200' for open, '1000' for paid)")] string? status = null,
        [Description("Filter by contact ID")] string? contactId = null,
        [Description("Filter by date from (YYYY-MM-DD)")] string? fromDate = null,
        [Description("Filter by date to (YYYY-MM-DD)")] string? toDate = null,
        int page = 1,
        int limit = 50)
    {
        limit = ClampLimit(limit);
        page = Math.Max(1, page);
        DateTime? from = ParseDate(fromDate);
        DateTime? to = ParseDate(toDate);

        var result = await _client.GetInvoicesAsync(status, contactId, from, to, page, limit);
        return Json(result);
    }

    [McpServerTool("sevdesk.invoices.get", Description = "Get an invoice by ID")]
    public async Task<string> GetInvoice([Description("The ID of the invoice")] string id)
    {
        var result = await _client.GetInvoiceAsync(id);
        if (result == null) throw new Exception("Invoice not found");
        return Json(result);
    }

    [McpServerTool("sevdesk.vouchers.list", Description = "List vouchers/receipts")]
    public async Task<string> ListVouchers(
        string? status = null,
        string? fromDate = null,
        string? toDate = null,
        int page = 1,
        int limit = 50)
    {
        limit = ClampLimit(limit);
        page = Math.Max(1, page);
        DateTime? from = ParseDate(fromDate);
        DateTime? to = ParseDate(toDate);

        var result = await _client.GetVouchersAsync(status, from, to, page, limit);
        return Json(result);
    }

    [McpServerTool("sevdesk.vouchers.get", Description = "Get a voucher by ID")]
    public async Task<string> GetVoucher([Description("The ID of the voucher")] string id)
    {
        var result = await _client.GetVoucherAsync(id);
        if (result == null) throw new Exception("Voucher not found");
        return Json(result);
    }

    [McpServerTool("sevdesk.documents.list", Description = "List documents linked to an object")]
    public async Task<string> ListDocuments(
        [Description("Object type (e.g., Invoice, Voucher, Contact)")] string? objectType = null,
        [Description("ID of the object")] string? objectId = null,
        int page = 1,
        int limit = 50)
    {
        limit = ClampLimit(limit);
        page = Math.Max(1, page);
        var result = await _client.GetDocumentsAsync(objectType, objectId, page, limit);
        return Json(result);
    }

    [McpServerTool("sevdesk.documents.downloadLink", Description = "Get a temporary download link for a document")]
    public async Task<string> GetDocumentDownloadLink([Description("The ID of the document")] string documentId)
    {
        var link = await _client.GetDocumentDownloadLinkAsync(documentId);
        if (link == null)
        {
            return "Could not generate download link or document not found.";
        }
        return Json(new { downloadUrl = link });
    }

    [McpServerTool("sevdesk.summary.financialSnapshot", Description = "Get a financial snapshot (unpaid invoices, top open invoices)")]
    public async Task<string> GetFinancialSnapshot()
    {
        var openInvoices = await _client.GetInvoicesAsync("200", null, null, null, 1, 100);
        decimal totalUnpaid = openInvoices.Objects.Sum(i => i.SumGross);
        var top5 = openInvoices.Objects.OrderByDescending(i => i.SumGross).Take(5).ToList();

        var snapshot = new FinancialSnapshot
        {
            OpenInvoiceCount = openInvoices.Objects.Count,
            UnpaidInvoicesTotal = totalUnpaid,
            TopOpenInvoices = top5,
            OverdueInvoiceCount = 0
        };

        return Json(snapshot);
    }

    [McpServerTool("sevdesk.contacts.create", Description = "Create a new contact")]
    public async Task<string> CreateContact(
        string familyname,
        string surename,
        string? email = null,
        [Description("Category (default: Supplier)")] string? category = null)
    {
        if (!_config.AllowWriteTools) throw new Exception("Write tools are disabled");

        var req = new CreateContactRequest
        {
            Familyname = familyname,
            Surename = surename,
            Email = email,
            Category = category
        };

        var contact = await _client.CreateContactAsync(req);
        return Json(contact);
    }

    // Helper for JSON serialization
    private string Json(object result)
    {
        return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
    }

    private int ClampLimit(int limit)
    {
        if (limit > _config.MaxPageSize) return _config.MaxPageSize;
        if (limit < 1) return 1;
        return limit;
    }

    private DateTime? ParseDate(string? dateStr)
    {
        if (string.IsNullOrEmpty(dateStr)) return null;
        if (DateTime.TryParse(dateStr, out var date)) return date;
        return null;
    }

    // Complex tools requiring objects or arrays as input are trickier with simple parameters.
    // The library supports complex types if they are JSON deserializable.

    [McpServerTool("sevdesk.invoices.createDraft", Description = "Create a draft invoice")]
    public async Task<string> CreateInvoiceDraft(
        string contactId,
        string invoiceDate,
        [Description("Array of line items.")] InvoiceLineItem[] lineItems,
        string currency = "EUR")
    {
        if (!_config.AllowWriteTools) throw new Exception("Write tools are disabled");

        DateTime date = DateTime.Today;
        DateTime.TryParse(invoiceDate, out date);

        var req = new CreateInvoiceRequest
        {
            ContactId = contactId,
            InvoiceDate = date,
            Currency = currency,
            LineItems = lineItems.ToList()
        };

        var invoice = await _client.CreateInvoiceDraftAsync(req);
        return Json(invoice);
    }

    [McpServerTool("sevdesk.invoices.bookAndSend", Description = "Book and send an invoice")]
    public async Task<string> BookAndSendInvoice(
        string invoiceId,
        [Description("VnM (Email), VpR (Print), VnPo (Post)")] string sendType,
        string? email = null)
    {
        if (!_config.AllowWriteTools) throw new Exception("Write tools are disabled");

        var req = new BookInvoiceRequest
        {
            InvoiceId = invoiceId,
            SendType = sendType,
            Email = email
        };

        await _client.BookInvoiceAsync(req);
        return Json(new { status = "Booked and sent" });
    }

    [McpServerTool("sevdesk.vouchers.create", Description = "Create a voucher")]
    public async Task<string> CreateVoucher(
        decimal amount,
        string voucherDate,
        string? description = null,
        string? supplierName = null)
    {
        if (!_config.AllowWriteTools) throw new Exception("Write tools are disabled");

        DateTime date = DateTime.Today;
        DateTime.TryParse(voucherDate, out date);

        var req = new CreateVoucherRequest
        {
            Amount = amount,
            VoucherDate = date,
            Description = description,
            SupplierName = supplierName
        };

        var voucher = await _client.CreateVoucherAsync(req);
        return Json(voucher);
    }

}
