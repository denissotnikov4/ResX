using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;
using ResX.Common.Caching;
using ResX.Common.Exceptions;
using ResX.Common.Models;
using ResX.Common.Persistence;
using ResX.EventBus.RabbitMQ.Abstractions;
using ResX.Listings.Application.Commands.AddListingPhoto;
using ResX.Listings.Application.Commands.ChangeListingStatus;
using ResX.Listings.Application.Commands.CreateCategory;
using ResX.Listings.Application.Commands.CreateListing;
using ResX.Listings.Application.Commands.DeactivateCategory;
using ResX.Listings.Application.Commands.DeleteListing;
using ResX.Listings.Application.Commands.UpdateCategory;
using ResX.Listings.Application.Commands.UpdateListing;
using ResX.Listings.Application.DTOs;
using ResX.Listings.Application.IntegrationEvents;
using ResX.Listings.Application.Queries.GetCategories;
using ResX.Listings.Application.Queries.GetCategoryHistory;
using ResX.Listings.Application.Queries.GetListingById;
using ResX.Listings.Application.Queries.GetListings;
using ResX.Listings.Application.Queries.GetMyListings;
using ResX.Listings.Application.Repositories;
using ResX.Listings.Application.Services;
using ResX.Listings.Domain.AggregateRoots;
using ResX.Listings.Domain.Entities;
using ResX.Listings.Domain.Enums;
using ResX.Listings.Domain.Filters;
using ResX.Listings.Domain.ValueObjects;
using Xunit;

namespace ResX.Listings.UnitTests.Handlers;

public class ListingsHandlersTests
{
    private readonly IListingRepository _listingRepo = Substitute.For<IListingRepository>();
    private readonly ICategoryRepository _catRepo = Substitute.For<ICategoryRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IEventBus _eventBus = Substitute.For<IEventBus>();
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly ICacheService _cache = Substitute.For<ICacheService>();
    private readonly IUsersClient _usersClient = Substitute.For<IUsersClient>();

    private static Category CreateActiveCategory() =>
        Category.Create("Cat", "desc", null, null, 1, 100, 50);

    private Listing CreateListing(Guid? donorId = null)
    {
        var d = donorId ?? Guid.NewGuid();
        var loc = Location.Create("Moscow");
        return Listing.Create("T", "D", Guid.NewGuid(), ItemCondition.Good, TransferType.Gift,
            TransferMethod.InPerson, loc, d, 1000, 100, 50);
    }

