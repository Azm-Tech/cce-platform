using System.Globalization;
using CCE.Api.Common.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;

namespace CCE.Api.IntegrationTests.Middleware;

public class LocalizationMiddlewareTests
{
    private static IHost BuildTestHost() =>
        new HostBuilder()
            .ConfigureWebHost(web =>
            {
                web.UseTestServer();
                web.Configure(app =>
                {
                    app.UseMiddleware<LocalizationMiddleware>();
                    app.Run(c => c.Response.WriteAsync(CultureInfo.CurrentCulture.Name));
                });
            })
            .Start();

    [Theory]
    [InlineData("ar", "ar")]
    [InlineData("en", "en")]
    [InlineData("en-US,en;q=0.9,ar;q=0.8", "en")]
    [InlineData("fr", "ar")]                    // unsupported -> default ar
    [InlineData("", "ar")]                       // empty -> default ar
    public async Task Selects_supported_locale_or_falls_back_to_ar(string acceptLanguage, string expected)
    {
        using var host = BuildTestHost();
        var client = host.GetTestClient();
        if (!string.IsNullOrEmpty(acceptLanguage))
        {
            client.DefaultRequestHeaders.Add("Accept-Language", acceptLanguage);
        }

        var resp = await client.GetAsync(new Uri("/", UriKind.Relative));

        var body = await resp.Content.ReadAsStringAsync();
        body.Should().Be(expected);
    }
}
