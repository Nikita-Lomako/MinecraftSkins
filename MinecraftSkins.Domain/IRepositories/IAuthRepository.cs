using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace MinecraftSkins.Domain.IRepositories;

public interface IAuthRepository
{
    Task<IdentityUser?> Login(string username, string password, CancellationToken cancellationToken = default);
    Task<IdentityUser?> Register(string username, string password, CancellationToken cancellationToken = default);
}

