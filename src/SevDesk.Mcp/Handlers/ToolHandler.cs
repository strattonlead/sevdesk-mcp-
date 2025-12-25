using System.Text.Json;
using SevDesk.Mcp.Client;
using SevDesk.Mcp.Configuration;
using SevDesk.Mcp.Models;

namespace SevDesk.Mcp.Handlers;

public class ToolHandler
{
    private readonly SevDeskClient _client;
    private readonly AppConfig _config;

    public ToolHandler(SevDeskClient client, AppConfig config)
    {
        _client = client;
        _config = config;
    }

    public List<McpTool> ListTools()
    {
        var tools = new List<McpTool>
        {
            new McpTool
            {
                Name = "sevdesk.contacts.search",
                Description = "Search contacts by query, email, or customer number",
                InputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        query = new { type = "string", description = "General search term" },
                        email = new { type = "string", description = "Filter by email" },
                        customerNumber = new { type = "string", description = "Filter by customer number" },
                        page = new { type = "integer", description = "Page number (1-based)" },
                        limit = new { type = "integer", description = "Items per page" }
                    }
                }
            },
            new McpTool
            {
                Name = "sevdesk.contacts.get",
                Description = "Get a contact by ID",
                InputSchema = new
                {
                    type = "object",
                    required = new[] { "id" },
                    properties = new
                    {
                        id = new { type = "string", description = "The ID of the contact" }
                    }
                }
            },
            new McpTool
            {
                Name = "sevdesk.invoices.list",
                Description = "List invoices with filtering",
                InputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        status = new { type = "string", description = "Filter by status (e.g. '100' for draft, '200' for open, '1000' for paid)" }, // SevDesk statuses are often numeric codes
                        contactId = new { type = "string", description = "Filter by contact ID" },
                        fromDate = new { type = "string", description = "Filter by date from (YYYY-MM-DD)" },
                        toDate = new { type = "string", description = "Filter by date to (YYYY-MM-DD)" },
                        page = new { type = "integer" },
                        limit = new { type = "integer" }
                    }
                }
            },
             new McpTool
            {
                Name = "sevdesk.invoices.get",
                Description = "Get an invoice by ID",
                InputSchema = new
                {
                    type = "object",
                    required = new[] { "id" },
                    properties = new
                    {
                        id = new { type = "string" }
                    }
                }
            },
            new McpTool
            {
                Name = "sevdesk.vouchers.list",
                Description = "List vouchers/receipts",
                InputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        status = new { type = "string" },
                        fromDate = new { type = "string" },
                        toDate = new { type = "string" },
                        page = new { type = "integer" },
                        limit = new { type = "integer" }
                    }
                }
            },
             new McpTool
            {
                Name = "sevdesk.vouchers.get",
                Description = "Get a voucher by ID",
                InputSchema = new
                {
                    type = "object",
                    required = new[] { "id" },
                    properties = new
                    {
                        id = new { type = "string" }
                    }
                }
            },
            new McpTool
            {
                Name = "sevdesk.documents.list",
                Description = "List documents linked to an object",
                InputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        objectType = new { type = "string", description = "Object type (e.g., Invoice, Voucher, Contact)" },
                        objectId = new { type = "string", description = "ID of the object" },
                        page = new { type = "integer" },
                        limit = new { type = "integer" }
                    }
                }
            },
            new McpTool
            {
                Name = "sevdesk.documents.downloadLink",
                Description = "Get a temporary download link for a document",
                InputSchema = new
                {
                    type = "object",
                    required = new[] { "documentId" },
                    properties = new
                    {
                        documentId = new { type = "string" }
                    }
                }
            },
            new McpTool
            {
                Name = "sevdesk.summary.financialSnapshot",
                Description = "Get a financial snapshot (unpaid invoices, top open invoices)",
                InputSchema = new { type = "object" } // No args needed mostly, maybe caps
            }
        };

        if (_config.AllowWriteTools)
        {
             tools.Add(new McpTool
             {
                 Name = "sevdesk.contacts.create",
                 Description = "Create a new contact",
                 InputSchema = new
                 {
                     type = "object",
                     required = new[] { "familyname", "surename" },
                     properties = new
                     {
                         familyname = new { type = "string" },
                         surename = new { type = "string" },
                         email = new { type = "string" },
                         category = new { type = "string", description = "Category (default: Supplier)" }
                     }
                 }
             });

             tools.Add(new McpTool
             {
                 Name = "sevdesk.invoices.createDraft",
                 Description = "Create a draft invoice",
                 InputSchema = new
                 {
                     type = "object",
                     required = new[] { "contactId", "invoiceDate", "lineItems" },
                     properties = new
                     {
                         contactId = new { type = "string" },
                         invoiceDate = new { type = "string" },
                         currency = new { type = "string" },
                         lineItems = new
                         {
                             type = "array",
                             items = new
                             {
                                 type = "object",
                                 required = new[] { "name", "quantity", "price", "unity" },
                                 properties = new
                                 {
                                     name = new { type = "string" },
                                     quantity = new { type = "number" },
                                     price = new { type = "number" },
                                     unity = new { type = "string" },
                                     taxRate = new { type = "number" }
                                 }
                             }
                         }
                     }
                 }
             });

             tools.Add(new McpTool
             {
                 Name = "sevdesk.invoices.bookAndSend",
                 Description = "Book and send an invoice",
                 InputSchema = new
                 {
                     type = "object",
                     required = new[] { "invoiceId", "sendType" },
                     properties = new
                     {
                         invoiceId = new { type = "string" },
                         sendType = new { type = "string", description = "VnM (Email), VpR (Print), VnPo (Post)" },
                         email = new { type = "string" }
                     }
                 }
             });

             tools.Add(new McpTool
             {
                 Name = "sevdesk.vouchers.create",
                 Description = "Create a voucher",
                 InputSchema = new
                 {
                     type = "object",
                     required = new[] { "amount", "voucherDate" },
                     properties = new
                     {
                         amount = new { type = "number" },
                         voucherDate = new { type = "string" },
                         description = new { type = "string" },
                         supplierName = new { type = "string" }
                     }
                 }
             });
        }

        return tools;
    }

    public async Task<McpCallToolResult> CallToolAsync(string name, Dictionary<string, object>? args)
    {
        try
        {
             switch (name)
             {
                 case "sevdesk.contacts.search":
                     return await HandleContactSearch(args);
                 case "sevdesk.contacts.get":
                     return await HandleContactGet(args);
                 case "sevdesk.invoices.list":
                     return await HandleInvoiceList(args);
                 case "sevdesk.invoices.get":
                     return await HandleInvoiceGet(args);
                 case "sevdesk.vouchers.list":
                     return await HandleVoucherList(args);
                 case "sevdesk.vouchers.get":
                     return await HandleVoucherGet(args);
                 case "sevdesk.documents.list":
                     return await HandleDocumentList(args);
                 case "sevdesk.documents.downloadLink":
                     return await HandleDocumentDownloadLink(args);
                 case "sevdesk.summary.financialSnapshot":
                     return await HandleFinancialSnapshot(args);
                 case "sevdesk.contacts.create":
                     return await HandleContactCreate(args);
                 case "sevdesk.invoices.createDraft":
                     return await HandleInvoiceCreateDraft(args);
                 case "sevdesk.invoices.bookAndSend":
                     return await HandleInvoiceBookAndSend(args);
                 case "sevdesk.vouchers.create":
                     return await HandleVoucherCreate(args);
                 default:
                     throw new Exception($"Tool '{name}' not found");
             }
        }
        catch (Exception ex)
        {
             return new McpCallToolResult
             {
                 IsError = true,
                 Content = new List<McpContent>
                 {
                     new McpContent { Type = "text", Text = $"Error: {ex.Message}" }
                 }
             };
        }
    }

    private int GetLimit(Dictionary<string, object>? args)
    {
        if (args != null && args.TryGetValue("limit", out var l) && l is JsonElement limitEl && limitEl.ValueKind == JsonValueKind.Number)
        {
             var limit = limitEl.GetInt32();
             if (limit > _config.MaxPageSize) return _config.MaxPageSize;
             if (limit < 1) return 1;
             return limit;
        }
        return _config.DefaultPageSize;
    }

     private int GetPage(Dictionary<string, object>? args)
    {
        if (args != null && args.TryGetValue("page", out var p) && p is JsonElement pageEl && pageEl.ValueKind == JsonValueKind.Number)
        {
             var page = pageEl.GetInt32();
             if (page < 1) return 1;
             return page;
        }
        return 1;
    }

    private string? GetString(Dictionary<string, object>? args, string key)
    {
        if (args != null && args.TryGetValue(key, out var val) && val is JsonElement el && el.ValueKind == JsonValueKind.String)
        {
            return el.GetString();
        }
        return null;
    }

     private DateTime? GetDate(Dictionary<string, object>? args, string key)
    {
        if (args != null && args.TryGetValue(key, out var val) && val is JsonElement el && el.ValueKind == JsonValueKind.String)
        {
            if (DateTime.TryParse(el.GetString(), out var date)) return date;
        }
        return null;
    }

    private async Task<McpCallToolResult> HandleContactSearch(Dictionary<string, object>? args)
    {
        var query = GetString(args, "query");
        var email = GetString(args, "email");
        var customerNumber = GetString(args, "customerNumber");
        var page = GetPage(args);
        var limit = GetLimit(args);

        var result = await _client.SearchContactsAsync(query, email, customerNumber, page, limit);
        return Json(result);
    }

     private async Task<McpCallToolResult> HandleContactGet(Dictionary<string, object>? args)
    {
        var id = GetString(args, "id") ?? throw new Exception("id is required");
        var result = await _client.GetContactAsync(id);
        if (result == null) throw new Exception("Contact not found");
        return Json(result);
    }

    private async Task<McpCallToolResult> HandleInvoiceList(Dictionary<string, object>? args)
    {
        var status = GetString(args, "status");
        var contactId = GetString(args, "contactId");
        var fromDate = GetDate(args, "fromDate");
        var toDate = GetDate(args, "toDate");
        var page = GetPage(args);
        var limit = GetLimit(args);

        var result = await _client.GetInvoicesAsync(status, contactId, fromDate, toDate, page, limit);
        return Json(result);
    }

    private async Task<McpCallToolResult> HandleInvoiceGet(Dictionary<string, object>? args)
    {
         var id = GetString(args, "id") ?? throw new Exception("id is required");
         var result = await _client.GetInvoiceAsync(id);
         if (result == null) throw new Exception("Invoice not found");
         return Json(result);
    }

    private async Task<McpCallToolResult> HandleVoucherList(Dictionary<string, object>? args)
    {
        var status = GetString(args, "status");
        var fromDate = GetDate(args, "fromDate");
        var toDate = GetDate(args, "toDate");
        var page = GetPage(args);
        var limit = GetLimit(args);

        var result = await _client.GetVouchersAsync(status, fromDate, toDate, page, limit);
        return Json(result);
    }

    private async Task<McpCallToolResult> HandleVoucherGet(Dictionary<string, object>? args)
    {
         var id = GetString(args, "id") ?? throw new Exception("id is required");
         var result = await _client.GetVoucherAsync(id);
         if (result == null) throw new Exception("Voucher not found");
         return Json(result);
    }

    private async Task<McpCallToolResult> HandleDocumentList(Dictionary<string, object>? args)
    {
        var objectType = GetString(args, "objectType");
        var objectId = GetString(args, "objectId");
        var page = GetPage(args);
        var limit = GetLimit(args);

        var result = await _client.GetDocumentsAsync(objectType, objectId, page, limit);
        return Json(result);
    }

    private async Task<McpCallToolResult> HandleDocumentDownloadLink(Dictionary<string, object>? args)
    {
        var documentId = GetString(args, "documentId") ?? throw new Exception("documentId is required");
        var link = await _client.GetDocumentDownloadLinkAsync(documentId);

        if (link == null)
        {
             return new McpCallToolResult
             {
                 IsError = true,
                 Content = new List<McpContent> { new McpContent { Type = "text", Text = "Could not generate download link or document not found." } }
             };
        }

        return Json(new { downloadUrl = link });
    }

    private async Task<McpCallToolResult> HandleFinancialSnapshot(Dictionary<string, object>? args)
    {
        // Compute summary
        // 1. Get open invoices
        var openInvoices = await _client.GetInvoicesAsync("200", null, null, null, 1, 100); // Status 200 = Open

        // 2. Get overdue (naive check on invoiceDate? SevDesk doesn't give due date easily in list?)
        // Assuming we rely on status or just basic totals for now.

        decimal totalUnpaid = openInvoices.Objects.Sum(i => i.SumGross);

        // Top 5 open
        var top5 = openInvoices.Objects.OrderByDescending(i => i.SumGross).Take(5).ToList();

        var snapshot = new FinancialSnapshot
        {
            OpenInvoiceCount = openInvoices.Objects.Count,
            UnpaidInvoicesTotal = totalUnpaid,
            TopOpenInvoices = top5,
            OverdueInvoiceCount = 0 // Needs more logic
        };

        return Json(snapshot);
    }

    // Write Handlers
    private async Task<McpCallToolResult> HandleContactCreate(Dictionary<string, object>? args)
    {
        if (!_config.AllowWriteTools) throw new Exception("Write tools are disabled");

        var req = new CreateContactRequest
        {
            Familyname = GetString(args, "familyname")!,
            Surename = GetString(args, "surename")!,
            Email = GetString(args, "email"),
            Category = GetString(args, "category")
        };

        var contact = await _client.CreateContactAsync(req);
        return Json(contact);
    }

     private async Task<McpCallToolResult> HandleInvoiceCreateDraft(Dictionary<string, object>? args)
    {
        if (!_config.AllowWriteTools) throw new Exception("Write tools are disabled");

        // Complex parsing for LineItems
        // We expect "lineItems" to be a JsonElement array
        var lineItems = new List<InvoiceLineItem>();
        if (args != null && args.TryGetValue("lineItems", out var liObj) && liObj is JsonElement liEl && liEl.ValueKind == JsonValueKind.Array)
        {
             foreach (var item in liEl.EnumerateArray())
             {
                 lineItems.Add(new InvoiceLineItem
                 {
                     Name = item.GetProperty("name").GetString()!,
                     Quantity = item.GetProperty("quantity").GetDecimal(),
                     Price = item.GetProperty("price").GetDecimal(),
                     Unity = item.GetProperty("unity").GetString()!,
                     TaxRate = item.TryGetProperty("taxRate", out var tr) ? tr.GetDecimal() : 19
                 });
             }
        }
        else
        {
             throw new Exception("lineItems array required");
        }

        var req = new CreateInvoiceRequest
        {
             ContactId = GetString(args, "contactId")!,
             InvoiceDate = GetDate(args, "invoiceDate") ?? DateTime.Today,
             Currency = GetString(args, "currency") ?? "EUR",
             LineItems = lineItems
        };

        var invoice = await _client.CreateInvoiceDraftAsync(req);
        return Json(invoice);
    }

    private async Task<McpCallToolResult> HandleInvoiceBookAndSend(Dictionary<string, object>? args)
    {
        if (!_config.AllowWriteTools) throw new Exception("Write tools are disabled");

        var req = new BookInvoiceRequest
        {
             InvoiceId = GetString(args, "invoiceId")!,
             SendType = GetString(args, "sendType")!,
             Email = GetString(args, "email")
        };

        await _client.BookInvoiceAsync(req);
        return Json(new { status = "Booked and sent" });
    }

    private async Task<McpCallToolResult> HandleVoucherCreate(Dictionary<string, object>? args)
    {
        if (!_config.AllowWriteTools) throw new Exception("Write tools are disabled");

        var req = new CreateVoucherRequest
        {
             Amount = GetDecimal(args, "amount"),
             VoucherDate = GetDate(args, "voucherDate") ?? DateTime.Today,
             Description = GetString(args, "description"),
             SupplierName = GetString(args, "supplierName")
        };

        var voucher = await _client.CreateVoucherAsync(req);
        return Json(voucher);
    }

    private decimal GetDecimal(Dictionary<string, object>? args, string key)
    {
         if (args != null && args.TryGetValue(key, out var val) && val is JsonElement el && el.ValueKind == JsonValueKind.Number)
        {
            return el.GetDecimal();
        }
        return 0;
    }

    private McpCallToolResult Json(object result)
    {
        return new McpCallToolResult
        {
            Content = new List<McpContent>
            {
                new McpContent
                {
                    Type = "text",
                    Text = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true })
                }
            }
        };
    }
}
