using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;
using ResX.Common.Caching;
using ResX.Common.Exceptions;
using ResX.Common.Persistence;
using ResX.EventBus.RabbitMQ.Abstractions;
using ResX.Identity.Application.Commands.ChangePassword;
using ResX.Identity.Application.Commands.LoginUser;
using ResX.Identity.Application.Commands.Logout;
using ResX.Identity.Application.Commands.RefreshToken;
using ResX.Identity.Application.Commands.RegisterUser;
using ResX.Identity.Application.IntegrationEvents;
using ResX.Identity.Application.Queries.GetUserById;
using ResX.Identity.Application.Repositories;
using ResX.Identity.Application.Services;
using ResX.Identity.Domain.AggregateRoots;
using ResX.Identity.Domain.Enums;
using Xunit;

namespace ResX.Identity.UnitTests.Handlers;

public class IdentityHandlersTests
{
    private readonly IUserRepository _userRepo = Substitute.For<IUserRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();
    private readonly ITokenService _tokens = Substitute.For<ITokenService>();
    private readonly IEventBus _eventBus = Substitute.For<IEventBus>();
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly ICacheService _cache = Substitute.For<ICacheService>();

    private User CreateUser(bool active = true)
    {
        var u = User.Create("user@example.com", null, "hash", "F", "L", UserRole.Donor);
        if (!active) u.Deactivate();
        return u;
    }

