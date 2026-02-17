using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MinecraftSkins.Application.Dtos;
using MinecraftSkins.Application.Interfaces;
using MinecraftSkins.Domain.IRepositories;

namespace MinecraftSkins.Application.Services;

public class AuthService : IAuthService
{
    private readonly IAuthRepository _authRepository;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IConfiguration _configuration;
    private readonly IJwtService _jwtService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IAuthRepository authRepository,
        UserManager<IdentityUser> userManager,
        IConfiguration configuration,
        IJwtService jwtService,
        ILogger<AuthService> logger)
    {
        _authRepository = authRepository;
        _userManager = userManager;
        _configuration = configuration;
        _jwtService = jwtService;
        _logger = logger;
    }

    public async Task<LoginResponseDto?> LoginAsync(LoginRequestDto loginRequestDto, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Login attempt for user {UserName}", loginRequestDto.UserName);
        
        cancellationToken.ThrowIfCancellationRequested();
        
        var user = await _authRepository.Login(loginRequestDto.UserName, loginRequestDto.Password, cancellationToken);
        if (user == null)
        {
            _logger.LogWarning("Login failed for user {UserName} - invalid credentials", loginRequestDto.UserName);
            return null;
        }

        // Get user roles from database
        var userRoles = await _userManager.GetRolesAsync(user);
        
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.UserName ?? ""),
            new Claim(ClaimTypes.NameIdentifier, user.Id)
        };
        
        // Add role claims from database
        foreach (var role in userRoles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }
        
        // If user has no roles, assign default "User" role (temporary fallback)
        if (userRoles.Count == 0)
        {
            claims.Add(new Claim(ClaimTypes.Role, "User"));
        }

        var token = _jwtService.GenerateToken(claims);
        _logger.LogInformation("Login successful for user {UserName}", loginRequestDto.UserName);
        
        return new LoginResponseDto
        {
            Token = token,
            UserName = user.UserName ?? ""
        };
    }

    public async Task<UserDto?> RegisterAsync(RegistrationRequestDto requestDto, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Registration attempt for user {UserName}", requestDto.UserName);
        
        cancellationToken.ThrowIfCancellationRequested();
        
        var user = await _authRepository.Register(requestDto.UserName, requestDto.Password, cancellationToken);
        if (user == null)
        {
            _logger.LogWarning("Registration failed for user {UserName} - registration process failed", requestDto.UserName);
            return null;
        }

        _logger.LogInformation("Registration successful for user {UserName}", requestDto.UserName);
        return new UserDto { Id = user.Id, Name = user.UserName ?? "" };
    }
}
