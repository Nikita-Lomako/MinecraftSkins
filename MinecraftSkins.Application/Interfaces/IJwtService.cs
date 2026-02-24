using System.Collections.Generic;
using System.Security.Claims;

namespace MinecraftSkins.Application.Interfaces;

public interface IJwtService
{
    string GenerateToken(IEnumerable<Claim> claims);
}

