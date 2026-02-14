using System.Threading;
using System.Threading.Tasks;
using MinecraftSkins.Application.Dtos;

namespace MinecraftSkins.Application.Interfaces;

public interface IAuthService
{
    Task<LoginResponseDto?> LoginAsync(LoginRequestDto loginRequestDto, CancellationToken cancellationToken = default);
    Task<UserDto?> RegisterAsync(RegistrationRequestDto requestDto, CancellationToken cancellationToken = default);
}

