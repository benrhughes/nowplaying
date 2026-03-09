// Copyright (c) Ben Hughes. SPDX-License-Identifier: AGPL-3.0-or-later
namespace NowPlaying.Tests.Filters;

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;
using NowPlaying.Filters;
using Xunit;

public class ValidationFilterTests
{
    private readonly ValidationFilter _filter;

    public ValidationFilterTests()
    {
        _filter = new ValidationFilter();
    }

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

    private class TestModel
    {
        [Required]
        public string Name { get; set; } = default!;
    }
}
