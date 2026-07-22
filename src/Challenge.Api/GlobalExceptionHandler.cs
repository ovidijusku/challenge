using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Challenge.Api;

/// <summary>
/// Converts unhandled exceptions into consistent RFC 7807 ProblemDetails responses.
/// Unique-constraint violations (e.g. duplicate email) and foreign-key conflicts
/// (e.g. deleting a user that still has transactions) are surfaced as 409 Conflict
/// instead of a generic 500.
/// </summary>
public sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    // SQL Server error numbers.
    private const int DuplicateKeyRow = 2601;    // unique index violation
    private const int UniqueConstraint = 2627;   // unique constraint violation
    private const int ReferenceConstraint = 547; // foreign-key / reference conflict

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title) = exception switch
        {
            DbUpdateException { InnerException: SqlException { Number: DuplicateKeyRow or UniqueConstraint } }
                => (StatusCodes.Status409Conflict, "A record with the same unique value already exists."),
            DbUpdateException { InnerException: SqlException { Number: ReferenceConstraint } }
                => (StatusCodes.Status409Conflict, "The operation conflicts with related data and cannot be completed."),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred."),
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception");
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
}

