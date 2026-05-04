using System.Diagnostics.CodeAnalysis;
using System.Net;
using FluentAssertions;
using Microsoft.Graph;
using Microsoft.Kiota.Abstractions.Authentication;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;

namespace CCE.Infrastructure.Tests.Identity;

/// <summary>
/// Sub-11 Phase 01 — stands up a WireMock server emulating both Microsoft
/// Graph (graph.microsoft.com) and the Entra ID OAuth2 token endpoint
/// (login.microsoftonline.com). Tests pass <see cref="CreateGraphClient"/>'s
/// output to the system under test instead of a real
/// <see cref="GraphServiceClient"/>.
/// </summary>
public sealed class EntraIdFixture : IAsyncLifetime
{
    private const string FixturesDir = "Identity/Fixtures/entra-id-fixtures";

    public WireMockServer Server { get; private set; } = null!;
    public string GraphBaseUrl => Server.Urls[0];

    public Task InitializeAsync()
    {
        Server = WireMockServer.Start();
        StubOAuthToken();
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        Server?.Stop();
        return Task.CompletedTask;
    }

    /// <summary>Resets all WireMock stubs except the OAuth token stub.</summary>
    public void Reset()
    {
        Server.Reset();
        StubOAuthToken();
    }

    /// <summary>Registers POST /v1.0/users → 201 with success fixture.</summary>
    public void StubCreateUserSuccess() => StubCreateUser(HttpStatusCode.Created, "graph-create-user-success.json");

    /// <summary>Registers POST /v1.0/users → 400 (Graph treats UPN-conflict as 400).</summary>
    public void StubCreateUserConflict() => StubCreateUser(HttpStatusCode.BadRequest, "graph-create-user-conflict.json");

    /// <summary>Registers POST /v1.0/users → 403 with insufficient-privileges fixture.</summary>
    public void StubCreateUserForbidden() => StubCreateUser(HttpStatusCode.Forbidden, "graph-create-user-forbidden.json");

    /// <summary>
    /// Builds a <see cref="GraphServiceClient"/> pointed at the WireMock
    /// server. Uses a static-token credential so the OAuth dance is bypassed
    /// inside the SDK; OAuth stub is registered for completeness only.
    /// </summary>
    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope",
        Justification = "HttpClient lifetime is tied to the test scope; the GraphServiceClient holds the only reference and is GC-collected with it. WireMock server stops in DisposeAsync.")]
    public GraphServiceClient CreateGraphClient()
    {
        var auth = new BaseBearerTokenAuthenticationProvider(new StaticAccessTokenProvider("TEST_TOKEN"));
        // Graph SDK 5.x's request builders generate paths like "{baseUrl}/users".
        // Real Graph base URL is "https://graph.microsoft.com/v1.0"; mirror that
        // here so stubs registered at "/v1.0/users" match the actual SDK request.
        var graphRoot = $"{GraphBaseUrl}/v1.0";
        var http = new HttpClient { BaseAddress = new System.Uri(graphRoot) };
        return new GraphServiceClient(http, auth, graphRoot);
    }

    private void StubCreateUser(HttpStatusCode status, string fixtureFile)
    {
        var body = File.ReadAllText(Path.Combine(FixturesDir, fixtureFile));
        Server
            .Given(Request.Create().WithPath("/v1.0/users").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode((int)status)
                .WithHeader("Content-Type", "application/json")
                .WithBody(body));
    }

    private void StubOAuthToken()
    {
        var body = File.ReadAllText(Path.Combine(FixturesDir, "oauth-token.json"));
        Server
            .Given(Request.Create().WithPath("/*/oauth2/v2.0/token").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(body));
    }

    private sealed class StaticAccessTokenProvider : IAccessTokenProvider
    {
        private readonly string _token;
        public StaticAccessTokenProvider(string token) => _token = token;
        public AllowedHostsValidator AllowedHostsValidator { get; } = new();
        public Task<string> GetAuthorizationTokenAsync(System.Uri uri,
            Dictionary<string, object>? additionalAuthenticationContext = null,
            CancellationToken cancellationToken = default)
            => Task.FromResult(_token);
    }
}

[CollectionDefinition(nameof(EntraIdCollection))]
[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix",
    Justification = "xUnit's CollectionDefinition pattern uses 'Collection' as the conventional suffix.")]
public sealed class EntraIdCollection : ICollectionFixture<EntraIdFixture> { }

[Collection(nameof(EntraIdCollection))]
public sealed class EntraIdFixtureSmokeTests
{
    private readonly EntraIdFixture _fixture;
    public EntraIdFixtureSmokeTests(EntraIdFixture fixture) => _fixture = fixture;

    [Fact]
    public void Server_StartsCleanly_WithBaseUrl()
    {
        _fixture.GraphBaseUrl.Should().StartWith("http://");
        _fixture.Server.IsStarted.Should().BeTrue();
    }
}
