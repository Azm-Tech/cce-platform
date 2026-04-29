using System.Net;
using System.Net.Http;
using CCE.Api.Common.Auth;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CCE.Api.IntegrationTests.Auth;

public class BffSessionMiddlewareTests
{
    [Fact]
    public async Task No_cookie_passes_through_without_auth_header()
    {
        using var host = await BuildHostAsync(_ => { });
        using var client = host.GetTestClient();

        var resp = await client.GetAsync(new Uri("/", UriKind.Relative));

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Valid_cookie_synthesizes_bearer_header()
    {
        var captured = new System.Threading.Tasks.TaskCompletionSource<string?>();
        using var host = await BuildHostAsync(ctx =>
        {
            captured.TrySetResult(ctx.Request.Headers.Authorization.ToString());
        });
        using var client = host.GetTestClient();

        var session = new BffSession("access-token-x", "refresh-y", System.DateTimeOffset.UtcNow.AddMinutes(20));
        var encrypted = EncryptForCookie(host.Services, session);
        client.DefaultRequestHeaders.Add("Cookie", $"{BffSessionCookie.CookieName}={encrypted}");

        await client.GetAsync(new Uri("/", UriKind.Relative));

        var auth = await captured.Task;
        auth.Should().Be("Bearer access-token-x");
    }

    [Fact]
    public async Task Malformed_cookie_passes_through_without_throwing()
    {
        using var host = await BuildHostAsync(_ => { });
        using var client = host.GetTestClient();
        client.DefaultRequestHeaders.Add("Cookie", $"{BffSessionCookie.CookieName}=garbage");

        var resp = await client.GetAsync(new Uri("/", UriKind.Relative));

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Expired_cookie_with_refresh_failure_clears_and_returns_401()
    {
        // The fake IHttpClientFactory returns a 400 on the refresh call, simulating Keycloak rejection.
        using var host = await BuildHostAsync(
            _ => { },
            refreshHandler: (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)));
        using var client = host.GetTestClient();

        var expired = new BffSession("access", "refresh", System.DateTimeOffset.UtcNow.AddMinutes(-5));
        var encrypted = EncryptForCookie(host.Services, expired);
        client.DefaultRequestHeaders.Add("Cookie", $"{BffSessionCookie.CookieName}={encrypted}");

        var resp = await client.GetAsync(new Uri("/", UriKind.Relative));

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        // Clear-cookie header should be present
        resp.Headers.Should().Contain(h => h.Key.Equals("Set-Cookie", System.StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Expired_cookie_with_refresh_success_rotates_and_synthesizes_new_bearer()
    {
        var captured = new System.Threading.Tasks.TaskCompletionSource<string?>();
        using var host = await BuildHostAsync(
            ctx => captured.TrySetResult(ctx.Request.Headers.Authorization.ToString()),
            refreshHandler: (_, _) =>
            {
                var msg = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        """{"access_token":"new-access","refresh_token":"new-refresh","expires_in":1800}""",
                        System.Text.Encoding.UTF8,
                        "application/json")
                };
                return Task.FromResult(msg);
            });
        using var client = host.GetTestClient();

        var expired = new BffSession("access", "refresh", System.DateTimeOffset.UtcNow.AddMinutes(-5));
        var encrypted = EncryptForCookie(host.Services, expired);
        client.DefaultRequestHeaders.Add("Cookie", $"{BffSessionCookie.CookieName}={encrypted}");

        await client.GetAsync(new Uri("/", UriKind.Relative));

        var auth = await captured.Task;
        auth.Should().Be("Bearer new-access");
    }

    private static string EncryptForCookie(IServiceProvider services, BffSession session)
    {
        var provider = services.GetRequiredService<IDataProtectionProvider>();
        var protector = provider.CreateProtector("cce.bff.session.v1");
        var json = System.Text.Json.JsonSerializer.Serialize(session);
        return protector.Protect(json);
    }

    private static async Task<IHost> BuildHostAsync(
        System.Action<HttpContext> observe,
        System.Func<HttpRequestMessage, System.Threading.CancellationToken, Task<HttpResponseMessage>>? refreshHandler = null)
    {
        var host = new HostBuilder()
            .ConfigureWebHost(web =>
            {
                web.UseTestServer();
                web.ConfigureServices(services =>
                {
                    services.AddDataProtection();
                    services.Configure<BffOptions>(o =>
                    {
                        o.KeycloakBaseUrl = "http://stub";
                        o.KeycloakRealm = "test";
                        o.KeycloakClientId = "test-client";
                        o.KeycloakClientSecret = "test-secret";
                        o.CookieDomain = "localhost";
                        o.SessionLifetimeMinutes = 30;
                    });
                    services.AddSingleton<BffSessionCookie>();
                    services.AddSingleton<BffTokenRefresher>();
                    var handler = refreshHandler
                        ?? ((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)));
                    services.AddSingleton<IHttpClientFactory>(_ => new FakeHttpClientFactory(handler));
                    services.AddLogging();
                });
                web.Configure(app =>
                {
                    app.UseMiddleware<BffSessionMiddleware>();
                    app.Run(ctx =>
                    {
                        observe(ctx);
                        return Task.CompletedTask;
                    });
                });
            })
            .Build();

        await host.StartAsync().ConfigureAwait(false);
        return host;
    }

    private sealed class FakeHttpClientFactory : IHttpClientFactory
    {
        private readonly System.Func<HttpRequestMessage, System.Threading.CancellationToken, Task<HttpResponseMessage>> _handler;

        public FakeHttpClientFactory(System.Func<HttpRequestMessage, System.Threading.CancellationToken, Task<HttpResponseMessage>> handler)
        {
            _handler = handler;
        }

        public HttpClient CreateClient(string name)
        {
            // HttpClient takes ownership of the handler and disposes it — CA2000 is a false positive here.
#pragma warning disable CA2000
            return new HttpClient(new DelegatingFakeHandler(_handler));
#pragma warning restore CA2000
        }

        private sealed class DelegatingFakeHandler : HttpMessageHandler
        {
            private readonly System.Func<HttpRequestMessage, System.Threading.CancellationToken, Task<HttpResponseMessage>> _handler;

            public DelegatingFakeHandler(System.Func<HttpRequestMessage, System.Threading.CancellationToken, Task<HttpResponseMessage>> handler)
            {
                _handler = handler;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
                => _handler(request, cancellationToken);
        }
    }
}
