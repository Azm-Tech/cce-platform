namespace CCE.Api.IntegrationTests.Identity;

/// <summary>
/// Sub-11 — issues a test bearer token consumed by
/// <see cref="TestAuthHandler"/> to synthesize a <c>cce-admin</c> principal
/// in-process. No network calls; no live IdP dependency.
///
/// Pre-Sub-11 this fixture made an HTTP POST to the dev Keycloak
/// realm's token endpoint to mint a service-account JWT under the
/// <c>cce-admin-cms</c> client. Phase 04 deleted Keycloak; the entire
/// fixture is now a single string constant.
/// </summary>
public sealed class AdminAuthFixture : IAsyncLifetime
{
    /// <summary>
    /// Bearer token value for admin-scoped tests. <see cref="TestAuthHandler"/>
    /// reads this as the role name and synthesizes a principal with
    /// <c>roles=cce-admin</c>.
    /// </summary>
    public string AccessToken { get; } = "cce-admin";

    public Task InitializeAsync() => Task.CompletedTask;
    public Task DisposeAsync()    => Task.CompletedTask;
}
