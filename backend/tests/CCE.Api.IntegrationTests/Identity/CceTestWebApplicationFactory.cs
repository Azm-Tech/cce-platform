using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace CCE.Api.IntegrationTests.Identity;

/// <summary>
/// Sub-11 — drop-in replacement for <see cref="WebApplicationFactory{TProgram}"/>
/// in IntegrationTests. Swaps the production JwtBearer / Microsoft.Identity.Web
/// auth chain for an in-process <see cref="TestAuthHandler"/> so tests don't
/// require a live IdP.
///
/// Behavior preserved from the real factory: same DI graph, same middleware
/// pipeline, same routes. Only the authentication scheme is replaced.
/// </summary>
public class CceTestWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram>
    where TProgram : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // Drop the JwtBearer scheme's options-configurator that
            // AddCceJwtAuth + AddMicrosoftIdentityWebApi registered. Without
            // this, the real handler still tries to validate the test bearer
            // token as a JWT and fails with IDX14100 ("token has no dots").
            services.RemoveAll<IConfigureOptions<JwtBearerOptions>>();
            services.RemoveAll<IPostConfigureOptions<JwtBearerOptions>>();

            // Register TestAuth scheme + make it the default for both
            // authenticate + challenge. Real schemes still exist (so any
            // explicit scheme reference works); but [Authorize] without an
            // explicit scheme now flows through TestAuthHandler.
            services
                .AddAuthentication(opts =>
                {
                    opts.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                    opts.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                    opts.DefaultScheme = TestAuthHandler.SchemeName;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    TestAuthHandler.SchemeName, _ => { });
        });
    }
}
