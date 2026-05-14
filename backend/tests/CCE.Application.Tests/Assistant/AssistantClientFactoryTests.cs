using CCE.Application.Assistant;
using CCE.Infrastructure.Assistant;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CCE.Application.Tests.Assistant;

public class AssistantClientFactoryTests
{
    [Fact]
    public void Provider_stub_registers_stub_client()
    {
        var services = new ServiceCollection();
        var config = BuildConfig(("Assistant:Provider", "stub"));
        services.AddCceAssistantClient(config);

        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(ISmartAssistantClient));
        descriptor.Should().NotBeNull();
        descriptor!.ImplementationType.Should().Be<SmartAssistantClient>();
    }

    [Fact]
    public void Provider_anthropic_with_key_registers_Anthropic_client()
    {
        Environment.SetEnvironmentVariable(AssistantClientFactory.AnthropicApiKeyEnvVar, "sk-test");
        try
        {
            var services = new ServiceCollection();
            var config = BuildConfig(("Assistant:Provider", "anthropic"));
            services.AddCceAssistantClient(config);

            var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(ISmartAssistantClient));
            descriptor!.ImplementationType.Should().Be<AnthropicSmartAssistantClient>();
        }
        finally
        {
            Environment.SetEnvironmentVariable(AssistantClientFactory.AnthropicApiKeyEnvVar, null);
        }
    }

    [Fact]
    public void Provider_anthropic_without_key_falls_back_to_stub()
    {
        Environment.SetEnvironmentVariable(AssistantClientFactory.AnthropicApiKeyEnvVar, null);

        var services = new ServiceCollection();
        var config = BuildConfig(("Assistant:Provider", "anthropic"));
        services.AddCceAssistantClient(config);

        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(ISmartAssistantClient));
        descriptor!.ImplementationType.Should().Be<SmartAssistantClient>();
    }

    [Fact]
    public void Default_provider_is_stub()
    {
        var services = new ServiceCollection();
        var config = BuildConfig();
        services.AddCceAssistantClient(config);

        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(ISmartAssistantClient));
        descriptor!.ImplementationType.Should().Be<SmartAssistantClient>();
    }

    private static IConfiguration BuildConfig(params (string Key, string Value)[] entries)
        => new ConfigurationBuilder()
            .AddInMemoryCollection(entries.Select(e =>
                new KeyValuePair<string, string?>(e.Key, e.Value)))
            .Build();
}
