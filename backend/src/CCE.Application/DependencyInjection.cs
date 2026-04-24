using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace CCE.Application;

/// <summary>
/// Extension methods to register the Application layer's services on the DI container.
/// Web API composition roots (External/Internal APIs) call <see cref="AddApplication"/> from their
/// <c>Program.cs</c>. Phase 07 adds real handlers (HealthQuery etc.); Foundation wires the infrastructure.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // MediatR — scans this assembly for IRequestHandler<,> implementations
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
        });

        // FluentValidation — scans this assembly for AbstractValidator<T> implementations
        services.AddValidatorsFromAssembly(assembly);

        return services;
    }
}
