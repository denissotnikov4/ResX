using MediatR;
using Microsoft.Extensions.Logging;
using ResX.Common.Caching;
using ResX.Common.Exceptions;
using ResX.Common.Persistence;
using ResX.Identity.Application.Repositories;

namespace ResX.Identity.Application.Commands.Logout;

public class LogoutCommandHandler : IRequestHandler<LogoutCommand, Unit>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cache;
    private readonly ILogger<LogoutCommandHandler> _logger;

    public LogoutCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        ICacheService cache,
        ILogger<LogoutCommandHandler> logger)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _cache = cache;
        _logger = logger;
    }

    public async Task<Unit> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByRefreshTokenAsync(request.RefreshToken, cancellationToken);
        if (user == null)
        {
            return Unit.Value;
        }

        try
        {
            user.RevokeRefreshToken(request.RefreshToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _cache.SetAsync(
                $"identity:token:blacklist:{request.RefreshToken}",
                value: true,
                expiry: TimeSpan.FromDays(30),
                cancellationToken);
        }
        catch (DomainException)
        {
            // Token already revoked — treat as already logged out
        }

        _logger.LogInformation("User {UserId} logged out.", user.Id);

        return Unit.Value;
    }
}
