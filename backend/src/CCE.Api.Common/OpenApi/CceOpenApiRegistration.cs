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
                Description = $"CCE Knowledge Center — {title}"
            });
        });
        return services;
    }

    public static IApplicationBuilder UseCceOpenApi(this IApplicationBuilder app, string apiTag = "v1")
    {
        app.UseSwagger(opts =>
        {
            opts.RouteTemplate = $"swagger/{apiTag}/{{documentName}}/swagger.{{json|yaml}}";
        });
        app.UseSwaggerUI(opts =>
        {
            opts.SwaggerEndpoint($"/swagger/{apiTag}/v1/swagger.json", $"CCE {apiTag} API");
            opts.RoutePrefix = $"swagger/{apiTag}";
        });
        return app;
    }
}
