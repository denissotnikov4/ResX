using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;
using ResX.Common.Caching;
using ResX.Common.Exceptions;
using ResX.Common.Models;
using ResX.Common.Persistence;
using ResX.Users.Application.Commands.AddReview;
using ResX.Users.Application.Commands.CreateUserProfile;
using ResX.Users.Application.Commands.UpdateAvatar;
using ResX.Users.Application.Commands.UpdateEcoStats;
using ResX.Users.Application.Commands.UpdateUserProfile;
using ResX.Users.Application.DTOs;
using ResX.Users.Application.IntegrationEvents.TransactionCompleted;
using ResX.Users.Application.IntegrationEvents.UserRegistered;
using ResX.Users.Application.Queries.GetEcoLeaderboard;
using ResX.Users.Application.Queries.GetUserProfile;
using ResX.Users.Application.Queries.GetUserProfilesBatch;
using ResX.Users.Application.Queries.GetUserReviews;
using ResX.Users.Application.Repositories;
using ResX.Users.Domain.Aggregates;
using ResX.Users.Domain.Entities;
using Xunit;

namespace ResX.Users.UnitTests.Handlers;

public class UsersHandlersTests
{
    private readonly IUserProfileRepository _repo = Substitute.For<IUserProfileRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly ICacheService _cache = Substitute.For<ICacheService>();
    private readonly IMediator _mediator = Substitute.For<IMediator>();

