using MediatR;
using Microsoft.Extensions.Logging;
using ResX.Common.Exceptions;
using ResX.Common.Persistence;
using ResX.EventBus.RabbitMQ.Abstractions;
using ResX.Identity.Application.DTOs;
using ResX.Identity.Application.IntegrationEvents;
using ResX.Identity.Application.Repositories;
using ResX.Identity.Application.Services;
using ResX.Identity.Domain.AggregateRoots;

namespace ResX.Identity.Application.Commands.RegisterUser;

public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, TokensDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IEventBus _eventBus;
    private readonly IMediator _mediator;
    private readonly ILogger<RegisterUserCommandHandler> _logger;

    public RegisterUserCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IEventBus eventBus,
        IMediator mediator,
        ILogger<RegisterUserCommandHandler> logger)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _eventBus = eventBus;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<TokensDto> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        if (await _userRepository.ExistsByEmailAsync(request.Email, cancellationToken))
        {
            throw new DomainException($"User with email '{request.Email}' already exists.");
        }

        var passwordHash = _passwordHasher.Hash(request.Password);

        var user = User.Create(
            request.Email,
            request.Phone,
            passwordHash,
            request.FirstName,
            request.LastName,
            request.Role);

        var accessToken = _tokenService.GenerateAccessToken(user);
        var refreshTokenValue = _tokenService.GenerateRefreshToken();
        var expiresAt = _tokenService.GetAccessTokenExpiry();

        var refreshToken = user.AddRefreshToken(refreshTokenValue, DateTime.UtcNow.AddDays(30));

        await _userRepository.AddAsync(user, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Publish domain events via MediatR
        foreach (var domainEvent in user.DomainEvents)
        {
            await _mediator.Publish(domainEvent, cancellationToken);
        }
        user.ClearDomainEvents();

        // Publish integration event to RabbitMQ
        await _eventBus.PublishAsync(new UserRegisteredIntegrationEvent(
            user.Id,
            user.Email.Value,
            user.FirstName,
            user.LastName,
            user.Role.ToString()), cancellationToken);

        _logger.LogInformation("User {UserId} registered successfully.", user.Id);

        return new TokensDto(accessToken, refreshToken.Token, expiresAt);
    }
}
