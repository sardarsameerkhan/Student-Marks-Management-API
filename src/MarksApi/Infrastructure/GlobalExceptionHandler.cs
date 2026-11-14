using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using MarksApi.Services;

namespace MarksApi.Infrastructure;

public sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title, detail) = exception switch
        {
            ApiNotFoundException ex => (404, "Not found", ex.Message),
            ApiConflictException ex => (409, "Conflict", ex.Message),
            ApiForbiddenException ex => (403, "Forbidden", ex.Message),
            ApiUnauthorizedException ex => (401, "Unauthorized", ex.Message),
            DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } } =>
                (409, "Conflict", "A resource with the same unique value already exists."),
            DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation } } =>
                (409, "Conflict", "The operation conflicts with related records."),
            _ => (500, "Internal Server Error", "An unexpected error occurred.")
        };

        if (status == 500)
            logger.LogError(exception, "Unhandled API exception");
        else if (status >= 500)
            logger.LogError(exception, "API request failed with status {StatusCode}", status);
        else
            logger.LogInformation("API request rejected with status {StatusCode}: {Title}", status, title);

        httpContext.Response.StatusCode = status;
        var problem = new ProblemDetails { Status = status, Title = title, Detail = detail };
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception
        });
    }
}
