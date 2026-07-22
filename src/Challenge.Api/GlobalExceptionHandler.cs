using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Challenge.Api;

/// <summary>
/// Converts unhandled exceptions into consistent RFC 7807 ProblemDetails responses.
/// Unique-constraint violations (e.g. duplicate email) are surfaced as 409 Conflict
/// instead of a generic 500.
/// </summary>
public sealed partial class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    // SQL Server error numbers for unique index / unique constraint violations.
    private const int DuplicateKeyRow = 2601;
    private const int UniqueConstraint = 2627;

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title) = exception switch
        {
            DbUpdateException { InnerException: SqlException { Number: DuplicateKeyRow or UniqueConstraint } }
                => (StatusCodes.Status409Conflict, "A record with the same unique value already exists."),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred."),
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            LogUnhandledException(logger, exception);
        }

        httpContext.Response.StatusCode = statusCode;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
            },
        });
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Unhandled exception")]
    private static partial void LogUnhandledException(ILogger logger, Exception exception);
}