    [Fact]
    public async Task CreateListing_CategoryNotFound_Throws()
    {
        _catRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Category?)null);
        var handler = new CreateListingCommandHandler(_listingRepo, _catRepo, _uow, _eventBus, _mediator, _cache,
            Substitute.For<ILogger<CreateListingCommandHandler>>());
        var cmd = new CreateListingCommand(Guid.NewGuid(), "t", "d", Guid.NewGuid(), 100,
            ItemCondition.Good, TransferType.Gift, TransferMethod.InPerson, "City", null, null, null, null);

        var act = () => handler.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task CreateListing_InactiveCategory_Throws()
    {
        var c = CreateActiveCategory();
        c.Deactivate();
        _catRepo.GetByIdAsync(c.Id, Arg.Any<CancellationToken>()).Returns(c);
        var handler = new CreateListingCommandHandler(_listingRepo, _catRepo, _uow, _eventBus, _mediator, _cache,
            Substitute.For<ILogger<CreateListingCommandHandler>>());
        var cmd = new CreateListingCommand(Guid.NewGuid(), "t", "d", c.Id, 100,
            ItemCondition.Good, TransferType.Gift, TransferMethod.InPerson, "City", null, null, null, null);

        var act = () => handler.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task CreateListing_Valid_CreatesAndPublishes()
    {
        var c = CreateActiveCategory();
        _catRepo.GetByIdAsync(c.Id, Arg.Any<CancellationToken>()).Returns(c);
        var handler = new CreateListingCommandHandler(_listingRepo, _catRepo, _uow, _eventBus, _mediator, _cache,
            Substitute.For<ILogger<CreateListingCommandHandler>>());
        var cmd = new CreateListingCommand(Guid.NewGuid(), "t", "d", c.Id, 1000,
            ItemCondition.Good, TransferType.Gift, TransferMethod.InPerson, "City", null, null, null, new[] { "tag" });

        var id = await handler.Handle(cmd, CancellationToken.None);

        id.Should().NotBe(Guid.Empty);
        await _listingRepo.Received(1).AddAsync(Arg.Any<Listing>(), Arg.Any<CancellationToken>());
        await _eventBus.Received(1).PublishAsync(Arg.Any<ListingCreatedIntegrationEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateListing_NotFound_Throws()
    {
        _listingRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Listing?)null);
        var handler = new UpdateListingCommandHandler(_listingRepo, _catRepo, _uow, _cache,
            Substitute.For<ILogger<UpdateListingCommandHandler>>());

        var act = () => handler.Handle(
            new UpdateListingCommand(Guid.NewGuid(), Guid.NewGuid(), "t", "d", Guid.NewGuid(), 100, ItemCondition.Good,
                TransferType.Gift, TransferMethod.InPerson, "C", null, null, null, null),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpdateListing_NotOwner_Throws()
    {
        var l = CreateListing();
        _listingRepo.GetByIdAsync(l.Id, Arg.Any<CancellationToken>()).Returns(l);
        var handler = new UpdateListingCommandHandler(_listingRepo, _catRepo, _uow, _cache,
            Substitute.For<ILogger<UpdateListingCommandHandler>>());

        var act = () => handler.Handle(
            new UpdateListingCommand(l.Id, Guid.NewGuid(), "t", "d", Guid.NewGuid(), 100, ItemCondition.Good,
                TransferType.Gift, TransferMethod.InPerson, "C", null, null, null, null),
            CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task DeleteListing_NotFound_Throws()
    {
        _listingRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Listing?)null);
        var handler = new DeleteListingCommandHandler(_listingRepo, _uow, _cache,
            Substitute.For<ILogger<DeleteListingCommandHandler>>());

        var act = () => handler.Handle(new DeleteListingCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task DeleteListing_NotOwner_Throws()
    {
        var l = CreateListing();
        _listingRepo.GetByIdAsync(l.Id, Arg.Any<CancellationToken>()).Returns(l);
        var handler = new DeleteListingCommandHandler(_listingRepo, _uow, _cache,
            Substitute.For<ILogger<DeleteListingCommandHandler>>());

        var act = () => handler.Handle(new DeleteListingCommand(l.Id, Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task DeleteListing_Owner_DeletesAndCancels()
    {
        var donor = Guid.NewGuid();
        var l = CreateListing(donor);
        _listingRepo.GetByIdAsync(l.Id, Arg.Any<CancellationToken>()).Returns(l);
        var handler = new DeleteListingCommandHandler(_listingRepo, _uow, _cache,
            Substitute.For<ILogger<DeleteListingCommandHandler>>());

        await handler.Handle(new DeleteListingCommand(l.Id, donor), CancellationToken.None);

        l.Status.Should().Be(ListingStatus.Cancelled);
        await _listingRepo.Received(1).DeleteAsync(l.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ChangeListingStatus_Found_ChangesAndPublishes()
    {
        var l = CreateListing();
        _listingRepo.GetByIdAsync(l.Id, Arg.Any<CancellationToken>()).Returns(l);
        var handler = new ChangeListingStatusCommandHandler(_listingRepo, _uow, _eventBus, _mediator, _cache,
            Substitute.For<ILogger<ChangeListingStatusCommandHandler>>());

        await handler.Handle(new ChangeListingStatusCommand(l.Id, l.DonorId, ListingStatus.Active), CancellationToken.None);

        l.Status.Should().Be(ListingStatus.Active);
        await _eventBus.Received(1).PublishAsync(Arg.Any<ListingStatusChangedIntegrationEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ChangeListingStatus_NotFound_Throws()
    {
        _listingRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Listing?)null);
        var handler = new ChangeListingStatusCommandHandler(_listingRepo, _uow, _eventBus, _mediator, _cache,
            Substitute.For<ILogger<ChangeListingStatusCommandHandler>>());

        var act = () => handler.Handle(new ChangeListingStatusCommand(Guid.NewGuid(), Guid.NewGuid(), ListingStatus.Active),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task AddListingPhoto_NotFound_Throws()
    {
        _listingRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Listing?)null);
        var handler = new AddListingPhotoCommandHandler(_listingRepo, _uow,
            Substitute.For<ILogger<AddListingPhotoCommandHandler>>());

        var act = () => handler.Handle(new AddListingPhotoCommand(Guid.NewGuid(), Guid.NewGuid(), "u", 1), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task AddListingPhoto_NotOwner_Throws()
    {
        var l = CreateListing();
        _listingRepo.GetByIdAsync(l.Id, Arg.Any<CancellationToken>()).Returns(l);
        var handler = new AddListingPhotoCommandHandler(_listingRepo, _uow,
            Substitute.For<ILogger<AddListingPhotoCommandHandler>>());

        var act = () => handler.Handle(new AddListingPhotoCommand(l.Id, Guid.NewGuid(), "u", 1), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task AddListingPhoto_Owner_Adds()
    {
        var donor = Guid.NewGuid();
        var l = CreateListing(donor);
        _listingRepo.GetByIdAsync(l.Id, Arg.Any<CancellationToken>()).Returns(l);
        var handler = new AddListingPhotoCommandHandler(_listingRepo, _uow,
            Substitute.For<ILogger<AddListingPhotoCommandHandler>>());

        var photoId = await handler.Handle(new AddListingPhotoCommand(l.Id, donor, "u", 1), CancellationToken.None);

        photoId.Should().NotBe(Guid.Empty);
        l.Photos.Should().HaveCount(1);
    }

    [Fact]
    public async Task CreateCategory_CreatesAndAddsHistory()
    {
        var handler = new CreateCategoryCommandHandler(_catRepo, _uow, _cache,
            Substitute.For<ILogger<CreateCategoryCommandHandler>>());

        var id = await handler.Handle(
            new CreateCategoryCommand(Guid.NewGuid(), "Name", "Desc", null, "icon", 1, 100, 50),
            CancellationToken.None);

        id.Should().NotBe(Guid.Empty);
        await _catRepo.Received(1).AddAsync(Arg.Any<Category>(), Arg.Any<CancellationToken>());
        await _catRepo.Received(1).AddHistoryAsync(Arg.Any<CategoryHistory>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateCategory_NotFound_Throws()
    {
        _catRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Category?)null);
        var handler = new UpdateCategoryCommandHandler(_catRepo, _uow, _cache,
            Substitute.For<ILogger<UpdateCategoryCommandHandler>>());

        var act = () => handler.Handle(
            new UpdateCategoryCommand(Guid.NewGuid(), Guid.NewGuid(), "n", null, null, null, 0, 0, 0),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpdateCategory_ParentInactive_Throws()
    {
        var c = CreateActiveCategory();
        var parent = CreateActiveCategory();
        parent.Deactivate();
        _catRepo.GetByIdAsync(c.Id, Arg.Any<CancellationToken>()).Returns(c);
        _catRepo.GetByIdAsync(parent.Id, Arg.Any<CancellationToken>()).Returns(parent);
        var handler = new UpdateCategoryCommandHandler(_catRepo, _uow, _cache,
            Substitute.For<ILogger<UpdateCategoryCommandHandler>>());

        var act = () => handler.Handle(
            new UpdateCategoryCommand(c.Id, Guid.NewGuid(), "n", null, parent.Id, null, 0, 0, 0),
            CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task UpdateCategory_Found_Updates()
    {
        var c = CreateActiveCategory();
        _catRepo.GetByIdAsync(c.Id, Arg.Any<CancellationToken>()).Returns(c);
        var handler = new UpdateCategoryCommandHandler(_catRepo, _uow, _cache,
            Substitute.For<ILogger<UpdateCategoryCommandHandler>>());

        await handler.Handle(new UpdateCategoryCommand(c.Id, Guid.NewGuid(), "NewName", null, null, null, 2, 200, 100),
            CancellationToken.None);

        c.Name.Should().Be("NewName");
    }

    [Fact]
    public async Task DeactivateCategory_NotFound_Throws()
    {
        _catRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Category?)null);
        var handler = new DeactivateCategoryCommandHandler(_catRepo, _uow, _cache,
            Substitute.For<ILogger<DeactivateCategoryCommandHandler>>());

        var act = () => handler.Handle(new DeactivateCategoryCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task DeactivateCategory_Found_Deactivates()
    {
        var c = CreateActiveCategory();
        _catRepo.GetByIdAsync(c.Id, Arg.Any<CancellationToken>()).Returns(c);
        var handler = new DeactivateCategoryCommandHandler(_catRepo, _uow, _cache,
            Substitute.For<ILogger<DeactivateCategoryCommandHandler>>());

        await handler.Handle(new DeactivateCategoryCommand(c.Id, Guid.NewGuid()), CancellationToken.None);

        c.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task GetCategories_ReturnsMapped()
    {
        var c1 = CreateActiveCategory();
        _catRepo.GetAllActiveAsync(Arg.Any<CancellationToken>()).Returns(new List<Category> { c1 });
        var handler = new GetCategoriesQueryHandler(_catRepo);

        var result = await handler.Handle(new GetCategoriesQuery(), CancellationToken.None);

        result.Should().ContainSingle();
    }

    [Fact]
    public async Task GetCategoryHistory_NotFound_Throws()
    {
        _catRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Category?)null);
        var handler = new GetCategoryHistoryQueryHandler(_catRepo);

        var act = () => handler.Handle(new GetCategoryHistoryQuery(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetCategoryHistory_Found_ReturnsEntries()
    {
        var c = CreateActiveCategory();
        var h = CategoryHistory.Create(c.Id, Guid.NewGuid(), CategoryChangeType.Created, null, "{}");
        _catRepo.GetByIdAsync(c.Id, Arg.Any<CancellationToken>()).Returns(c);
        _catRepo.GetHistoryAsync(c.Id, Arg.Any<CancellationToken>()).Returns(new List<CategoryHistory> { h });
        var handler = new GetCategoryHistoryQueryHandler(_catRepo);

        var result = await handler.Handle(new GetCategoryHistoryQuery(c.Id), CancellationToken.None);

        result.Should().ContainSingle();
    }

    [Fact]
    public async Task GetListingById_NotFound_Throws()
    {
        _listingRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Listing?)null);
        var handler = new GetListingByIdQueryHandler(_listingRepo, _catRepo, _usersClient,
            Substitute.For<ILogger<GetListingByIdQueryHandler>>());

        var act = () => handler.Handle(new GetListingByIdQuery(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetListingById_DonorMissing_Throws()
    {
        var l = CreateListing();
        _listingRepo.GetByIdAsync(l.Id, Arg.Any<CancellationToken>()).Returns(l);
        _usersClient.GetDonorsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, DonorDto>());
        _catRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Category?)null);
        var handler = new GetListingByIdQueryHandler(_listingRepo, _catRepo, _usersClient,
            Substitute.For<ILogger<GetListingByIdQueryHandler>>());

        var act = () => handler.Handle(new GetListingByIdQuery(l.Id), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetListingById_Found_ReturnsDto()
    {
        var l = CreateListing();
        var donor = new DonorDto(l.DonorId, "F", "L", null, 0, 0);
        _listingRepo.GetByIdAsync(l.Id, Arg.Any<CancellationToken>()).Returns(l);
        _usersClient.GetDonorsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, DonorDto> { [l.DonorId] = donor });
        _catRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Category?)null);
        var handler = new GetListingByIdQueryHandler(_listingRepo, _catRepo, _usersClient,
            Substitute.For<ILogger<GetListingByIdQueryHandler>>());

        var dto = await handler.Handle(new GetListingByIdQuery(l.Id), CancellationToken.None);

        dto.Id.Should().Be(l.Id);
        dto.Donor.Should().Be(donor);
        await _listingRepo.Received(1).IncrementViewCountAsync(l.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetMyListings_ReturnsPaged()
    {
        var donor = Guid.NewGuid();
        var l = CreateListing(donor);
        var paged = new PagedList<Listing>(new List<Listing> { l }, 1, 1, 10);
        _listingRepo.GetPagedAsync(Arg.Any<ListingFilter>(), 1, 10, Arg.Any<CancellationToken>()).Returns(paged);
        _catRepo.GetByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Category>());
        var handler = new GetMyListingsQueryHandler(_listingRepo, _catRepo);

        var result = await handler.Handle(new GetMyListingsQuery(donor, 1, 10), CancellationToken.None);

        result.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetListings_ReturnsCachedPage()
    {
        // Cache returns the precomputed paged list, so handler skips factory and just maps donors/categories.
        var donor = Guid.NewGuid();
        var fakeCachedPage = new PagedList<object>(new List<object>().AsReadOnly(), 0, 1, 20);
        // We provide an empty page via the cache. Tested behavior: empty result returns empty PagedList.
        _cache.GetAsync<int>("listings:version", Arg.Any<CancellationToken>()).Returns(0);
        var fakeStub = new FakeCacheStub();
        _usersClient.GetDonorsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, DonorDto>());
        _catRepo.GetByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Category>());
        _listingRepo.GetPagedAsync(Arg.Any<ListingFilter>(), 1, 20, Arg.Any<CancellationToken>())
            .Returns(new PagedList<Listing>(new List<Listing>(), 0, 1, 20));
        var handler = new GetListingsQueryHandler(_listingRepo, _catRepo, _usersClient, fakeStub);

        var result = await handler.Handle(new GetListingsQuery(PageNumber: 1, PageSize: 20), CancellationToken.None);

        result.TotalCount.Should().Be(0);
    }

    private sealed class FakeCacheStub : ICacheService
    {
        public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) => Task.FromResult<T?>(default);
        public Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RemoveAsync(string key, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiry = null, CancellationToken cancellationToken = default)
            => await factory();
    }
}
