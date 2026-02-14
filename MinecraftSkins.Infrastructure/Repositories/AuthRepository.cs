using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using MinecraftSkins.Domain.IRepositories;

namespace MinecraftSkins.Infrastructure.Repositories;

public class AuthRepository : IAuthRepository
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly ILogger<AuthRepository> _logger;

    public AuthRepository(UserManager<IdentityUser> userManager, ILogger<AuthRepository> logger)
    {
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<IdentityUser?> Login(string username, string password, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _logger.LogDebug("Login attempt for user {Username}", username);

        var user = await _userManager.FindByNameAsync(username);
        if (user == null)
        {
            _logger.LogWarning("Login failed for user {Username} - user not found", username);
            return null;
        }

        var passwordValid = await _userManager.CheckPasswordAsync(user, password);
        if (!passwordValid)
        {
            _logger.LogWarning("Login failed for user {Username} - invalid password", username);
            return null;
        }

        _logger.LogDebug("Login successful for user {Username}", username);
        return user;
    }

    public async Task<IdentityUser?> Register(string username, string password, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _logger.LogDebug("Registration attempt for user {Username}", username);

        var existingUser = await _userManager.FindByNameAsync(username);
        if (existingUser != null)
        {
            _logger.LogWarning("Registration failed for user {Username} - user already exists", username);
            return null;
        }

        var user = new IdentityUser { UserName = username };
        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            _logger.LogWarning("Registration failed for user {Username} - {Errors}", username, errors);
            return null;
        }

        _logger.LogDebug("Registration successful for user {Username}", username);
        return user;
    }
}
