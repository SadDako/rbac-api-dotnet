using FluentValidation;
using MediatR;
using Rbac.Shared;

namespace Rbac.Application.Behaviors;

public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : Result
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!_validators.Any())
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);
        var failures = (await Task.WhenAll(_validators.Select(v => v.ValidateAsync(context, cancellationToken))))
            .SelectMany(result => result.Errors)
            .Where(error => error is not null)
            .ToList();

        if (failures.Any())
        {
            var errors = failures.Select(e => e.ErrorMessage).Distinct().ToArray();
            if (typeof(TResponse) == typeof(Result))
            {
                return (TResponse)(object)Result.Failure(errors);
            }

            if (typeof(TResponse).IsGenericType && typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>))
            {
                var valueType = typeof(TResponse).GenericTypeArguments[0];
                var failureMethod = typeof(Result).GetMethod("Failure", new[] { typeof(string[]) })?.MakeGenericMethod(valueType);
                if (failureMethod is not null)
                {
                    return (TResponse)failureMethod.Invoke(null, new object[] { errors })!;
                }
            }

            throw new InvalidOperationException("Unsupported response type for validation behavior.");
        }

        return await next();
    }
}
