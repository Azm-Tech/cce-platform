using CCE.Application.Common.Behaviors;
using CCE.Application.Messages;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace CCE.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddOpenBehavior(typeof(ResponseValidationBehavior<,>));
            // Last: runs after the handler commits; evicts cache regions for ICacheInvalidatingRequest.
            cfg.AddOpenBehavior(typeof(CacheInvalidationBehavior<,>));
        });

        services.AddValidatorsFromAssembly(assembly);

        services.AddScoped<MessageFactory>();
        services.AddScoped<CCE.Application.Community.Public.FeedHydratorService>();
        services.AddScoped<CCE.Application.Identity.Public.Commands.ContactChangeOtpService>();
        services.AddScoped<CCE.Application.Content.IUserContentInterestResolver, CCE.Application.Content.UserContentInterestResolver>();

        services.AddSingleton<Reports.ICsvStreamWriter, Reports.CsvStreamWriter>();

        return services;
    }
}
