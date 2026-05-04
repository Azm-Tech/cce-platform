using CCE.Api.Common.Observability;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Sentry.Serilog;
using Serilog.Events;
using Xunit;

namespace CCE.Infrastructure.Tests.Observability;

public sealed class LoggingExtensionsSentryTests
{
    [Fact]
    public void ConfigureSentry_PropagatesEnvironmentFromConfig()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SENTRY_ENVIRONMENT"] = "production",
                ["SENTRY_RELEASE"]     = "app-v1.0.0",
            })
            .Build();
        var options = new SentrySerilogOptions();

        LoggingExtensions.ConfigureSentry(options, "https://x@y/1", config, fallbackEnvironmentName: "Production");

        options.Dsn.Should().Be("https://x@y/1");
        options.Environment.Should().Be("production");
        options.Release.Should().Be("app-v1.0.0");
        options.MinimumEventLevel.Should().Be(LogEventLevel.Warning);
        options.MinimumBreadcrumbLevel.Should().Be(LogEventLevel.Information);
    }

    [Fact]
    public void ConfigureSentry_FallsBackToHostingEnvironmentName_WhenSentryEnvironmentMissing()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var options = new SentrySerilogOptions();

        LoggingExtensions.ConfigureSentry(options, "https://x@y/1", config, fallbackEnvironmentName: "Staging");

        options.Environment.Should().Be("Staging");
    }

    [Fact]
    public void ConfigureSentry_LeavesReleaseUnset_WhenSentryReleaseMissing()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SENTRY_ENVIRONMENT"] = "test",
            })
            .Build();
        var options = new SentrySerilogOptions();

        LoggingExtensions.ConfigureSentry(options, "https://x@y/1", config, fallbackEnvironmentName: "Production");

        options.Environment.Should().Be("test");
        options.Release.Should().BeNull("Release should remain unset when SENTRY_RELEASE is not configured");
    }

    [Fact]
    public void ConfigureSentry_LeavesReleaseUnset_WhenSentryReleaseIsEmpty()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SENTRY_RELEASE"] = "",
            })
            .Build();
        var options = new SentrySerilogOptions();

        LoggingExtensions.ConfigureSentry(options, "https://x@y/1", config, fallbackEnvironmentName: "Production");

        options.Release.Should().BeNull();
    }
}
