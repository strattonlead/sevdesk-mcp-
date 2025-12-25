# SevDesk MCP Server

An MCP (Model Context Protocol) server for [sevDesk](https://sevdesk.com/), enabling AI agents to interact with accounting data (Contacts, Invoices, Vouchers).

## Features

- **Contacts**: Search and retrieve contact details.
- **Invoices**: List and retrieve invoices with filtering options.
- **Vouchers**: List and retrieve vouchers/receipts.
- **Resources**: Direct access to entities via `sevdesk://` URIs.
- **Secure**: Read-only by default. API tokens are handled securely via environment variables.
- **Containerized**: Ready to run in Docker.

## Configuration

The server is configured via environment variables:

| Variable | Required | Default | Description |
|----------|----------|---------|-------------|
| `SEVDESK_API_TOKEN` | Yes | - | Your sevDesk API Token. |
| `SEVDESK_BASE_URL` | No | `https://my.sevdesk.de/api/v1` | Base URL for sevDesk API. |
| `MCP_MODE` | No | `stdio` | Transport mode (`stdio` or `http`). |
| `ALLOW_WRITE_TOOLS` | No | `false` | Set to `true` to enable write operations (e.g. creating contacts). |
| `LOG_LEVEL` | No | `Information` | Logging level. |

## Usage

### Running with Docker

```bash
docker run -i --rm \
  -e SEVDESK_API_TOKEN=your_token_here \
  createiflabs/sevdesk-mcp
```

### Tools

- `sevdesk.contacts.search`: Search contacts by query, email, or customer number.
- `sevdesk.contacts.get`: Get a single contact by ID.
- `sevdesk.invoices.list`: List invoices with filters (status, date range, contact).
- `sevdesk.invoices.get`: Get a single invoice by ID.
- `sevdesk.vouchers.list`: List vouchers with filters.
- `sevdesk.vouchers.get`: Get a single voucher by ID.

### Resources

You can read resources directly using URIs:

- `sevdesk://contacts/{id}`
- `sevdesk://invoices/{id}`
- `sevdesk://vouchers/{id}`

## Development

### Prerequisites

- .NET 8 SDK

### Build

```bash
dotnet build src/SevDesk.Mcp/SevDesk.Mcp.csproj
```

### Test

```bash
dotnet test tests/SevDesk.Mcp.Tests/SevDesk.Mcp.Tests.csproj
```

### Run Locally

```bash
export SEVDESK_API_TOKEN=your_test_token
dotnet run --project src/SevDesk.Mcp/SevDesk.Mcp.csproj
```

## Troubleshooting

- **401 Unauthorized**: Check your `SEVDESK_API_TOKEN`.
- **429 Too Many Requests**: The server handles rate limiting, but if you hit limits, wait and retry.
- **Docker logs**: Logs are written to stderr.