    [Fact]
    public async Task RegisterUser_EmailExists_Throws()
    {
        _userRepo.ExistsByEmailAsync("e@e.com", Arg.Any<CancellationToken>()).Returns(true);
        var handler = new RegisterUserCommandHandler(
            _userRepo, _uow, _hasher, _tokens, _eventBus, _mediator,
            Substitute.For<ILogger<RegisterUserCommandHandler>>());

        var act = () => handler.Handle(new RegisterUserCommand("e@e.com", null, "p", "F", "L"), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task RegisterUser_NewEmail_CreatesAndPublishes()
    {
        _userRepo.ExistsByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _hasher.Hash("p").Returns("HASH");
        _tokens.GenerateAccessToken(Arg.Any<User>()).Returns("ACCESS");
        _tokens.GenerateRefreshToken().Returns("REFRESH");
        var handler = new RegisterUserCommandHandler(
            _userRepo, _uow, _hasher, _tokens, _eventBus, _mediator,
            Substitute.For<ILogger<RegisterUserCommandHandler>>());
        var cmd = new RegisterUserCommand("new@x.com", null, "p", "F", "L");

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.AccessToken.Should().Be("ACCESS");
        result.RefreshToken.Should().Be("REFRESH");
        await _userRepo.Received(1).AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
        await _eventBus.Received(1).PublishAsync(Arg.Any<UserRegisteredIntegrationEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LoginUser_NoUser_ThrowsDomainException()
    {
        _userRepo.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((User?)null);
        _userRepo.GetByPhoneAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((User?)null);
        var handler = new LoginUserCommandHandler(
            _userRepo, _uow, _hasher, _tokens, _mediator,
            Substitute.For<ILogger<LoginUserCommandHandler>>());

        var act = () => handler.Handle(new LoginUserCommand("a@b.com", "x"), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task LoginUser_Deactivated_ThrowsDomainException()
    {
        var u = CreateUser(active: false);
        _userRepo.GetByEmailAsync("user@example.com", Arg.Any<CancellationToken>()).Returns(u);
        var handler = new LoginUserCommandHandler(
            _userRepo, _uow, _hasher, _tokens, _mediator,
            Substitute.For<ILogger<LoginUserCommandHandler>>());

        var act = () => handler.Handle(new LoginUserCommand("user@example.com", "x"), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*deactivated*");
    }

    [Fact]
    public async Task LoginUser_WrongPassword_Throws()
    {
        var u = CreateUser();
        _userRepo.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(u);
        _hasher.Verify("wrong", "hash").Returns(false);
        var handler = new LoginUserCommandHandler(
            _userRepo, _uow, _hasher, _tokens, _mediator,
            Substitute.For<ILogger<LoginUserCommandHandler>>());

        var act = () => handler.Handle(new LoginUserCommand("user@example.com", "wrong"), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task LoginUser_Success_ReturnsTokens()
    {
        var u = CreateUser();
        _userRepo.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(u);
        _hasher.Verify("p", "hash").Returns(true);
        _tokens.GenerateAccessToken(u).Returns("A");
        _tokens.GenerateRefreshToken().Returns("R");
        var handler = new LoginUserCommandHandler(
            _userRepo, _uow, _hasher, _tokens, _mediator,
            Substitute.For<ILogger<LoginUserCommandHandler>>());

        var result = await handler.Handle(new LoginUserCommand("user@example.com", "p"), CancellationToken.None);

        result.AccessToken.Should().Be("A");
        result.RefreshToken.Should().Be("R");
    }

    [Fact]
    public async Task Logout_UnknownToken_ReturnsUnit()
    {
        _userRepo.GetByRefreshTokenAsync("t", Arg.Any<CancellationToken>()).Returns((User?)null);
        var handler = new LogoutCommandHandler(
            _userRepo, _uow, _cache,
            Substitute.For<ILogger<LogoutCommandHandler>>());

        var result = await handler.Handle(new LogoutCommand("t"), CancellationToken.None);

        result.Should().Be(Unit.Value);
    }

    [Fact]
    public async Task Logout_KnownToken_RevokesAndBlacklists()
    {
        var u = CreateUser();
        u.AddRefreshToken("tok", DateTime.UtcNow.AddDays(1));
        _userRepo.GetByRefreshTokenAsync("tok", Arg.Any<CancellationToken>()).Returns(u);
        var handler = new LogoutCommandHandler(
            _userRepo, _uow, _cache,
            Substitute.For<ILogger<LogoutCommandHandler>>());

        await handler.Handle(new LogoutCommand("tok"), CancellationToken.None);

        await _cache.Received(1).SetAsync(
            Arg.Is<string>(k => k.Contains("tok")),
            Arg.Any<bool>(),
            Arg.Any<TimeSpan?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RefreshToken_Blacklisted_ThrowsUnauthorized()
    {
        _cache.ExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
        var handler = new RefreshTokenCommandHandler(
            _userRepo, _uow, _tokens, _cache,
            Substitute.For<ILogger<RefreshTokenCommandHandler>>());

        var act = () => handler.Handle(new RefreshTokenCommand("t"), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task RefreshToken_UnknownUser_ThrowsUnauthorized()
    {
        _cache.ExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _userRepo.GetByRefreshTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((User?)null);
        var handler = new RefreshTokenCommandHandler(
            _userRepo, _uow, _tokens, _cache,
            Substitute.For<ILogger<RefreshTokenCommandHandler>>());

        var act = () => handler.Handle(new RefreshTokenCommand("t"), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task RefreshToken_NotActive_ThrowsUnauthorized()
    {
        _cache.ExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        var u = CreateUser();
        _userRepo.GetByRefreshTokenAsync("tok", Arg.Any<CancellationToken>()).Returns(u);
        var handler = new RefreshTokenCommandHandler(
            _userRepo, _uow, _tokens, _cache,
            Substitute.For<ILogger<RefreshTokenCommandHandler>>());

        var act = () => handler.Handle(new RefreshTokenCommand("tok"), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task RefreshToken_Valid_ReturnsNewTokens()
    {
        _cache.ExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        var u = CreateUser();
        u.AddRefreshToken("oldtok", DateTime.UtcNow.AddDays(1));
        _userRepo.GetByRefreshTokenAsync("oldtok", Arg.Any<CancellationToken>()).Returns(u);
        _tokens.GenerateAccessToken(u).Returns("NEW-A");
        _tokens.GenerateRefreshToken().Returns("NEW-R");
        var handler = new RefreshTokenCommandHandler(
            _userRepo, _uow, _tokens, _cache,
            Substitute.For<ILogger<RefreshTokenCommandHandler>>());

        var result = await handler.Handle(new RefreshTokenCommand("oldtok"), CancellationToken.None);

        result.AccessToken.Should().Be("NEW-A");
        result.RefreshToken.Should().Be("NEW-R");
    }

    [Fact]
    public async Task ChangePassword_UserNotFound_Throws()
    {
        _userRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((User?)null);
        var handler = new ChangePasswordCommandHandler(
            _userRepo, _uow, _hasher,
            Substitute.For<ILogger<ChangePasswordCommandHandler>>());

        var act = () => handler.Handle(new ChangePasswordCommand(Guid.NewGuid(), "old", "new"), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task ChangePassword_OldWrong_Throws()
    {
        var u = CreateUser();
        _userRepo.GetByIdAsync(u.Id, Arg.Any<CancellationToken>()).Returns(u);
        _hasher.Verify("old", "hash").Returns(false);
        var handler = new ChangePasswordCommandHandler(
            _userRepo, _uow, _hasher,
            Substitute.For<ILogger<ChangePasswordCommandHandler>>());

        var act = () => handler.Handle(new ChangePasswordCommand(u.Id, "old", "new"), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task ChangePassword_Success_UpdatesHash()
    {
        var u = CreateUser();
        _userRepo.GetByIdAsync(u.Id, Arg.Any<CancellationToken>()).Returns(u);
        _hasher.Verify("old", "hash").Returns(true);
        _hasher.Hash("new").Returns("NEWHASH");
        var handler = new ChangePasswordCommandHandler(
            _userRepo, _uow, _hasher,
            Substitute.For<ILogger<ChangePasswordCommandHandler>>());

        await handler.Handle(new ChangePasswordCommand(u.Id, "old", "new"), CancellationToken.None);

        u.PasswordHash.Should().Be("NEWHASH");
    }

    [Fact]
    public async Task GetUserById_NotFound_Throws()
    {
        _userRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((User?)null);
        var handler = new GetUserByIdQueryHandler(_userRepo);

        var act = () => handler.Handle(new GetUserByIdQuery(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetUserById_Found_ReturnsDto()
    {
        var u = CreateUser();
        _userRepo.GetByIdAsync(u.Id, Arg.Any<CancellationToken>()).Returns(u);
        var handler = new GetUserByIdQueryHandler(_userRepo);

        var dto = await handler.Handle(new GetUserByIdQuery(u.Id), CancellationToken.None);

        dto.Id.Should().Be(u.Id);
        dto.Email.Should().Be("user@example.com");
    }
}
