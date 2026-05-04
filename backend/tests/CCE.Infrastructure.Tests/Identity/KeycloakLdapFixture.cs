using System.Diagnostics.CodeAnalysis;
using Testcontainers.Keycloak;
using Xunit;

namespace CCE.Infrastructure.Tests.Identity;

/// <summary>
/// xUnit fixture that boots one Keycloak container. Shared across all
/// tests in <see cref="KeycloakLdapCollection"/>.
///
/// We do NOT boot a real LDAP container — the federation-provider tests
/// only exercise Keycloak's admin REST API behaviour (POST/PUT/GET on the
/// /components endpoint). They don't call "Test connection" or attempt
/// actual LDAP authentication, which would require live LDAP. End-to-end
/// LDAP federation against a real AD is exercised manually per the
/// AD federation runbook (docs/runbooks/ad-federation.md).
/// </summary>
public sealed class KeycloakLdapFixture : IAsyncLifetime
{
    private const string LdapBaseDn = "DC=cce,DC=local";
    private const string LdapBindDn = "cn=admin,DC=cce,DC=local";

    public KeycloakContainer Keycloak { get; }

    public const string KeycloakAdminUser     = "admin";
    public const string KeycloakAdminPassword = "admin";
    // Synthetic LDAP config — Keycloak accepts these in the federation
    // provider POST without validating reachability until "Test connection"
    // is explicitly invoked (which our tests don't do).
    public const string LdapHost              = "openldap.test.invalid";
    public const int    LdapPort              = 389;
    public const string LdapBindDnPublic      = LdapBindDn;
    public const string LdapBindPasswordPublic= "synthetic-bind-password";
    public const string LdapUsersDnPublic     = "OU=Users," + LdapBaseDn;
    public const string LdapGroupsDnPublic    = "OU=Groups," + LdapBaseDn;

    public KeycloakLdapFixture()
    {
        Keycloak = new KeycloakBuilder()
            .WithImage("quay.io/keycloak/keycloak:26.0")
            .WithUsername(KeycloakAdminUser)
            .WithPassword(KeycloakAdminPassword)
            .Build();
    }

    public async Task InitializeAsync() => await Keycloak.StartAsync().ConfigureAwait(false);
    public async Task DisposeAsync()    => await Keycloak.DisposeAsync().ConfigureAwait(false);
}

[CollectionDefinition(nameof(KeycloakLdapCollection))]
[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix",
    Justification = "xUnit's CollectionDefinition pattern uses 'Collection' as the conventional suffix.")]
public sealed class KeycloakLdapCollection : ICollectionFixture<KeycloakLdapFixture> { }
