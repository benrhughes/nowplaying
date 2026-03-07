// Copyright (c) Ben Hughes. SPDX-License-Identifier: AGPL-3.0-or-later
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using NowPlaying.Endpoints;
using NowPlaying.Models;
using NowPlaying.Services;
using Xunit;

namespace NowPlaying.Tests.Endpoints;

public class AuthenticationEndpointsTests
{
    private readonly Mock<IMastodonService> _mastodonServiceMock;
    private readonly Mock<HttpContext> _httpContextMock;
    private readonly Mock<ISession> _sessionMock;
    private readonly Mock<Microsoft.AspNetCore.Authentication.IAuthenticationService> _authServiceMock;
    private readonly Mock<IRegistrationStore> _registrationStoreMock;
    private readonly Mock<ILogger<AuthenticationEndpoints>> _loggerMock;
    private readonly AppConfig _config;

    public AuthenticationEndpointsTests()
    {
        _mastodonServiceMock = new Mock<IMastodonService>();
        _httpContextMock = new Mock<HttpContext>();
        _sessionMock = new Mock<ISession>();
        _authServiceMock = new Mock<Microsoft.AspNetCore.Authentication.IAuthenticationService>();
        _registrationStoreMock = new Mock<IRegistrationStore>();
        _loggerMock = new Mock<ILogger<AuthenticationEndpoints>>();

        _httpContextMock.SetupGet(h => h.Session).Returns(() => _sessionMock.Object);

        // Provide a request services provider that returns a mock IAuthenticationService
        var servicesMock = new Mock<IServiceProvider>();
        servicesMock.Setup(s => s.GetService(typeof(Microsoft.AspNetCore.Authentication.IAuthenticationService)))
            .Returns(_authServiceMock.Object);
        _httpContextMock.SetupGet(h => h.RequestServices).Returns(servicesMock.Object);
        _config = new AppConfig
        {
            Port = 3000,
            RedirectUri = "http://localhost:3000/auth/callback",
            SessionSecret = "test-secret"
        };
    }

    private AuthenticationEndpoints CreateEndpoints() => new(_mastodonServiceMock.Object, _loggerMock.Object, _registrationStoreMock.Object, _config);

