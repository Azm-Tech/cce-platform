using CCE.Application.Localization;
using CCE.Domain.Common;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace CCE.Application.Common.Behaviors;

/// <summary>
/// MediatR pipeline behavior for requests returning <see cref="Result{T}"/>.
/// Instead of throwing <see cref="ValidationException"/>, it returns a failure Result
/// with localized messages and structured field-level details.
/// </summary>
public sealed class ResultValidationBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : class
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;
    private readonly IServiceProvider _serviceProvider;

    public ResultValidationBehavior(
        IEnumerable<IValidator<TRequest>> validators,
        IServiceProvider serviceProvider)
    {
        _validators = validators;
        _serviceProvider = serviceProvider;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Only intercept when TResponse is Result<T>
        if (!IsResultType(typeof(TResponse)))
        {
            return await next().ConfigureAwait(false);
        }

        if (!_validators.Any())
            return await next().ConfigureAwait(false);

        var context = new ValidationContext<TRequest>(request);
        var results = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)))
            .ConfigureAwait(false);

        var failures = results.SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count == 0)
            return await next().ConfigureAwait(false);

        var details = failures
            .GroupBy(f => f.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(f => f.ErrorMessage).ToArray());

        var localization = _serviceProvider.GetRequiredService<ILocalizationService>();
        var msg = localization.GetLocalizedMessage("GENERAL_VALIDATION_ERROR");
        var error = new Error(
            "GENERAL_VALIDATION_ERROR",
            msg?.Ar ?? "عذرًا، البيانات المدخلة غير صحيحة",
            msg?.En ?? "Sorry, the entered data is invalid",
            ErrorType.Validation,
            details);

        // Use reflection to call Result<T>.Failure(error)
        var innerType = typeof(TResponse).GetGenericArguments()[0];
        var failureMethod = typeof(Result<>)
            .MakeGenericType(innerType)
            .GetMethod("Failure")!;

        return (TResponse)failureMethod.Invoke(null, [error])!;
    }

    private static bool IsResultType(Type type)
        => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Result<>);
}
