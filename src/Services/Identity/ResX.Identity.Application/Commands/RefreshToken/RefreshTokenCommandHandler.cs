using MediatR;
using Microsoft.Extensions.Logging;
using ResX.Common.Caching;
using ResX.Common.Persistence;
using ResX.Identity.Application.DTOs;
using ResX.Identity.Application.Repositories;
using ResX.Identity.Application.Services;

namespace ResX.Identity.Application.Commands.RefreshToken;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, TokensDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITokenService _tokenService;
    private readonly ICacheService _cache;
    private readonly ILogger<RefreshTokenCommandHandler> _logger;

    public RefreshTokenCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        ITokenService tokenService,
        ICacheService cache,
        ILogger<RefreshTokenCommandHandler> logger)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _tokenService = tokenService;
        _cache = cache;
        _logger = logger;
    }

    public async Task<TokensDto> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        // Fast blacklist check before hitting the DB
        if (await _cache.ExistsAsync($"identity:token:blacklist:{request.RefreshToken}", cancellationToken))
            throw new UnauthorizedAccessException();

        // Look up the user by the opaque refresh token value — no JWT parsing required.
        // The refresh token is a random-bytes base64 string stored in the DB, not a JWT.
        var user = await _userRepository.GetByRefreshTokenAsync(request.RefreshToken, cancellationToken)
            ?? throw new UnauthorizedAccessException();

        var activeToken = user.GetActiveRefreshToken(request.RefreshToken)
            ?? throw new UnauthorizedAccessException();

        activeToken.Revoke();

        var newAccessToken = _tokenService.GenerateAccessToken(user);
        var newRefreshTokenValue = _tokenService.GenerateRefreshToken();

        var newRefreshToken = user.AddRefreshToken(newRefreshTokenValue, DateTime.UtcNow.AddDays(30));

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Blacklist the used token so it can't be replayed
        await _cache.SetAsync(
            $"identity:token:blacklist:{request.RefreshToken}",
            true,
            TimeSpan.FromDays(30),
            cancellationToken);

        _logger.LogInformation("Token refreshed for user {UserId}.", user.Id);

        return new TokensDto(newAccessToken, newRefreshToken.Token);
    }
}
