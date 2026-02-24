using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using MinecraftSkins.Application.Dtos;
using MinecraftSkins.Application.Services;
using MinecraftSkins.Domain.IRepositories;
using MinecraftSkins.Tests.Mocks.Helpers;

namespace MinecraftSkins.Tests.UnitTests.Application.Services;

public class AuthServiceTests
{
    private readonly IAuthRepository _authRepository;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IConfiguration _configuration;
    private readonly IJwtService _jwtService;
    private readonly ILogger<AuthService> _logger;
    private readonly AuthService _service;
    private readonly Fixture _fixture;

    public AuthServiceTests()
    {
        _authRepository = Substitute.For<IAuthRepository>();
        _userManager = Substitute.For<UserManager<IdentityUser>>(
            Substitute.For<IUserStore<IdentityUser>>(),
            null!, null!, null!, null!, null!, null!, null!, null!);
        _configuration = CreateTestConfiguration();
        _jwtService = Substitute.For<IJwtService>();
        _logger = Substitute.For<ILogger<AuthService>>();
        _service = new AuthService(
            _authRepository,
            _userManager,
            _configuration,
            _jwtService,
            _logger);
        _fixture = new Fixture();
    }

    private static IConfiguration CreateTestConfiguration()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ApiSettings:Secret", "test-secret-key-for-jwt-token-generation-in-tests-min-32-chars" }
            })
            .Build();
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsLoginResponse()
    {
        var ct = TestContext.Current.CancellationToken;

        // Arrange
        var loginDto = new LoginRequestDto
        {
            UserName = "testuser",
            Password = "Test123!"
        };
        var user = new IdentityUser { Id = "user-123", UserName = "testuser" };
        var token = "test-jwt-token";

        _authRepository.Login(loginDto.UserName, loginDto.Password, Arg.Any<CancellationToken>())
            .Returns(user);
        _userManager.GetRolesAsync(user)
            .Returns(Task.FromResult<IList<string>>(new List<string> { "User" }));
        _jwtService.GenerateToken(Arg.Any<List<System.Security.Claims.Claim>>())
            .Returns(token);

        // Act
        var result = await _service.LoginAsync(loginDto, ct);

        // Assert
        result.Should().NotBeNull();
        result!.Token.Should().Be(token);
        result.UserName.Should().Be("testuser");
    }

    [Fact]
    public async Task LoginAsync_WithInvalidCredentials_ReturnsNull()
    {
        var ct = TestContext.Current.CancellationToken;

        // Arrange
        var loginDto = new LoginRequestDto
        {
            UserName = "testuser",
            Password = "WrongPassword"
        };

        _authRepository.Login(loginDto.UserName, loginDto.Password, Arg.Any<CancellationToken>())
            .Returns((IdentityUser?)null);

        // Act
        var result = await _service.LoginAsync(loginDto, ct);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task RegisterAsync_WithValidData_ReturnsUserDto()
    {
        var ct = TestContext.Current.CancellationToken;

        // Arrange
        var registerDto = new RegistrationRequestDto
        {
            UserName = "newuser",
            Password = "Test123!"
        };
        var user = new IdentityUser { Id = "user-456", UserName = "newuser" };

        _authRepository.Register(registerDto.UserName, registerDto.Password, Arg.Any<CancellationToken>())
            .Returns(user);

        // Act
        var result = await _service.RegisterAsync(registerDto, ct);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be("user-456");
        result.Name.Should().Be("newuser");
    }

    [Fact]
    public async Task RegisterAsync_WithInvalidData_ReturnsNull()
    {
        var ct = TestContext.Current.CancellationToken;

        // Arrange
        var registerDto = new RegistrationRequestDto
        {
            UserName = "existinguser",
            Password = "Test123!"
        };

        _authRepository.Register(registerDto.UserName, registerDto.Password, Arg.Any<CancellationToken>())
            .Returns((IdentityUser?)null);

        // Act
        var result = await _service.RegisterAsync(registerDto, ct);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task LoginAsync_WithUserRoles_IncludesRolesInToken()
    {
        var ct = TestContext.Current.CancellationToken;

        // Arrange
        var loginDto = new LoginRequestDto
        {
            UserName = "admin",
            Password = "Admin123!"
        };
        var user = new IdentityUser { Id = "admin-123", UserName = "admin" };

        _authRepository.Login(loginDto.UserName, loginDto.Password, Arg.Any<CancellationToken>())
            .Returns(user);
        _userManager.GetRolesAsync(user)
            .Returns(Task.FromResult<IList<string>>(new List<string> { "Admin", "User" }));
        _jwtService.GenerateToken(Arg.Any<List<System.Security.Claims.Claim>>())
            .Returns("token");

        // Act
        var result = await _service.LoginAsync(loginDto, ct);

        // Assert
        result.Should().NotBeNull();
        _jwtService.Received(1).GenerateToken(
            Arg.Is<List<System.Security.Claims.Claim>>(claims => 
                claims.Any(c => c.Type == System.Security.Claims.ClaimTypes.Role && c.Value == "Admin") &&
                claims.Any(c => c.Type == System.Security.Claims.ClaimTypes.Role && c.Value == "User")));
    }
}
