using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace CCE.Infrastructure.Tests.Identity;

[Collection(nameof(KeycloakLdapCollection))]
[SuppressMessage("Usage", "CA2234:Pass system uri objects instead of strings",
    Justification = "Test code; relative URLs against fixture's HttpClient.BaseAddress are clearer as strings.")]
public sealed class KeycloakLdapFederationTests : IDisposable
{
    private readonly KeycloakLdapFixture _fixture;
    private readonly HttpClient _http;

    public KeycloakLdapFederationTests(KeycloakLdapFixture fixture)
    {
        _fixture = fixture;
        _http = new HttpClient { BaseAddress = new Uri(fixture.Keycloak.GetBaseAddress()) };
    }

    public void Dispose()
    {
        _http.Dispose();
    }

    private async Task<string> AcquireMasterAdminTokenAsync()
    {
        using var form = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "password"),
            new KeyValuePair<string, string>("client_id",  "admin-cli"),
            new KeyValuePair<string, string>("username",   KeycloakLdapFixture.KeycloakAdminUser),
            new KeyValuePair<string, string>("password",   KeycloakLdapFixture.KeycloakAdminPassword),
        });
        var resp = await _http.PostAsync("/realms/master/protocol/openid-connect/token", form);
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("access_token").GetString()!;
    }

    private async Task<string> EnsureRealmExistsAsync(string token)
    {
        // Master admin can create realms via POST /admin/realms.
        const string realmName = "cce";
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var get = await _http.GetAsync($"/admin/realms/{realmName}");
        if (get.IsSuccessStatusCode) return realmName;

        var body = JsonSerializer.Serialize(new { realm = realmName, enabled = true });
        using var content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");
        var post = await _http.PostAsync("/admin/realms", content);
        post.EnsureSuccessStatusCode();
        return realmName;
    }

    private static object BuildFederationProviderBody() => new
    {
        name = "cce-ldap",
        providerId = "ldap",
        providerType = "org.keycloak.storage.UserStorageProvider",
        config = new Dictionary<string, string[]>
        {
            ["enabled"]                  = new[] { "true" },
            ["vendor"]                   = new[] { "ad" },
            ["connectionUrl"]            = new[] { $"ldap://{KeycloakLdapFixture.LdapHost}:{KeycloakLdapFixture.LdapPort}" },
            ["authType"]                 = new[] { "simple" },
            ["bindDn"]                   = new[] { KeycloakLdapFixture.LdapBindDnPublic },
            ["bindCredential"]           = new[] { KeycloakLdapFixture.LdapBindPasswordPublic },
            ["editMode"]                 = new[] { "READ_ONLY" },
            ["syncRegistrations"]        = new[] { "false" },
            ["importEnabled"]            = new[] { "true" },
            ["usersDn"]                  = new[] { KeycloakLdapFixture.LdapUsersDnPublic },
            ["searchScope"]              = new[] { "2" },
            ["usernameLDAPAttribute"]    = new[] { "sAMAccountName" },
            ["rdnLDAPAttribute"]         = new[] { "cn" },
            ["uuidLDAPAttribute"]        = new[] { "objectGUID" },
            ["userObjectClasses"]        = new[] { "person, organizationalPerson, user" },
            ["validatePasswordPolicy"]   = new[] { "false" },
            ["trustEmail"]               = new[] { "true" },
            ["pagination"]               = new[] { "true" },
            ["batchSizeForSync"]         = new[] { "1000" },
            ["fullSyncPeriod"]           = new[] { "-1" },
            ["changedSyncPeriod"]        = new[] { "-1" },
        }
    };

    [Fact]
    public async Task FederationProvider_CreatesViaPost_OnFreshRealm()
    {
        var token = await AcquireMasterAdminTokenAsync();
        var realm = await EnsureRealmExistsAsync(token);

        var body = BuildFederationProviderBody();
        var bodyJson = JsonSerializer.Serialize(body);

        using var content = new StringContent(bodyJson, System.Text.Encoding.UTF8, "application/json");
        var post = await _http.PostAsync($"/admin/realms/{realm}/components", content);
        post.IsSuccessStatusCode.Should().BeTrue($"POST should create the federation component (status={post.StatusCode}, body={await post.Content.ReadAsStringAsync()})");

        // Verify the component is now visible.
        var listResp = await _http.GetAsync(
            $"/admin/realms/{realm}/components?type=org.keycloak.storage.UserStorageProvider");
        listResp.EnsureSuccessStatusCode();
        var components = await listResp.Content.ReadFromJsonAsync<JsonElement[]>();
        components.Should().NotBeNull();
        components!.Should().Contain(c => c.GetProperty("name").GetString() == "cce-ldap");
    }

    [Fact]
    public async Task FederationProvider_PutIsIdempotent_OnSecondApply()
    {
        var token = await AcquireMasterAdminTokenAsync();
        var realm = await EnsureRealmExistsAsync(token);

        var body = BuildFederationProviderBody();
        var bodyJson = JsonSerializer.Serialize(body);

        // First apply: POST.
        using (var content = new StringContent(bodyJson, System.Text.Encoding.UTF8, "application/json"))
        {
            await _http.PostAsync($"/admin/realms/{realm}/components", content);
        }

        // Lookup id.
        var listResp = await _http.GetAsync(
            $"/admin/realms/{realm}/components?type=org.keycloak.storage.UserStorageProvider");
        var components = await listResp.Content.ReadFromJsonAsync<JsonElement[]>();
        var existing = components!.First(c => c.GetProperty("name").GetString() == "cce-ldap");
        var compId = existing.GetProperty("id").GetString()!;
        var parentId = existing.GetProperty("parentId").GetString()!;

        // Second apply: PUT with id + parentId. Reuse the body's config.
        var configValue = ((Dictionary<string, string[]>)((dynamic)body).config);
        var updateBody = new Dictionary<string, object?>
        {
            ["id"]           = compId,
            ["parentId"]     = parentId,
            ["name"]         = "cce-ldap",
            ["providerId"]   = "ldap",
            ["providerType"] = "org.keycloak.storage.UserStorageProvider",
            ["config"]       = configValue
        };
        var updateJson = JsonSerializer.Serialize(updateBody);
        using var updateContent = new StringContent(updateJson, System.Text.Encoding.UTF8, "application/json");
        var put = await _http.PutAsync($"/admin/realms/{realm}/components/{compId}", updateContent);
        put.IsSuccessStatusCode.Should().BeTrue("PUT should be idempotent — no error on re-apply");

        // Verify the component still exists post-PUT (Keycloak rejects POST
        // of duplicate names within a realm, so the count is implicitly 1
        // — the assertion here is "PUT didn't accidentally delete the row").
        var listResp2 = await _http.GetAsync(
            $"/admin/realms/{realm}/components?type=org.keycloak.storage.UserStorageProvider");
        var components2 = await listResp2.Content.ReadFromJsonAsync<JsonElement[]>();
        components2!.Should().Contain(c => c.GetProperty("name").GetString() == "cce-ldap",
            "PUT should leave the component in place");
    }

    [Fact]
    public async Task GroupMapper_AttachesAsChildOfFederationProvider()
    {
        var token = await AcquireMasterAdminTokenAsync();
        var realm = await EnsureRealmExistsAsync(token);

        // Create the parent.
        var parentBody = BuildFederationProviderBody();
        var parentJson = JsonSerializer.Serialize(parentBody);
        using (var parentContent = new StringContent(parentJson, System.Text.Encoding.UTF8, "application/json"))
        {
            await _http.PostAsync($"/admin/realms/{realm}/components", parentContent);
        }

        var listResp = await _http.GetAsync(
            $"/admin/realms/{realm}/components?type=org.keycloak.storage.UserStorageProvider");
        var components = await listResp.Content.ReadFromJsonAsync<JsonElement[]>();
        var parentId = components!.First(c => c.GetProperty("name").GetString() == "cce-ldap")
                                  .GetProperty("id").GetString()!;

        // Attach the group mapper.
        var mapperBody = new
        {
            name = "cce-group-mapper",
            providerId = "group-ldap-mapper",
            providerType = "org.keycloak.storage.ldap.mappers.LDAPStorageMapper",
            parentId = parentId,
            config = new Dictionary<string, string[]>
            {
                ["groups.dn"]                              = new[] { KeycloakLdapFixture.LdapGroupsDnPublic },
                ["group.name.ldap.attribute"]              = new[] { "cn" },
                ["group.object.classes"]                   = new[] { "group" },
                ["membership.ldap.attribute"]              = new[] { "member" },
                ["membership.attribute.type"]              = new[] { "DN" },
                ["membership.user.ldap.attribute"]         = new[] { "sAMAccountName" },
                ["preserve.group.inheritance"]             = new[] { "false" },
                ["ignore.missing.groups"]                  = new[] { "true" },
                ["user.roles.retrieve.strategy"]           = new[] { "LOAD_GROUPS_BY_MEMBER_ATTRIBUTE" },
                ["mapped.group.attributes"]                = new[] { "" },
                ["mode"]                                   = new[] { "READ_ONLY" },
                ["drop.non.existing.groups.during.sync"]   = new[] { "false" },
                ["groups.path"]                            = new[] { "/" },
            }
        };
        var mapperJson = JsonSerializer.Serialize(mapperBody);
        using var mapperContent = new StringContent(mapperJson, System.Text.Encoding.UTF8, "application/json");
        var post = await _http.PostAsync($"/admin/realms/{realm}/components", mapperContent);
        post.IsSuccessStatusCode.Should().BeTrue("POST should attach the group mapper");

        // Verify it's listed under the parent.
        var mappersResp = await _http.GetAsync(
            $"/admin/realms/{realm}/components?type=org.keycloak.storage.ldap.mappers.LDAPStorageMapper&parent={parentId}");
        var mappers = await mappersResp.Content.ReadFromJsonAsync<JsonElement[]>();
        mappers!.Should().Contain(m => m.GetProperty("name").GetString() == "cce-group-mapper");
    }
}
