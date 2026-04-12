using System.Security.Claims;
using ResX.Identity.Domain.AggregateRoots;

namespace ResX.Identity.Application.Services;

public interface ITokenService
{
    string GenerateAccessToken(User user);

    string GenerateRefreshToken();

    ClaimsPrincipal GetPrincipalFromToken(string token);

    int GetAccessTokenExpiryMinutes();
}