    private void SetupSessionString(string key, string value)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(value);
        _sessionMock.Setup(s => s.TryGetValue(key, out It.Ref<byte[] ?>.IsAny))
            .Returns((string k, out byte[] ? v) =>
            {
                v = bytes;
                return true;
            });
    }

    [Fact]
    public void Login_WithSessionInstance_ReturnsResult()
    {
        // Arrange
        SetupSessionString("instance", "mastodon.social");
        _registrationStoreMock.Setup(r => r.TryGet("https://mastodon.social", out It.Ref<RegistrationInfo?>.IsAny))
            .Returns((string i, out RegistrationInfo? info) => { info = new RegistrationInfo("client-id", "client-secret", "http://localhost:3000/auth/callback"); return true; });

        // Act
        var result = CreateEndpoints().Login(_httpContextMock.Object, "https://mastodon.social");

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void Login_WithParameterInstance_ReturnsResult()
    {
        // Arrange
        SetupSessionString("redirectUri", "http://localhost:3000/auth/callback");
        _registrationStoreMock.Setup(r => r.TryGet("https://mastodon.social", out It.Ref<RegistrationInfo?>.IsAny))
            .Returns((string i, out RegistrationInfo? info) => { info = new RegistrationInfo("client-id", "client-secret", "http://localhost:3000/auth/callback"); return true; });

        // Act
        var result = CreateEndpoints().Login(_httpContextMock.Object, "https://mastodon.social");

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void Login_WithMissingInstance_ReturnsResult()
    {
        // Act
        var result = CreateEndpoints().Login(_httpContextMock.Object, null);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void Login_WithMissingClientId_ReturnsResult()
    {
        // Arrange
        SetupSessionString("instance", "mastodon.social");

        _registrationStoreMock.Setup(r => r.TryGet(It.IsAny<string>(), out It.Ref<RegistrationInfo?>.IsAny))
            .Returns(false);

        // Act
        var result = CreateEndpoints().Login(_httpContextMock.Object, "https://mastodon.social");

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Callback_WithValidCode_ReturnsResult()
    {
        // Arrange
        SetupSessionString("instance", "mastodon.social");
        SetupSessionString("oauth_state", "valid_state");
        _registrationStoreMock.Setup(r => r.TryGet("https://mastodon.social", out It.Ref<RegistrationInfo?>.IsAny))
            .Returns((string i, out RegistrationInfo? info) => { info = new RegistrationInfo("client-id", "client-secret", "http://localhost:3000/auth/callback"); return true; });

        _mastodonServiceMock.Setup(m => m.GetAccessTokenAsync("https://mastodon.social", "client-id", "client-secret", "auth-code", "http://localhost:3000/auth/callback"))
            .ReturnsAsync("access-token");

        _mastodonServiceMock.Setup(m => m.VerifyCredentialsAsync("https://mastodon.social", "access-token"))
            .ReturnsAsync("user-id");

        // Ensure SignInAsync is available via the IAuthenticationService
        _authServiceMock.Setup(a => a.SignInAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<System.Security.Claims.ClaimsPrincipal>(), It.IsAny<Microsoft.AspNetCore.Authentication.AuthenticationProperties>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await CreateEndpoints().Callback(_httpContextMock.Object, "auth-code", "valid_state");

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Callback_WithNullCode_ReturnsResult()
    {
        // Arrange & Act
        var result = await CreateEndpoints().Callback(_httpContextMock.Object, null, "state");

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Callback_WithMissingInstance_ReturnsResult()
    {
        // Arrange
        SetupSessionString("oauth_state", "state");

        // Act
        var result = await CreateEndpoints().Callback(_httpContextMock.Object, "auth-code", "state");

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Callback_WithMissingClientId_ReturnsResult()
    {
        // Arrange
        SetupSessionString("instance", "mastodon.social");
        SetupSessionString("oauth_state", "state");

        _registrationStoreMock.Setup(r => r.TryGet(It.IsAny<string>(), out It.Ref<RegistrationInfo?>.IsAny))
            .Returns(false);

        // Act
        var result = await CreateEndpoints().Callback(_httpContextMock.Object, "auth-code", "state");

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Callback_WithMissingClientSecret_ReturnsResult()
    {
        // Arrange
        SetupSessionString("instance", "mastodon.social");
        SetupSessionString("clientId", "client-id");
        SetupSessionString("oauth_state", "state");

        _registrationStoreMock.Setup(r => r.TryGet(It.IsAny<string>(), out It.Ref<RegistrationInfo?>.IsAny))
            .Returns((string i, out RegistrationInfo? info) => { info = new RegistrationInfo("client-id", null!, "http://localhost:3000/auth/callback"); return true; });

        // Act
        var result = await CreateEndpoints().Callback(_httpContextMock.Object, "auth-code", "state");

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Callback_WithServiceError_ReturnsResult()
    {
        // Arrange
        SetupSessionString("instance", "mastodon.social");
        SetupSessionString("oauth_state", "state");

        _registrationStoreMock.Setup(r => r.TryGet("https://mastodon.social", out It.Ref<RegistrationInfo?>.IsAny))
            .Returns((string i, out RegistrationInfo? info) => { info = new RegistrationInfo("client-id", "client-secret", "http://localhost:3000/auth/callback"); return true; });

        _mastodonServiceMock.Setup(m => m.GetAccessTokenAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new HttpRequestException("Service error"));

        // Act
        var result = await CreateEndpoints().Callback(_httpContextMock.Object, "auth-code", "state");

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Logout_ClearsSessionAndReturnsResult()
    {
        // Arrange
        _authServiceMock.Setup(a => a.SignOutAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<Microsoft.AspNetCore.Authentication.AuthenticationProperties>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await CreateEndpoints().Logout(_httpContextMock.Object);

        // Assert
        Assert.NotNull(result);
        _sessionMock.Verify(s => s.Clear(), Times.Once);
    }
}
