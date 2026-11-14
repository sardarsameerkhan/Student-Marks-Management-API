using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace MarksApi.Infrastructure;

public sealed class ValidationFilter<T> : IEndpointFilter where T : class
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var request = context.Arguments.FirstOrDefault(x => x is T) as T;
        if (request is null)
            return await next(context);

        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(request, new ValidationContext(request), results, validateAllProperties: true);
        foreach (var result in results)
        {
            foreach (var member in result.MemberNames.DefaultIfEmpty("request"))
            {
                var key = JsonNamingPolicy.CamelCase.ConvertName(member);
                var existing = errors.GetValueOrDefault(key, []);
                errors[key] = [.. existing, result.ErrorMessage ?? "The value is invalid."];
            }
        }

        return errors.Count == 0
            ? await next(context)
            : Results.ValidationProblem(errors, statusCode: StatusCodes.Status400BadRequest,
                title: "One or more validation errors occurred.");
    }
}
