using CCE.Application.Localization;
using CCE.Application.Messages;
using CCE.Domain.Common;
using FluentValidation;
using MediatR;

namespace CCE.Application.Common.Behaviors;

public sealed class ResponseValidationBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;
    private readonly ILocalizationService _l;

    public ResponseValidationBehavior(
        IEnumerable<IValidator<TRequest>> validators,
        ILocalizationService l)
    {
        _validators = validators;
        _l = l;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
            return await next().ConfigureAwait(false);

        var context = new ValidationContext<TRequest>(request);
        var results = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken))).ConfigureAwait(false);

        var failures = results
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();

        if (failures.Count == 0)
            return await next().ConfigureAwait(false);

        var responseType = typeof(TResponse);
        if (responseType.IsGenericType &&
            responseType.GetGenericTypeDefinition() == typeof(Response<>))
        {
            var fieldErrors = failures.Select(f =>
            {
                var domainKey = f.ErrorMessage;
                var valCode = SystemCodeMap.ToSystemCode(domainKey);
                var msg = _l.GetString(domainKey);
                return new FieldError(
                    ToCamelCase(f.PropertyName),
                    valCode,
                    msg);
            }).ToList();

            var headerDomainKey = "VALIDATION_ERROR";
            var headerCode = SystemCodeMap.ToSystemCode(headerDomainKey);
            var headerMsg = _l.GetString(headerDomainKey);

            var failMethod = responseType.GetMethod("Fail",
                new[] { typeof(string), typeof(string), typeof(MessageType), typeof(IReadOnlyList<FieldError>) });

            return (TResponse)failMethod!.Invoke(null, new object[]
            {
                headerCode,
                headerMsg,
                MessageType.Validation,
                fieldErrors
            })!;
        }

        throw new ValidationException(failures);
    }

    private static string ToCamelCase(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        return char.ToLowerInvariant(name[0]) + name[1..];
    }
}
