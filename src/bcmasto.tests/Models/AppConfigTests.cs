using BcMasto.Models;
using Xunit;

namespace BcMasto.Tests.Models;

public class AppConfigTests
{
    [Fact]
    public void AppConfig_HasDefaultValues()
    {
        // Arrange & Act
        var config = new AppConfig();

        // Assert
        Assert.Equal(5000, config.Port);
        Assert.Equal("http://localhost:5000/auth/callback", config.RedirectUri);
        Assert.Equal("dev-secret-change-in-production", config.SessionSecret);
    }

    [Fact]
    public void AppConfig_CanSetProperties()
    {
        // Arrange & Act
        var config = new AppConfig
        {
            Port = 8080,
            RedirectUri = "https://example.com/callback",
            SessionSecret = "new-secret"
        };

        // Assert
        Assert.Equal(8080, config.Port);
        Assert.Equal("https://example.com/callback", config.RedirectUri);
        Assert.Equal("new-secret", config.SessionSecret);
    }

    [Fact]
    public void AppConfig_AppNameConstant_IsCorrect()
    {
        // Act & Assert
        Assert.Equal("BcMasto", AppConfig.AppName);
    }
}
