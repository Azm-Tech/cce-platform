using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

namespace CCE.Api.Common.OpenApi;

public static class CceOpenApiRegistration
{
    public static IServiceCollection AddCceOpenApi(this IServiceCollection services, string title)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(opts =>
        {
            opts.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = title,
                Version = "v1",
                Description = "CCE Knowledge Center API — Foundation"
            });
        });
        return services;
    }

    public static IApplicationBuilder UseCceOpenApi(this IApplicationBuilder app)
    {
        app.UseSwagger();
        app.UseSwaggerUI();
        return app;
    }
}
