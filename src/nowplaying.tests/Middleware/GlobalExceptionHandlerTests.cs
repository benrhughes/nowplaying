// Copyright (c) Ben Hughes. SPDX-License-Identifier: AGPL-3.0-or-later
namespace NowPlaying.Tests.Middleware;

using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using NowPlaying.Middleware;
using Xunit;

/// <summary>
/// Unit tests for the <see cref="GlobalExceptionHandler"/> class.
/// </summary>
public class GlobalExceptionHandlerTests
{
    private readonly Mock<ILogger<GlobalExceptionHandler>> _loggerMock;
    private readonly GlobalExceptionHandler _handler;

    /// <summary>
    /// Initializes a new instance of the <see cref="GlobalExceptionHandlerTests"/> class.
    /// </summary>
    public GlobalExceptionHandlerTests()
    {
        _loggerMock = new Mock<ILogger<GlobalExceptionHandler>>();
        _handler = new GlobalExceptionHandler(_loggerMock.Object);
    }

    /// <summary>
    /// Verifies that UnauthorizedAccessException is handled and returns a 401 status code.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task TryHandleAsync_WithUnauthorizedAccessException_Returns401()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var exception = new UnauthorizedAccessException("Access denied");

        // Act
        var result = await _handler.TryHandleAsync(context, exception, CancellationToken.None);

        // Assert
        Assert.True(result);
        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
    }

    /// <summary>
    /// Verifies that HttpRequestException with Unauthorized status is handled and returns a 401 status code.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task TryHandleAsync_WithHttpRequestException401_Returns401()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var exception = new HttpRequestException("Unauthorized", null, HttpStatusCode.Unauthorized);

        // Act
        var result = await _handler.TryHandleAsync(context, exception, CancellationToken.None);

        // Assert
        Assert.True(result);
        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
    }

    /// <summary>
    /// Verifies that general exceptions are handled and return a 500 status code.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task TryHandleAsync_WithGeneralException_Returns500()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream(); // To allow writing JSON
        var exception = new Exception("Something went wrong");

        // Act
        var result = await _handler.TryHandleAsync(context, exception, CancellationToken.None);

        // Assert
        Assert.True(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
    }
}
