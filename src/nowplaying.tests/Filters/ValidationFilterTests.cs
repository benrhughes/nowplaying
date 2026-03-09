// Copyright (c) Ben Hughes. SPDX-License-Identifier: AGPL-3.0-or-later
namespace NowPlaying.Tests.Filters;

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Moq;
using NowPlaying.Filters;
using Xunit;

/// <summary>
/// Unit tests for the <see cref="ValidationFilter"/> class.
/// </summary>
public class ValidationFilterTests
{
    private readonly ValidationFilter _filter;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationFilterTests"/> class.
    /// </summary>
    public ValidationFilterTests()
    {
        _filter = new ValidationFilter();
    }

    /// <summary>
    /// Verifies that InvokeAsync calls the next filter when the model is valid.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task InvokeAsync_WithValidObject_CallsNext()
    {
        // Arrange
        var contextMock = new Mock<EndpointFilterInvocationContext>();
        var validObject = new TestModel { Name = "Valid" };
        contextMock.Setup(c => c.Arguments).Returns(new List<object?> { validObject });

        var nextCalled = false;
        EndpointFilterDelegate next = (context) =>
        {
            nextCalled = true;
            return ValueTask.FromResult<object?>(Results.Ok());
        };

        // Act
        await _filter.InvokeAsync(contextMock.Object, next);

        // Assert
        Assert.True(nextCalled);
    }

    /// <summary>
    /// Verifies that InvokeAsync returns a validation problem when the model is invalid.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task InvokeAsync_WithInvalidObject_ReturnsValidationProblem()
    {
        // Arrange
        var contextMock = new Mock<EndpointFilterInvocationContext>();
        var invalidObject = new TestModel { Name = null! }; // Required
        contextMock.Setup(c => c.Arguments).Returns(new List<object?> { invalidObject });

        var nextCalled = false;
        EndpointFilterDelegate next = (context) =>
        {
            nextCalled = true;
            return ValueTask.FromResult<object?>(Results.Ok());
        };

        // Act
        var result = await _filter.InvokeAsync(contextMock.Object, next);

        // Assert
        Assert.False(nextCalled);

        // Minimal API Results.ValidationProblem returns a ProblemHttpResult or similar
        Assert.NotNull(result);
    }

    /// <summary>
    /// Verifies that InvokeAsync calls the next filter when the argument is null.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task InvokeAsync_WithNullArgument_CallsNext()
    {
        // Arrange
        var contextMock = new Mock<EndpointFilterInvocationContext>();
        contextMock.Setup(c => c.Arguments).Returns(new List<object?> { null });

        var nextCalled = false;
        EndpointFilterDelegate next = (context) =>
        {
            nextCalled = true;
            return ValueTask.FromResult<object?>(Results.Ok());
        };

        // Act
        await _filter.InvokeAsync(contextMock.Object, next);

        // Assert
        Assert.True(nextCalled);
    }

    /// <summary>
    /// A test model for validation.
    /// </summary>
    private class TestModel
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        [Required]
        public string Name { get; set; } = default!;
    }
}
