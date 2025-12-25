using Xunit;
using SevDesk.Mcp.Configuration;

namespace SevDesk.Mcp.Tests;

public class AppConfigTests
{
    [Fact]
    public void Validate_Throws_IfTokenMissing()
    {
        // Environment variables are global, so we must be careful.
        // For unit tests, we can mock environment or use reflection if needed,
        // but AppConfig.LoadFromEnvironment calls Environment.GetEnvironmentVariable directly.
        // A better design would be to inject an IEnvironmentService.
        // But for this simple app, we can just set/unset env vars in the test (and lock if parallel).

        lock (this)
        {
            var oldToken = Environment.GetEnvironmentVariable("SEVDESK_API_TOKEN");
            Environment.SetEnvironmentVariable("SEVDESK_API_TOKEN", null);

            try
            {
                Assert.Throws<InvalidOperationException>(() => AppConfig.LoadFromEnvironment());
            }
            finally
            {
                Environment.SetEnvironmentVariable("SEVDESK_API_TOKEN", oldToken);
            }
        }
    }

    [Fact]
    public void Validate_Success_WithDefaults()
    {
        lock (this)
        {
            var oldToken = Environment.GetEnvironmentVariable("SEVDESK_API_TOKEN");
            Environment.SetEnvironmentVariable("SEVDESK_API_TOKEN", "test-token");

            try
            {
                var config = AppConfig.LoadFromEnvironment();
                Assert.Equal("test-token", config.SevDeskApiToken);
                Assert.Equal(50, config.DefaultPageSize);
                Assert.Equal(100, config.MaxPageSize);
                Assert.False(config.AllowWriteTools);
            }
            finally
            {
                 Environment.SetEnvironmentVariable("SEVDESK_API_TOKEN", oldToken);
            }
        }
    }

    [Fact]
    public void Validate_Throws_InvalidPageSize()
    {
         lock (this)
        {
            var oldToken = Environment.GetEnvironmentVariable("SEVDESK_API_TOKEN");
            var oldDefault = Environment.GetEnvironmentVariable("DEFAULT_PAGE_SIZE");
             var oldMax = Environment.GetEnvironmentVariable("MAX_PAGE_SIZE");

            Environment.SetEnvironmentVariable("SEVDESK_API_TOKEN", "test-token");
            Environment.SetEnvironmentVariable("DEFAULT_PAGE_SIZE", "150");
            Environment.SetEnvironmentVariable("MAX_PAGE_SIZE", "100");

            try
            {
                Assert.Throws<InvalidOperationException>(() => AppConfig.LoadFromEnvironment());
            }
            finally
            {
                 Environment.SetEnvironmentVariable("SEVDESK_API_TOKEN", oldToken);
                 Environment.SetEnvironmentVariable("DEFAULT_PAGE_SIZE", oldDefault);
                 Environment.SetEnvironmentVariable("MAX_PAGE_SIZE", oldMax);
            }
        }
    }
}
