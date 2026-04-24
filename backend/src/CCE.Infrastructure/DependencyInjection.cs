using CCE.Domain.Common;
using Microsoft.Extensions.DependencyInjection;

namespace CCE.Infrastructure;

/// <summary>
/// Extension methods to register the Infrastructure layer's services on the DI container.
/// Phase 06 expands this to wire EF Core DbContext + Redis + OIDC/JWT + Serilog. Foundation
/// wires only the clock abstraction so Application layer handlers written in Phase 07 have
/// everything they need to resolve via DI.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<ISystemClock, SystemClock>();
        return services;
    }
}
