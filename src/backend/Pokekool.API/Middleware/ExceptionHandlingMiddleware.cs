using System.Net;
using System.Text.Json;
using FluentValidation;
using Pokekool.Domain.Common;
using Pokekool.Shared.Dtos.Common;

namespace Pokekool.API.Middleware;

/// <summary>
/// Centralise la conversion des exceptions en réponses RFC 7807.
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException vx)
        {
            _logger.LogWarning(vx, "Validation failed");
            var errors = vx.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => string.IsNullOrEmpty(g.Key) ? "_" : g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray());
            await WriteAsync(context, HttpStatusCode.BadRequest,
                "https://pokekool.app/errors/validation",
                "Validation failed",
                "One or more fields are invalid.",
                errors);
        }
        catch (DomainException dx) when (dx.Code.StartsWith("auth.", StringComparison.Ordinal))
        {
            _logger.LogWarning(dx, "Auth failure: {Code}", dx.Code);
            await WriteAsync(context, HttpStatusCode.Unauthorized,
                "https://pokekool.app/errors/auth",
                "Authentication failed",
                dx.Message);
        }
        catch (DomainException dx) when (dx.Code.EndsWith(".not_found", StringComparison.Ordinal))
        {
            _logger.LogInformation(dx, "Resource not found: {Code}", dx.Code);
            await WriteAsync(context, HttpStatusCode.NotFound,
                "https://pokekool.app/errors/not-found",
                "Resource not found",
                dx.Message);
        }
        catch (DomainException dx) when (dx.Code.EndsWith(".conflict", StringComparison.Ordinal)
                                       || dx.Code.Contains("_conflict", StringComparison.Ordinal))
        {
            _logger.LogInformation(dx, "Conflict: {Code}", dx.Code);
            await WriteAsync(context, HttpStatusCode.Conflict,
                "https://pokekool.app/errors/conflict",
                "Resource conflict",
                dx.Message);
        }
        catch (DomainException dx) when (dx.Code.Contains("forbidden", StringComparison.Ordinal))
        {
            _logger.LogInformation(dx, "Forbidden: {Code}", dx.Code);
            await WriteAsync(context, HttpStatusCode.Forbidden,
                "https://pokekool.app/errors/forbidden",
                "Access forbidden",
                dx.Message);
        }
        catch (DomainException dx)
        {
            _logger.LogWarning(dx, "Domain rule violation: {Code}", dx.Code);
            await WriteAsync(context, HttpStatusCode.UnprocessableEntity,
                "https://pokekool.app/errors/domain",
                "Domain rule violation",
                dx.Message);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // Requête annulée (client déconnecté ou shutdown) — pas une erreur applicative
            _logger.LogDebug("Request cancelled (client disconnect or app shutdown)");
            context.Response.StatusCode = 499; // Client Closed Request
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            await WriteAsync(context, HttpStatusCode.InternalServerError,
                "https://pokekool.app/errors/internal",
                "Internal server error",
                "An unexpected error occurred.");
        }
    }

    private static Task WriteAsync(
        HttpContext ctx,
        HttpStatusCode status,
        string type,
        string title,
        string detail,
        IReadOnlyDictionary<string, string[]>? errors = null)
    {
        ctx.Response.ContentType = "application/problem+json";
        ctx.Response.StatusCode = (int)status;

        var payload = new ProblemDetailsDto(
            Type: type,
            Title: title,
            Status: (int)status,
            Detail: detail,
            Instance: ctx.Request.Path,
            TraceId: ctx.TraceIdentifier,
            Errors: errors);

        return ctx.Response.WriteAsync(JsonSerializer.Serialize(payload, JsonOptions));
    }
}
