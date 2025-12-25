namespace SevDesk.Mcp.Models;

public record Contact
{
    public string Id { get; init; }
    public string ObjectName { get; init; }
    public string Familyname { get; init; }
    public string Surename { get; init; }
    public string Name { get; init; } // Often organization name
    public string CustomerNumber { get; init; }
    public string Email { get; init; }
}

public record Invoice
{
    public string Id { get; init; }
    public string InvoiceNumber { get; init; }
    public string ContactId { get; init; }
    public DateTime InvoiceDate { get; init; }
    public string Status { get; init; }
    public decimal TotalNet { get; init; }
    public decimal TotalGross { get; init; }
    public decimal SumNet { get; init; }
    public decimal SumGross { get; init; }
    public decimal SumTax { get; init; }
    public string Currency { get; init; }
}

public record Voucher
{
    public string Id { get; init; }
    public string Status { get; init; }
    public DateTime VoucherDate { get; init; }
    public string Description { get; init; }
    public decimal TotalNet { get; init; }
    public decimal TotalGross { get; init; }
}

public record Document
{
    public string Id { get; init; }
    public string Filename { get; init; }
    public string MimeType { get; init; }
    public int FileSize { get; init; }
    // Add other fields as necessary
}

public record DocumentLink
{
    public string Id { get; init; }
    public string DownloadPath { get; init; }
}

public record CreateContactRequest
{
     public required string Familyname { get; init; }
     public required string Surename { get; init; }
     public string? Email { get; init; }
     public string? Category { get; init; } = "Supplier"; // "Supplier" or "Customer"
}

public record CreateInvoiceRequest
{
    public required string ContactId { get; init; }
    public required DateTime InvoiceDate { get; init; }
    public string Currency { get; init; } = "EUR";
    public required List<InvoiceLineItem> LineItems { get; init; }
    public string Status { get; init; } = "100"; // Draft
}

public record InvoiceLineItem
{
    public required string Unity { get; init; }
    public required decimal Quantity { get; init; }
    public required decimal Price { get; init; } // Net price per unit
    public required string Name { get; init; }
    public decimal TaxRate { get; init; } = 19;
}

public record CreateVoucherRequest
{
    public required decimal Amount { get; init; }
    public required DateTime VoucherDate { get; init; }
    public string? SupplierName { get; init; }
    public string? Description { get; init; }
    public string Status { get; init; } = "50";
}

public record BookInvoiceRequest
{
    public required string InvoiceId { get; init; }
    public required string SendType { get; init; } // "VpR" (Print), "VnM" (Mail), "VnPo" (Post)
    public string? Email { get; init; } // Override email
}

public record FinancialSnapshot
{
    public decimal UnpaidInvoicesTotal { get; init; }
    public int OverdueInvoiceCount { get; init; }
    public int OpenInvoiceCount { get; init; }
    public List<Invoice> TopOpenInvoices { get; init; } = new();
}
