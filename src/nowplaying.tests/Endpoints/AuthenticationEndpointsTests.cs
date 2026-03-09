// Copyright (c) Ben Hughes. SPDX-License-Identifier: AGPL-3.0-or-later
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;
using Moq;
using NowPlaying.Endpoints;
using NowPlaying.Models;
using NowPlaying.Services;
using Xunit;

namespace NowPlaying.Tests.Endpoints;

/// <summary>
/// Unit tests for the <see cref="AuthenticationEndpoints"/> class.
/// </summary>
public class AuthenticationEndpointsTests
{
    private readonly Mock<IMastodonService> _mastodonServiceMock;
    private readonly Mock<HttpContext> _httpContextMock;
    private readonly Mock<ISession> _sessionMock;
    private readonly Mock<Microsoft.AspNetCore.Authentication.IAuthenticationService> _authServiceMock;
    private readonly Mock<IRegistrationStore> _registrationStoreMock;
    private readonly Mock<ILogger<AuthenticationEndpoints>> _loggerMock;
    private readonly AppConfig _config;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthenticationEndpointsTests"/> class.
    /// </summary>
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

    /// <summary>
    /// Creates a new instance of <see cref="AuthenticationEndpoints"/>.
    /// </summary>
    /// <returns>A new <see cref="AuthenticationEndpoints"/> instance.</returns>
    private AuthenticationEndpoints CreateEndpoints() => new(_mastodonServiceMock.Object, _loggerMock.Object, _registrationStoreMock.Object, _config);

    /// <summary>
    /// Sets up a string value in the mock session.
    /// </summary>
    /// <param name="key">The session key.</param>
    /// <param name="value">The session value.</param>
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

