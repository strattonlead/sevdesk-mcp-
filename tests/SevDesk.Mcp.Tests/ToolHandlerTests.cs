using Moq;
using Moq.Protected;
using System.Net;
using System.Text.Json;
using SevDesk.Mcp.Client;
using SevDesk.Mcp.Configuration;
using SevDesk.Mcp.Handlers;
using SevDesk.Mcp.Models;
using Xunit;

namespace SevDesk.Mcp.Tests;

public class ToolHandlerTests
{
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly SevDeskClient _client;
    private readonly AppConfig _config;
    private readonly ToolHandler _handler;

    public ToolHandlerTests()
    {
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(_httpMessageHandlerMock.Object)
        {
            BaseAddress = new Uri("https://api.sevdesk.de")
        };

        _config = new AppConfig
        {
            SevDeskApiToken = "test",
            AllowWriteTools = true,
            DefaultPageSize = 50,
            MaxPageSize = 100
        };

        _client = new SevDeskClient(httpClient, _config);
        _handler = new ToolHandler(_client, _config);
    }

    [Fact]
    public async Task CallToolAsync_SearchContacts_ReturnsResult()
    {
        // Arrange
        var contacts = new List<Contact>
        {
            new Contact
            {
                Id = "1",
                Familyname = "Doe",
                Surename = "John",
                Email = "john@example.com",
                ObjectName = "Contact",
                Name = "John Doe",
                CustomerNumber = "100"
            }
        };
        var response = new ListResponse<Contact> { Objects = contacts };

        SetupMockResponse(JsonSerializer.Serialize(response));

        var args = new Dictionary<string, object>
        {
            { "query", "John" }
        };

        // Act
        var result = await _handler.CallToolAsync("sevdesk.contacts.search", args);

        // Assert
        Assert.False(result.IsError);
        Assert.Contains("Doe", result.Content[0].Text);
    }

    [Fact]
    public async Task CallToolAsync_CreateContact_Succeeds_WhenWriteAllowed()
    {
        var contact = new Contact
        {
            Id = "2",
            Familyname = "Smith",
            Surename = "Jane",
            ObjectName = "Contact",
            Name = "Jane Smith",
            CustomerNumber = "101",
            Email = "jane@example.com"
        };

        // Mock POST response usually returns the object wrapped in SingleResponse or just object
        // SevDeskClient.PostAsync handles standard SevDesk response.
        // Assuming the client expects the object.

        SetupMockResponse(JsonSerializer.Serialize(contact));

        var args = new Dictionary<string, object>
        {
            { "familyname", "Smith" },
            { "surename", "Jane" },
            { "email", "jane@example.com" }
        };

        var result = await _handler.CallToolAsync("sevdesk.contacts.create", args);

        Assert.False(result.IsError);
        Assert.Contains("Smith", result.Content[0].Text);
    }

    [Fact]
    public async Task CallToolAsync_CreateContact_Fails_WhenWriteDisabled()
    {
        _config.AllowWriteTools = false;
        var args = new Dictionary<string, object>
        {
            { "familyname", "Smith" }
        };

        var result = await _handler.CallToolAsync("sevdesk.contacts.create", args);

        Assert.True(result.IsError);
        Assert.Contains("Write tools are disabled", result.Content[0].Text);
    }

    [Fact]
    public async Task CallToolAsync_FinancialSnapshot_ReturnsCorrectData()
    {
        // Mock Invoices
        var invoices = new List<Invoice>
        {
            new Invoice { Id = "1", SumGross = 100, Status = "200", ContactId = "1", Currency = "EUR", InvoiceDate = DateTime.Now, InvoiceNumber = "INV-1", SumNet = 80, SumTax = 20, TotalGross = 100, TotalNet = 80 },
            new Invoice { Id = "2", SumGross = 200, Status = "200", ContactId = "2", Currency = "EUR", InvoiceDate = DateTime.Now, InvoiceNumber = "INV-2", SumNet = 160, SumTax = 40, TotalGross = 200, TotalNet = 160 }
        };

        SetupMockResponse(JsonSerializer.Serialize(new ListResponse<Invoice> { Objects = invoices }));

        var result = await _handler.CallToolAsync("sevdesk.summary.financialSnapshot", new Dictionary<string, object>());

        Assert.False(result.IsError);
        Assert.Contains("300", result.Content[0].Text); // Total Unpaid
        Assert.Contains("OpenInvoiceCount\": 2", result.Content[0].Text);
    }

    private void SetupMockResponse(string content)
    {
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(content)
            });
    }
}
