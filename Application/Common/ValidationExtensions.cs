using FluentValidation;

namespace SamanMobileInsurance.Application.Common;

public static class ValidationExtensions
{
    public static async Task EnsureValidAsync<T>(this IValidator<T> validator, T instance, CancellationToken cancellationToken = default)
    {
        var result = await validator.ValidateAsync(instance, cancellationToken);
        if (!result.IsValid)
        {
            throw new ValidationAppException("خطای اعتبارسنجی", result.Errors.Select(e => e.ErrorMessage).ToList());
        }
    }
}
