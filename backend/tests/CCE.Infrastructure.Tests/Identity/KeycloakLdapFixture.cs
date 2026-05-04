using System.Diagnostics.CodeAnalysis;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using Testcontainers.Keycloak;
using Xunit;

namespace CCE.Infrastructure.Tests.Identity;

/// <summary>
/// xUnit fixture that boots one Keycloak container + one OpenLDAP container
/// on a shared docker network. Shared across all tests in
/// <see cref="KeycloakLdapCollection"/>. OpenLDAP is seeded from a fixture
/// LDIF (one user 'alice' under OU=Users, one group 'CCE-Admins' under
/// OU=Groups). Each test should clean up any state it modifies (drop the
/// federation provider before exiting).
/// </summary>
public sealed class KeycloakLdapFixture : IAsyncLifetime
{
    private const string LdapAdminPassword = "admin-pass-1234";
    private const string LdapBaseDn        = "DC=cce,DC=local";
    private const string LdapBindDn        = "cn=admin,DC=cce,DC=local";

    public INetwork Network { get; }
    public KeycloakContainer Keycloak { get; }
    public IContainer OpenLdap { get; }

    public const string KeycloakAdminUser     = "admin";
    public const string KeycloakAdminPassword = "admin";
    public const string LdapHost              = "openldap";       // hostname inside the docker network
    public const int    LdapPort              = 1389;             // bitnami openldap default
    public const string LdapBindDnPublic      = LdapBindDn;
    public const string LdapBindPasswordPublic= LdapAdminPassword;
    public const string LdapUsersDnPublic     = "OU=Users," + LdapBaseDn;
    public const string LdapGroupsDnPublic    = "OU=Groups," + LdapBaseDn;

    public KeycloakLdapFixture()
    {
        Network = new NetworkBuilder()
            .WithName($"cce-keycloak-ldap-{Guid.NewGuid():N}")
            .Build();

        Keycloak = new KeycloakBuilder()
            .WithImage("quay.io/keycloak/keycloak:26.0")
            .WithUsername(KeycloakAdminUser)
            .WithPassword(KeycloakAdminPassword)
            .WithNetwork(Network)
            .Build();

        OpenLdap = new ContainerBuilder()
            .WithImage("bitnami/openldap:2.6")
            .WithNetwork(Network)
            .WithNetworkAliases("openldap")
            .WithEnvironment("LDAP_ADMIN_USERNAME", "admin")
            .WithEnvironment("LDAP_ADMIN_PASSWORD", LdapAdminPassword)
            .WithEnvironment("LDAP_ROOT", LdapBaseDn)
            .WithEnvironment("LDAP_PORT_NUMBER", "1389")
            .WithEnvironment("LDAP_USERS", "alice")
            .WithEnvironment("LDAP_PASSWORDS", "alice-pass")
            .WithPortBinding(1389, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(1389))
            .Build();
    }

    public async Task InitializeAsync()
    {
        await Network.CreateAsync().ConfigureAwait(false);
        // Boot Keycloak + OpenLDAP in parallel.
        var kcTask = Keycloak.StartAsync();
        var ldapTask = OpenLdap.StartAsync();
        await Task.WhenAll(kcTask, ldapTask).ConfigureAwait(false);
    }

    public async Task DisposeAsync()
    {
        await Keycloak.DisposeAsync().ConfigureAwait(false);
        await OpenLdap.DisposeAsync().ConfigureAwait(false);
        await Network.DisposeAsync().ConfigureAwait(false);
    }
}

[CollectionDefinition(nameof(KeycloakLdapCollection))]
[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix",
    Justification = "xUnit's CollectionDefinition pattern uses 'Collection' as the conventional suffix.")]
public sealed class KeycloakLdapCollection : ICollectionFixture<KeycloakLdapFixture> { }
