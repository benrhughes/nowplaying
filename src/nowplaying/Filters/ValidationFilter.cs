// Copyright (c) Ben Hughes. SPDX-License-Identifier: AGPL-3.0-or-later
namespace NowPlaying.Filters;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// A filter that validates the arguments of an endpoint using <see cref="Validator"/>.
/// </summary>
public class ValidationFilter : IEndpointFilter
{
    /// <summary>
    /// Invokes the validation logic.
    /// </summary>
    /// <param name="context">The invocation context.</param>
    /// <param name="next">The next filter in the pipeline.</param>
    /// <returns>The result of the invocation.</returns>
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
