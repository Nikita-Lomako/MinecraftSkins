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

        // Assign default "User" role to newly registered user
        var roleResult = await _userManager.AddToRoleAsync(user, "User");
        if (!roleResult.Succeeded)
        {
            var errors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
            _logger.LogError("Failed to assign User role to {Username} - {Errors}. Rolling back user creation.", username, errors);
            
            // Rollback: delete the user if role assignment failed
            var deleteResult = await _userManager.DeleteAsync(user);
            if (!deleteResult.Succeeded)
            {
                _logger.LogError("Failed to rollback user creation for {Username}. User may be in inconsistent state.", username);
            }
            
            return null;
        }

        _logger.LogDebug("Registration successful for user {Username} with User role", username);
        return user;
    }
}
