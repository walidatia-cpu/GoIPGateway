using FluentValidation;
using GoIPGateway.Application.Common.Models;
using MediatR;

namespace GoIPGateway.Application.Common.Behaviors;

public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
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

        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .Where(r => r.Errors.Count != 0)
            .SelectMany(r => r.Errors)
            .Select(f => Error.Validation(f.PropertyName, f.ErrorMessage))
            .ToList();

        if (failures.Count != 0)
        {
            if (typeof(TResponse).IsGenericType &&
                typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>))
            {
                var resultType = typeof(TResponse).GetGenericArguments()[0];
                var method = typeof(Result)
                    .GetMethods()
                    .First(m => m.Name == nameof(Result.ValidationFailure) && m.IsGenericMethod)
                    .MakeGenericMethod(resultType);

                return (TResponse)method.Invoke(null, new object[] { failures })!;
            }

            if (typeof(TResponse) == typeof(Result))
            {
                return (TResponse)(object)Result.ValidationFailure(failures);
            }

            throw new ValidationException(validationResults.SelectMany(r => r.Errors));
        }

        return await next();
    }
}
