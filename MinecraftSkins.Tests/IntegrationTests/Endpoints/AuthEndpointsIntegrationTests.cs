using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using MinecraftSkins.Application.Dtos;
using MinecraftSkins.Tests.IntegrationTests;

namespace MinecraftSkins.Tests.IntegrationTests.Endpoints;

public class AuthEndpointsIntegrationTests : IntegrationTestBase
{
    public AuthEndpointsIntegrationTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    public override async ValueTask InitializeAsync()
    {
        await CleanupAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Register_WithValidData_ReturnsOk()
    {
        var ct = TestContext.Current.CancellationToken;

        // Arrange
        var registerDto = new RegistrationRequestDto
        {
            UserName = "newuser",
            Password = "Test123!"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/register", registerDto, ct);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var user = await response.Content.ReadFromJsonAsync<UserDto>(ct);
        user.Should().NotBeNull();
        user!.Name.Should().Be("newuser");
    }

    [Fact]
    public async Task Register_WithExistingUserName_ReturnsBadRequest()
    {
        var ct = TestContext.Current.CancellationToken;

        // Arrange
        await CreateTestUserAsync("existinguser", "Test123!", ct);
        var registerDto = new RegistrationRequestDto
        {
            UserName = "existinguser",
            Password = "Test123!"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/register", registerDto, ct);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsToken()
    {
        var ct = TestContext.Current.CancellationToken;

        // Arrange
        var userName = "testuser";
        var password = "Test123!";
        await CreateTestUserAsync(userName, password, ct);
        
        var loginDto = new LoginRequestDto
        {
            UserName = userName,
            Password = password
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/login", loginDto, ct);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponseDto>(ct);
        loginResponse.Should().NotBeNull();
        loginResponse!.Token.Should().NotBeNullOrEmpty();
        loginResponse.UserName.Should().Be(userName);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsBadRequest()
    {
        var ct = TestContext.Current.CancellationToken;

        // Arrange
        await CreateTestUserAsync("testuser", "Test123!", ct);
        
        var loginDto = new LoginRequestDto
        {
            UserName = "testuser",
            Password = "WrongPassword"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/login", loginDto, ct);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_WithNonExistentUser_ReturnsBadRequest()
    {
        var ct = TestContext.Current.CancellationToken;

        // Arrange
        var loginDto = new LoginRequestDto
        {
            UserName = "nonexistent",
            Password = "Test123!"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/login", loginDto, ct);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
