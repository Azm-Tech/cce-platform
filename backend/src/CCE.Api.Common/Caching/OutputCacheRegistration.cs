using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CCE.Api.Common.Caching;

public static class OutputCacheRegistration
{
    public static IServiceCollection AddCceOutputCache(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<OutputCacheOptions>()
            .Bind(configuration.GetSection(OutputCacheOptions.SectionName));
        return services;
    }

    public static IApplicationBuilder UseCceOutputCache(this IApplicationBuilder app)
        => app.UseMiddleware<RedisOutputCacheMiddleware>();
}