    /// <summary>
    /// Verifies that Login returns a redirect result when an instance URL is in the session.
    /// </summary>
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
        var redirectResult = Assert.IsType<RedirectHttpResult>(result);
        Assert.Contains("mastodon.social/oauth/authorize", redirectResult.Url);
        Assert.Contains("client_id=client-id", redirectResult.Url);
    }

    /// <summary>
    /// Verifies that Login returns a redirect result when an instance URL is provided as a parameter.
    /// </summary>
    [Fact]
    public void Login_WithParameterInstance_ReturnsResult()
    {
        // Arrange
        SetupSessionString("redirectUri", "http://localhost:3000/auth/callback");
        _registrationStoreMock.Setup(r => r.TryGet("https://mastodon.social", out It.Ref<RegistrationInfo?>.IsAny))
            .Returns((string i, out RegistrationInfo? info) => { info = new RegistrationInfo("client-id", "client-secret", "http://localhost:3000/auth/callback"); return true; });

        // Act
        var result2 = CreateEndpoints().Login(_httpContextMock.Object, "https://mastodon.social");

        // Assert
        var redirectResult2 = Assert.IsType<RedirectHttpResult>(result2);
        Assert.Contains("mastodon.social/oauth/authorize", redirectResult2.Url);
    }

    /// <summary>
    /// Verifies that Login returns a result even when the instance URL is missing.
    /// </summary>
    [Fact]
    public void Login_WithMissingInstance_ReturnsResult()
    {
        // Act
        var result = CreateEndpoints().Login(_httpContextMock.Object, null);

        // Assert
        var badRequest = Assert.IsType<BadRequest<ErrorResponse>>(result);
        Assert.Equal("Instance not configured. Please select an instance first.", badRequest.Value!.Error);
    }

    /// <summary>
    /// Verifies that Login returns a result when the client ID is missing from the registration store.
    /// </summary>
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
        var badRequest = Assert.IsType<BadRequest<ErrorResponse>>(result);
        Assert.Equal("Instance not registered. Please register your instance first.", badRequest.Value!.Error);
    }

    /// <summary>
    /// Verifies that Callback returns a success result with a valid authorization code.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task Callback_WithValidCode_ReturnsResult()
    {
        // Arrange
        SetupSessionString("instance", "https://mastodon.social");
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
        var redirectResult = Assert.IsType<RedirectHttpResult>(result);
        Assert.Equal("/", redirectResult.Url);
    }

    /// <summary>
    /// Verifies that Callback returns a bad request result when the authorization code is null.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task Callback_WithNullCode_ReturnsBadRequest()
    {
        // Arrange & Act
        var result = await CreateEndpoints().Callback(_httpContextMock.Object, null, "state");

        // Assert
        var badRequest = Assert.IsType<BadRequest<ErrorResponse>>(result);
        Assert.Equal("No authorization code provided", badRequest.Value!.Error);
    }

    /// <summary>
    /// Verifies that Callback returns a bad request result when the state is null.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task Callback_WithNullState_ReturnsBadRequest()
    {
        // Arrange
        SetupSessionString("oauth_state", "some_state");

        // Act
        var result = await CreateEndpoints().Callback(_httpContextMock.Object, "code", null);

        // Assert
        var badRequest = Assert.IsType<BadRequest<ErrorResponse>>(result);
        Assert.Equal("Invalid state parameter (CSRF protection)", badRequest.Value!.Error);
    }

    /// <summary>
    /// Verifies that Callback returns a result when the instance URL is missing from the session.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task Callback_WithMissingInstance_ReturnsBadRequest()
    {
        // Arrange
        SetupSessionString("oauth_state", "state");

        // Act
        var result = await CreateEndpoints().Callback(_httpContextMock.Object, "auth-code", "state");

        // Assert
        var badRequest = Assert.IsType<BadRequest<ErrorResponse>>(result);
        Assert.Equal("Session invalid. Please start the login process again.", badRequest.Value!.Error);
    }

    /// <summary>
    /// Verifies that Callback returns a result when the client ID is missing from the session.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task Callback_WithMissingClientId_ReturnsBadRequest()
    {
        // Arrange
        SetupSessionString("instance", "mastodon.social");
        SetupSessionString("oauth_state", "state");

        _registrationStoreMock.Setup(r => r.TryGet(It.IsAny<string>(), out It.Ref<RegistrationInfo?>.IsAny))
            .Returns(false);

        // Act
        var result = await CreateEndpoints().Callback(_httpContextMock.Object, "auth-code", "state");

        // Assert
        Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.BadRequest<ErrorResponse>>(result);
    }

    /// <summary>
    /// Verifies that Callback returns a result when the client secret is missing from the session.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task Callback_WithMissingClientSecret_ReturnsRedirect()
    {
        // Arrange
        SetupSessionString("instance", "mastodon.social");
        SetupSessionString("clientId", "client-id");
        SetupSessionString("oauth_state", "state");

        _registrationStoreMock.Setup(r => r.TryGet(It.IsAny<string>(), out It.Ref<RegistrationInfo?>.IsAny))
            .Returns((string i, out RegistrationInfo? info) => { info = new RegistrationInfo("client-id", null!, "http://localhost:3000/auth/callback"); return true; });

        _mastodonServiceMock.Setup(m => m.GetAccessTokenAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("token");
        _mastodonServiceMock.Setup(m => m.VerifyCredentialsAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("user-id");

        _authServiceMock.Setup(a => a.SignInAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<System.Security.Claims.ClaimsPrincipal>(), It.IsAny<Microsoft.AspNetCore.Authentication.AuthenticationProperties>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await CreateEndpoints().Callback(_httpContextMock.Object, "auth-code", "state");

        // Assert
        Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.RedirectHttpResult>(result);
    }

    /// <summary>
    /// Verifies that Callback handles service errors gracefully.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task Callback_WithServiceError_ReturnsBadRequest()
    {
        // Arrange
        SetupSessionString("instance", "https://mastodon.social");
        SetupSessionString("oauth_state", "state");
        _registrationStoreMock.Setup(r => r.TryGet("https://mastodon.social", out It.Ref<RegistrationInfo?>.IsAny))
            .Returns((string i, out RegistrationInfo? info) => { info = new RegistrationInfo("client-id", "client-secret", "http://localhost:3000/auth/callback"); return true; });

        _mastodonServiceMock.Setup(m => m.GetAccessTokenAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new HttpRequestException("OAuth failed"));

        // Act
        var result = await CreateEndpoints().Callback(_httpContextMock.Object, "auth-code", "state");

        // Assert
        Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.BadRequest<ErrorResponse>>(result);
    }

    /// <summary>
    /// Verifies that Callback returns a bad request result when the state is invalid.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task Callback_WithInvalidState_ReturnsBadRequest()
    {
        // Arrange
        SetupSessionString("oauth_state", "valid_state");

        // Act
        var result = await CreateEndpoints().Callback(_httpContextMock.Object, "auth-code", "wrong_state");

        // Assert
        var badRequest = Assert.IsType<BadRequest<ErrorResponse>>(result);
        Assert.Equal("Invalid state parameter (CSRF protection)", badRequest.Value!.Error);
    }

    /// <summary>
    /// Verifies that Callback returns an internal server error result on general exceptions.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task Callback_WithGeneralException_Returns500()
    {
        // Arrange
        SetupSessionString("instance", "https://mastodon.social");
        SetupSessionString("oauth_state", "state");
        _registrationStoreMock.Setup(r => r.TryGet("https://mastodon.social", out It.Ref<RegistrationInfo?>.IsAny))
            .Returns((string i, out RegistrationInfo? info) => { info = new RegistrationInfo("client-id", "client-secret", "http://localhost:3000/auth/callback"); return true; });

        _mastodonServiceMock.Setup(m => m.GetAccessTokenAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("Unknown error"));

        // Act
        var result = await CreateEndpoints().Callback(_httpContextMock.Object, "auth-code", "state");

        // Assert
        Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.StatusCodeHttpResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, ((Microsoft.AspNetCore.Http.HttpResults.StatusCodeHttpResult)result).StatusCode);
    }

    /// <summary>
    /// Verifies that Status returns the correct status when the user is authenticated.
    /// </summary>
    [Fact]
    public void Status_WhenAuthenticated_ReturnsStatus()
    {
        // Arrange
        var claims = new[] { new Claim("instance", "https://mastodon.social") };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var user = new ClaimsPrincipal(identity);
        _httpContextMock.SetupGet(h => h.User).Returns(user);
        _registrationStoreMock.Setup(r => r.Has(It.IsAny<string>())).Returns(true);

        // Act
        var result = CreateEndpoints().Status(_httpContextMock.Object);

        // Assert
        var okResult = Assert.IsType<Ok<StatusResponse>>(result);
        Assert.True(okResult.Value!.Authenticated);
        Assert.Equal("https://mastodon.social", okResult.Value.Instance);
    }

    /// <summary>
    /// Verifies that Status returns the correct status when the user is not authenticated.
    /// </summary>
    [Fact]
    public void Status_WhenNotAuthenticated_ReturnsStatus()
    {
        // Arrange
        var user = new ClaimsPrincipal(new ClaimsIdentity());
        _httpContextMock.SetupGet(h => h.User).Returns(user);
        SetupSessionString("instance", "mastodon.social");
        _registrationStoreMock.Setup(r => r.Has("https://mastodon.social")).Returns(true);

        // Act
        var result = CreateEndpoints().Status(_httpContextMock.Object);

        // Assert
        var okResult = Assert.IsType<Ok<StatusResponse>>(result);
        Assert.False(okResult.Value!.Authenticated);
        Assert.Equal("mastodon.social", okResult.Value.Instance);
    }

    /// <summary>
    /// Verifies that Logout clears the session and signs the user out.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task Logout_ClearsSessionAndSignsOut()
    {
        // Arrange
        _authServiceMock.Setup(a => a.SignOutAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<Microsoft.AspNetCore.Authentication.AuthenticationProperties>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await CreateEndpoints().Logout(_httpContextMock.Object);

        // Assert
        var redirectResult = Assert.IsType<RedirectHttpResult>(result);
        Assert.Equal("/", redirectResult.Url);
        _sessionMock.Verify(s => s.Clear(), Times.Once);
    }

    /// <summary>
    /// Verifies that Register returns a success result with a valid instance URL.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task Register_WithValidInstance_ReturnsResult()
    {
        // Arrange
        var request = new RegisterRequest { Instance = "mastodon.social" };
        _mastodonServiceMock.Setup(m => m.RegisterAppAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(("client-id", "client-secret"));

        // Act
        var result = await CreateEndpoints().Register(_httpContextMock.Object, request);

        // Assert
        var okResult = Assert.IsType<Ok<RegistrationResponse>>(result);
        Assert.True(okResult.Value!.Success);
        _registrationStoreMock.Verify(r => r.Add("https://mastodon.social", "client-id", "client-secret", _config.RedirectUri), Times.Once);
    }

    /// <summary>
    /// Verifies that Register returns a bad request result with an invalid instance URL.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task Register_WithInvalidInstance_ReturnsBadRequest()
    {
        // Arrange
        var request = new RegisterRequest { Instance = string.Empty }; // Invalid instance

        // Act
        var result = await CreateEndpoints().Register(_httpContextMock.Object, request);

        // Assert
        var badRequest = Assert.IsType<BadRequest<ErrorResponse>>(result);
        Assert.NotNull(badRequest.Value!.Error);
    }

    /// <summary>
    /// Verifies that Register returns a bad request result on service errors.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task Register_WithServiceError_ReturnsBadRequest()
    {
        // Arrange
        var request = new RegisterRequest { Instance = "mastodon.social" };
        _mastodonServiceMock.Setup(m => m.RegisterAppAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new HttpRequestException("Registration failed"));

        // Act
        var result = await CreateEndpoints().Register(_httpContextMock.Object, request);

        // Assert
        var badRequest = Assert.IsType<BadRequest<ErrorResponse>>(result);
        Assert.Contains("Failed to register", badRequest.Value!.Error);
    }

    /// <summary>
    /// Verifies that Register returns an internal server error result on general exceptions.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task Register_WithGeneralException_Returns500()
    {
        // Arrange
        var request = new RegisterRequest { Instance = "mastodon.social" };
        _mastodonServiceMock.Setup(m => m.RegisterAppAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("Unknown error"));

        // Act
        var result = await CreateEndpoints().Register(_httpContextMock.Object, request);

        // Assert
        var statusCodeResult = Assert.IsType<StatusCodeHttpResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
    }

    /// <summary>
    /// Verifies that Callback returns a bad request result with an empty authorization code.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task Callback_WithEmptyCode_ReturnsBadRequest()
    {
        // Arrange & Act
        var result = await CreateEndpoints().Callback(_httpContextMock.Object, string.Empty, "state");

        // Assert
        var badRequest = Assert.IsType<BadRequest<ErrorResponse>>(result);
        Assert.Equal("No authorization code provided", badRequest.Value!.Error);
    }

    /// <summary>
    /// Verifies that Callback returns a bad request result when registration info is null.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task Callback_WithRegistrationInfoNull_ReturnsBadRequest()
    {
        // Arrange
        SetupSessionString("instance", "mastodon.social");
        SetupSessionString("oauth_state", "state");

        _registrationStoreMock.Setup(r => r.TryGet(It.IsAny<string>(), out It.Ref<RegistrationInfo?>.IsAny))
            .Returns((string i, out RegistrationInfo? info) => { info = null; return true; });

        // Act
        var result = await CreateEndpoints().Callback(_httpContextMock.Object, "auth-code", "state");

        // Assert
        var badRequest = Assert.IsType<BadRequest<ErrorResponse>>(result);
        Assert.Contains("Registration info missing", badRequest.Value!.Error);
    }

    /// <summary>
    /// Verifies that Callback handles a null access token gracefully.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task Callback_WithNullAccessToken_HandlesGracefully()
    {
        // Arrange
        SetupSessionString("instance", "https://mastodon.social");
        SetupSessionString("oauth_state", "valid_state");
        _registrationStoreMock.Setup(r => r.TryGet("https://mastodon.social", out It.Ref<RegistrationInfo?>.IsAny))
            .Returns((string i, out RegistrationInfo? info) => { info = new RegistrationInfo("client-id", "client-secret", "http://localhost:3000/auth/callback"); return true; });

        _mastodonServiceMock.Setup(m => m.GetAccessTokenAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((string)null!);

        _mastodonServiceMock.Setup(m => m.VerifyCredentialsAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("user-id");

        _authServiceMock.Setup(a => a.SignInAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<System.Security.Claims.ClaimsPrincipal>(), It.IsAny<Microsoft.AspNetCore.Authentication.AuthenticationProperties>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await CreateEndpoints().Callback(_httpContextMock.Object, "auth-code", "valid_state");

        // Assert
        var redirectResult = Assert.IsType<RedirectHttpResult>(result);
        Assert.Equal("/", redirectResult.Url);
    }

    /// <summary>
    /// Verifies that Callback returns a bad request result when credential verification fails.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task Callback_WithVerifyCredentialsError_ReturnsBadRequest()
    {
        // Arrange
        SetupSessionString("instance", "https://mastodon.social");
        SetupSessionString("oauth_state", "state");
        _registrationStoreMock.Setup(r => r.TryGet("https://mastodon.social", out It.Ref<RegistrationInfo?>.IsAny))
            .Returns((string i, out RegistrationInfo? info) => { info = new RegistrationInfo("client-id", "client-secret", "http://localhost:3000/auth/callback"); return true; });

        _mastodonServiceMock.Setup(m => m.GetAccessTokenAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("token");

        _mastodonServiceMock.Setup(m => m.VerifyCredentialsAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new HttpRequestException("Verification failed"));

        // Act
        var result = await CreateEndpoints().Callback(_httpContextMock.Object, "auth-code", "state");

        // Assert
        var badRequest = Assert.IsType<BadRequest<ErrorResponse>>(result);
        Assert.Contains("Verification failed", badRequest.Value!.Error);
    }
}
