# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY src/SevDesk.Mcp/SevDesk.Mcp.csproj ./src/SevDesk.Mcp/
RUN dotnet restore src/SevDesk.Mcp/SevDesk.Mcp.csproj

# Copy everything else and build
COPY src/SevDesk.Mcp/ ./src/SevDesk.Mcp/
WORKDIR /app/src/SevDesk.Mcp
RUN dotnet publish -c Release -o /app/out

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime
WORKDIR /app

# Create a non-root user
RUN addgroup --system --gid 1000 appgroup \
    && adduser --system --uid 1000 --ingroup appgroup --shell /bin/sh appuser

COPY --from=build /app/out .

# Change ownership
RUN chown -R appuser:appgroup /app

USER appuser

# Set environment variables defaults
ENV SEVDESK_BASE_URL="https://my.sevdesk.de/api/v1" \
    MCP_MODE="stdio" \
    ALLOW_WRITE_TOOLS="false" \
    LOG_LEVEL="Information" \
    SEVDESK_USER_AGENT="sevdesk-mcp/1.0" \
    DOTNET_RUNNING_IN_CONTAINER=true

# Entrypoint
ENTRYPOINT ["dotnet", "SevDesk.Mcp.dll"]
