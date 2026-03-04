namespace NowPlaying.Tests.Models;

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using NowPlaying.Models;
using Microsoft.Extensions.Configuration;
using Xunit;

/// <summary>
/// Tests for <see cref="AppConfig"/> binding and validation.
/// </summary>
public class AppConfigTests
{
    /// <summary>
    /// Verifies that AppConfig correctly binds from environment variable style keys.
    /// </summary>
    [Fact]
    public void AppConfig_ShouldBindFromEnvironmentVariables()
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string?>
        {
            { "PORT", "3000" },
            { "REDIRECT_URI", "https://murph.local/callback" },
            { "SESSION_SECRET", "this-is-a-long-enough-secret-string" },
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        // Act
        var config = configuration.Get<AppConfig>();

        // Assert
        Assert.NotNull(config);
        Assert.Equal(3000, config.Port);
        Assert.Equal("https://murph.local/callback", config.RedirectUri);
        Assert.Equal("this-is-a-long-enough-secret-string", config.SessionSecret);
    }

    /// <summary>
    /// Verifies that AppConfig validation catches invalid settings.
    /// </summary>
    [Theory]
    [InlineData("not-a-url", "secret-too-short", 0)] // Invalid URL, short secret, out of range port
    public void AppConfig_ShouldFailValidation_WhenValuesAreInvalid(string url, string secret, int port)
    {
        // Arrange
        var config = new AppConfig
        {
            RedirectUri = url,
            SessionSecret = secret,
            Port = port,
        };

        var context = new ValidationContext(config);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(config, context, results, true);

        // Assert
        Assert.False(isValid);
        Assert.NotEmpty(results);
    }

    /// <summary>
    /// Verifies that AppConfig uses default values when no config is provided.
    /// </summary>
    [Fact]
    public void AppConfig_ShouldHaveDefaultValues()
    {
        // Act
        var config = new AppConfig();

        // Assert
        Assert.Equal(4444, config.Port);
        Assert.Equal("http://localhost:4444/auth/callback", config.RedirectUri);
    }
}
