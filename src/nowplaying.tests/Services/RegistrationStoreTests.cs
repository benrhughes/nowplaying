// Copyright (c) Ben Hughes. SPDX-License-Identifier: AGPL-3.0-or-later
namespace NowPlaying.Tests.Services;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using NowPlaying.Services;
using Xunit;

/// <summary>
/// Unit tests for <see cref="RegistrationStore"/>, including disk persistence
/// behaviour and trimming of instance URLs.
/// </summary>
public class RegistrationStoreTests : IDisposable
{
    private readonly string _tempDir;
    private readonly Mock<ILogger<RegistrationStore>> _loggerMock;
    private readonly Mock<IWebHostEnvironment> _envMock;

    /// <summary>
    /// Initializes a new instance of the <see cref="RegistrationStoreTests"/> class.
    /// </summary>
    public RegistrationStoreTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "nowplaying-tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);

        _loggerMock = new Mock<ILogger<RegistrationStore>>();
        _envMock = new Mock<IWebHostEnvironment>();
        _envMock.Setup(e => e.ContentRootPath).Returns(_tempDir);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, recursive: true);
            }
        }
        catch
        {
            // best effort cleanup
        }
    }

    /// <summary>
    /// Verifies that a new store is empty and creates a file when adding a registration.
    /// </summary>
    [Fact]
    public void NewStore_IsEmptyAndCreatesFileWhenAdding()
    {
        // Arrange
        var store = new RegistrationStore(_loggerMock.Object, _envMock.Object);
        var path = Path.Combine(_tempDir, "registrations.json");

        // Act & Assert - initially no registration
        Assert.False(store.Has("https://example.com"));
        Assert.False(store.TryGet("https://example.com", out _));

        // Add a registration and verify retrieval
        store.Add("https://example.com", "cid", "secret", "https://callback");
        Assert.True(store.Has("https://example.com"));
        Assert.True(store.TryGet("https://example.com", out var info));
        Assert.NotNull(info);
        Assert.Equal("cid", info!.ClientId);

        // File should exist and contain the entry
        Assert.True(File.Exists(path));
        var json = File.ReadAllText(path);
        Assert.Contains("cid", json);

        // Creating a new instance against the same file should load the entry
        var store2 = new RegistrationStore(_loggerMock.Object, _envMock.Object);
        Assert.True(store2.Has("https://example.com"));
    }

    /// <summary>
    /// Verifies that instance URLs are trimmed and normalized.
    /// </summary>
    [Fact]
    public void InstanceUrls_AreTrimmedAndNormalized()
    {
        var store = new RegistrationStore(_loggerMock.Object, _envMock.Object);
        store.Add("https://example.com/", "cid", "secret", "https://callback");

        Assert.True(store.Has("https://example.com"));
        Assert.True(store.Has("https://example.com/"));
        Assert.True(store.TryGet("https://example.com/", out var info));
        Assert.NotNull(info);
        Assert.Equal("cid", info!.ClientId);
    }

    /// <summary>
    /// Verifies that TryGet returns false when the instance URL is null.
    /// </summary>
    [Fact]
    public void TryGet_ReturnsFalse_WhenInstanceIsNull()
    {
        var store = new RegistrationStore(_loggerMock.Object, _envMock.Object);
        Assert.False(store.TryGet(null!, out _));
    }

    /// <summary>
    /// Verifies that Has returns false when the instance URL is null.
    /// </summary>
    [Fact]
    public void Has_ReturnsFalse_WhenInstanceIsNull()
    {
        var store = new RegistrationStore(_loggerMock.Object, _envMock.Object);
        Assert.False(store.Has(null!));
    }

    /// <summary>
    /// Verifies that the store handles corrupted JSON by logging an error and continuing.
    /// </summary>
    [Fact]
    public void LoadStore_WithCorruptedJson_LogsErrorAndContinues()
    {
        // Arrange - write invalid JSON to file before constructing
        var path = Path.Combine(_tempDir, "registrations.json");
        File.WriteAllText(path, "{ not valid json");

        // Act - constructing should catch and log
        var store = new RegistrationStore(_loggerMock.Object, _envMock.Object);

        // Assert - store is usable and empty
        Assert.False(store.Has("https://foo"));
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
