using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using FluentValidation;
using System.Net.Http;

namespace MinecraftSkins.Api;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IProblemDetailsService _problemDetailsService;

    public GlobalExceptionHandler(
        ILogger<GlobalExceptionHandler> logger,
        IProblemDetailsService problemDetailsService)
    {
        _logger = logger;
        _problemDetailsService = problemDetailsService;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(
            exception,
            "Unhandled exception occurred. Method: {Method}, Path: {Path}, TraceId: {TraceId}",
            httpContext.Request.Method,
            httpContext.Request.Path,
            httpContext.TraceIdentifier);

        var (statusCode, title, detail) = MapException(exception);

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path
        };

        problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;

        if (exception is ValidationException validationException)
        {
            problemDetails.Extensions["errors"] = validationException.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray()
                );
        }

        httpContext.Response.StatusCode = statusCode;

        return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problemDetails,
            Exception = exception
        });
    }

    private static (int StatusCode, string Title, string Detail) MapException(Exception exception)
    {
        return exception switch
        {
            KeyNotFoundException => (StatusCodes.Status404NotFound, "Not Found", exception.Message),
            ArgumentException => (StatusCodes.Status400BadRequest, "Bad Request", exception.Message),
            ValidationException => (StatusCodes.Status400BadRequest, "Validation Error", "One or more validation errors occurred."),
            InvalidOperationException ex when ex.Message.Contains("unavailable", StringComparison.OrdinalIgnoreCase) 
                => (StatusCodes.Status503ServiceUnavailable, "Service Unavailable", exception.Message),
            InvalidOperationException => (StatusCodes.Status409Conflict, "Conflict", exception.Message), 
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Unauthorized", exception.Message),
            NotImplementedException => (StatusCodes.Status501NotImplemented, "Not Implemented", exception.Message),
            OperationCanceledException => (499, "Client Closed Request", "The operation was canceled."),
            HttpRequestException => (StatusCodes.Status502BadGateway, "External Service Error", "Failed to communicate with an external service."),
            BadHttpRequestException => (StatusCodes.Status400BadRequest, "Bad Request", "Invalid request format or parameters."),
            System.Text.Json.JsonException => (StatusCodes.Status400BadRequest, "Invalid JSON", "The request body contains invalid JSON."),
            _ => (StatusCodes.Status500InternalServerError, "Server Error", "An unexpected error occurred")
        };
    }
}
