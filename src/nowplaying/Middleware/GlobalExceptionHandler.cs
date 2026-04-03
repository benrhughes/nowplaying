// Copyright (c) Ben Hughes. SPDX-License-Identifier: AGPL-3.0-or-later
using System.Net;
using Microsoft.AspNetCore.Diagnostics;
using NowPlaying.Models;

namespace NowPlaying.Middleware;

/// <summary>
/// Global exception handler for processing unhandled exceptions.
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GlobalExceptionHandler"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if ((exception is HttpRequestException httpEx && httpEx.StatusCode == HttpStatusCode.Unauthorized) ||
            exception is UnauthorizedAccessException)
        {
            _logger.LogWarning("Unauthorized access detected: {Message}", exception.Message);
            httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return true;
        }

        // Handle other specific exceptions if needed, or fall through
        _logger.LogError(exception, "An unhandled exception occurred");

        // Return 500 here for consistent error response
        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(new ErrorResponse("An unexpected error occurred."), cancellationToken);
        return true;
    }
}
