namespace BcMasto.Filters;

using System.ComponentModel.DataAnnotations;

public class ValidationFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        foreach (var arg in context.Arguments)
        {
            if (arg is null)
            {
                continue;
            }

            var validationContext = new ValidationContext(arg);
            var validationResults = new List<ValidationResult>();

            if (!Validator.TryValidateObject(arg, validationContext, validationResults, validateAllProperties: true))
            {
                var errors = validationResults
                    .GroupBy(r => r.MemberNames.FirstOrDefault() ?? string.Empty)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(r => r.ErrorMessage ?? "Invalid value").ToArray());

                return Results.ValidationProblem(errors);
            }
        }

        return await next(context);
    }
}
