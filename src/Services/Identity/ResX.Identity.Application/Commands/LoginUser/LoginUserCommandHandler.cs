using MediatR;
using Microsoft.Extensions.Logging;
using ResX.Common.Exceptions;
using ResX.Common.Persistence;
using ResX.Identity.Application.DTOs;
using ResX.Identity.Application.Repositories;
using ResX.Identity.Application.Services;

namespace ResX.Identity.Application.Commands.LoginUser;

public class LoginUserCommandHandler : IRequestHandler<LoginUserCommand, TokensDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IMediator _mediator;
    private readonly ILogger<LoginUserCommandHandler> _logger;

    public LoginUserCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IMediator mediator,
        ILogger<LoginUserCommandHandler> logger)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<TokensDto> Handle(LoginUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByEmailAsync(request.Login, cancellationToken)
            ?? await _userRepository.GetByPhoneAsync(request.Login, cancellationToken)
            ?? throw new DomainException("Invalid credentials.");

        if (!user.IsActive)
        {
            throw new DomainException("Account is deactivated.");
        }

        if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw new DomainException("Invalid credentials.");
        }

        user.RecordLogin();
        
        foreach (var domainEvent in user.DomainEvents)
        {
            await _mediator.Publish(domainEvent, cancellationToken);
        }
        user.ClearDomainEvents();

        var accessToken = _tokenService.GenerateAccessToken(user);
        var refreshTokenValue = _tokenService.GenerateRefreshToken();
        var expiresAt = _tokenService.GetAccessTokenExpiry();

        var refreshToken = user.AddRefreshToken(refreshTokenValue, DateTime.UtcNow.AddDays(30));

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User {UserId} logged in.", user.Id);

        return new TokensDto(accessToken, refreshToken.Token, expiresAt);
    }
}