    [Fact]
    public async Task CreateUserProfile_AddsAndSaves()
    {
        var handler = new CreateUserProfileCommandHandler(_repo, _uow,
            Substitute.For<ILogger<CreateUserProfileCommandHandler>>());

        var result = await handler.Handle(
            new CreateUserProfileCommand(Guid.NewGuid(), "F", "L", "city"), CancellationToken.None);

        result.Should().Be(Unit.Value);
        await _repo.Received(1).AddAsync(Arg.Any<UserProfile>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateUserProfile_NotFound_Throws()
    {
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((UserProfile?)null);
        var handler = new UpdateUserProfileCommandHandler(_repo, _uow, _cache);

        var act = () => handler.Handle(
            new UpdateUserProfileCommand(Guid.NewGuid(), "F", "L", "b", "c"), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpdateUserProfile_Found_UpdatesAndInvalidatesCache()
    {
        var p = UserProfile.Create(Guid.NewGuid(), "F", "L");
        _repo.GetByIdAsync(p.Id, Arg.Any<CancellationToken>()).Returns(p);
        var handler = new UpdateUserProfileCommandHandler(_repo, _uow, _cache);

        await handler.Handle(new UpdateUserProfileCommand(p.Id, "N", "L2", "bio", "city"), CancellationToken.None);

        p.FirstName.Should().Be("N");
        await _cache.Received(1).RemoveAsync(Arg.Is<string>(s => s.Contains(p.Id.ToString())), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAvatar_NotFound_Throws()
    {
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((UserProfile?)null);
        var handler = new UpdateAvatarCommandHandler(_repo, _uow, _cache);

        var act = () => handler.Handle(new UpdateAvatarCommand(Guid.NewGuid(), "url"), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpdateAvatar_Found_UpdatesAvatar()
    {
        var p = UserProfile.Create(Guid.NewGuid(), "F", "L");
        _repo.GetByIdAsync(p.Id, Arg.Any<CancellationToken>()).Returns(p);
        var handler = new UpdateAvatarCommandHandler(_repo, _uow, _cache);

        await handler.Handle(new UpdateAvatarCommand(p.Id, "http://a"), CancellationToken.None);

        p.AvatarUrl.Should().Be("http://a");
    }

    [Fact]
    public async Task AddReview_NotFound_Throws()
    {
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((UserProfile?)null);
        var handler = new AddReviewCommandHandler(_repo, _uow, _cache);

        var act = () => handler.Handle(new AddReviewCommand(Guid.NewGuid(), Guid.NewGuid(), "n", 4, "c"), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task AddReview_Found_AddsReview()
    {
        var p = UserProfile.Create(Guid.NewGuid(), "F", "L");
        _repo.GetByIdAsync(p.Id, Arg.Any<CancellationToken>()).Returns(p);
        var handler = new AddReviewCommandHandler(_repo, _uow, _cache);

        var id = await handler.Handle(new AddReviewCommand(p.Id, Guid.NewGuid(), "n", 5, "great"), CancellationToken.None);

        id.Should().NotBe(Guid.Empty);
        p.Reviews.Should().HaveCount(1);
    }

    [Fact]
    public async Task UpdateEcoStats_NotFound_Throws()
    {
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((UserProfile?)null);
        var handler = new UpdateEcoStatsCommandHandler(_repo, _uow, _cache);

        var act = () => handler.Handle(new UpdateEcoStatsCommand(Guid.NewGuid(), 1, 0, 1, 0), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpdateEcoStats_Found_Updates()
    {
        var p = UserProfile.Create(Guid.NewGuid(), "F", "L");
        _repo.GetByIdAsync(p.Id, Arg.Any<CancellationToken>()).Returns(p);
        var handler = new UpdateEcoStatsCommandHandler(_repo, _uow, _cache);

        await handler.Handle(new UpdateEcoStatsCommand(p.Id, 2, 1, 5m, 3m), CancellationToken.None);

        p.EcoStats.ItemsGifted.Should().Be(2);
    }

    [Fact]
    public async Task GetUserProfile_UsesCacheGetOrSet()
    {
        var p = UserProfile.Create(Guid.NewGuid(), "F", "L");
        var expectedDto = new UserProfileDto(p.Id, "F", "L", null, null, null, 0, 0,
            new EcoStatsDto(0, 0, 0, 0), p.CreatedAt);
        _cache.GetOrSetAsync(
                Arg.Any<string>(),
                Arg.Any<Func<Task<UserProfileDto>>>(),
                Arg.Any<TimeSpan?>(),
                Arg.Any<CancellationToken>())
            .Returns(expectedDto);
        var handler = new GetUserProfileQueryHandler(_repo, _cache);

        var dto = await handler.Handle(new GetUserProfileQuery(p.Id), CancellationToken.None);

        dto.Should().Be(expectedDto);
    }

    [Fact]
    public async Task GetUserProfilesBatch_Empty_ReturnsEmpty()
    {
        var handler = new GetUserProfilesBatchQueryHandler(_repo);
        var result = await handler.Handle(new GetUserProfilesBatchQuery(new List<Guid>()), CancellationToken.None);
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetUserProfilesBatch_NonEmpty_MapsBrief()
    {
        var p = UserProfile.Create(Guid.NewGuid(), "F", "L");
        _repo.GetByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<UserProfile> { p });
        var handler = new GetUserProfilesBatchQueryHandler(_repo);

        var result = await handler.Handle(new GetUserProfilesBatchQuery(new[] { p.Id }), CancellationToken.None);

        result.Should().ContainSingle().Which.Id.Should().Be(p.Id);
    }

    [Fact]
    public async Task GetUserReviews_PaginatesDtos()
    {
        var review = Review.Create(Guid.NewGuid(), Guid.NewGuid(), "R", 5, "c");
        var paged = new PagedList<Review>(new List<Review> { review }, 1, 1, 10);
        _repo.GetReviewsAsync(Arg.Any<Guid>(), 1, 10, Arg.Any<CancellationToken>()).Returns(paged);
        var handler = new GetUserReviewsQueryHandler(_repo);

        var result = await handler.Handle(new GetUserReviewsQuery(Guid.NewGuid(), 1, 10), CancellationToken.None);

        result.TotalCount.Should().Be(1);
        result.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetEcoLeaderboard_ReturnsDtos()
    {
        var p = UserProfile.Create(Guid.NewGuid(), "F", "L");
        var paged = new PagedList<UserProfile>(new List<UserProfile> { p }, 1, 1, 20);
        _repo.GetLeaderboardAsync(1, 20, Arg.Any<CancellationToken>()).Returns(paged);
        var handler = new GetEcoLeaderboardQueryHandler(_repo);

        var result = await handler.Handle(new GetEcoLeaderboardQuery(), CancellationToken.None);

        result.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task UserRegisteredIntegrationEventHandler_SendsCreateProfile()
    {
        var handler = new UserRegisteredIntegrationEventHandler(_mediator,
            Substitute.For<ILogger<UserRegisteredIntegrationEventHandler>>());

        await handler.HandleAsync(new UserRegisteredIntegrationEvent(Guid.NewGuid(), "e@x.com", "F", "L", "Donor"));

        await _mediator.Received(1).Send(Arg.Any<CreateUserProfileCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TransactionCompletedIntegrationEventHandler_SendsTwoUpdates()
    {
        var handler = new TransactionCompletedIntegrationEventHandler(_mediator,
            Substitute.For<ILogger<TransactionCompletedIntegrationEventHandler>>());
        var evt = new TransactionCompletedIntegrationEvent(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1000, 500, 200);

        await handler.HandleAsync(evt);

        await _mediator.Received(2).Send(Arg.Any<UpdateEcoStatsCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TransactionCompletedIntegrationEventHandler_SwallowsNotFound()
    {
        _mediator.Send(Arg.Any<UpdateEcoStatsCommand>(), Arg.Any<CancellationToken>())
            .Returns<Unit>(_ => throw new NotFoundException("UserProfile", Guid.NewGuid()));
        var handler = new TransactionCompletedIntegrationEventHandler(_mediator,
            Substitute.For<ILogger<TransactionCompletedIntegrationEventHandler>>());
        var evt = new TransactionCompletedIntegrationEvent(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1000, 500, 200);

        var act = () => handler.HandleAsync(evt);

        await act.Should().NotThrowAsync();
    }
}
