using FluentAssertions;
using ResX.Common.Exceptions;
using ResX.Listings.Domain.AggregateRoots;
using ResX.Listings.Domain.Entities;
using ResX.Listings.Domain.Enums;
using ResX.Listings.Domain.Events;
using ResX.Listings.Domain.ValueObjects;
using Xunit;

namespace ResX.Listings.UnitTests.Domain;

public class ListingsDomainTests
{
    private static Listing CreateValidListing(int weight = 1000)
    {
        var loc = Location.Create("Moscow");
        return Listing.Create(
            title: "T", description: "D",
            categoryId: Guid.NewGuid(), condition: ItemCondition.Good,
            transferType: TransferType.Gift, transferMethod: TransferMethod.InPerson,
            location: loc, donorId: Guid.NewGuid(),
            weightGrams: weight, categoryCo2Per100GramsG: 100, categoryWastePer100GramsG: 50,
            tags: new[] { "a", "b", "" });
    }

    [Fact]
    public void Listing_Create_RaisesEventAndComputesEcoImpact()
    {
        var listing = CreateValidListing(weight: 1000);

        listing.Status.Should().Be(ListingStatus.Draft);
        listing.Co2SavedG.Should().Be(1000);
        listing.WasteSavedG.Should().Be(500);
        listing.Tags.Should().HaveCount(2);
        listing.DomainEvents.Should().ContainSingle(e => e is ListingCreatedDomainEvent);
    }

    [Theory]
    [InlineData("", "desc")]
    [InlineData("title", "")]
    public void Listing_Create_EmptyTitleOrDescription_Throws(string title, string desc)
    {
        var loc = Location.Create("City");
        FluentActions.Invoking(() => Listing.Create(title, desc, Guid.NewGuid(), ItemCondition.Good,
                TransferType.Gift, TransferMethod.InPerson, loc, Guid.NewGuid(), 100, 1, 1))
            .Should().Throw<DomainException>();
    }

    [Fact]
    public void Listing_Create_EmptyCategoryId_Throws()
    {
        var loc = Location.Create("City");
        FluentActions.Invoking(() => Listing.Create("t", "d", Guid.Empty, ItemCondition.Good,
                TransferType.Gift, TransferMethod.InPerson, loc, Guid.NewGuid(), 100, 1, 1))
            .Should().Throw<DomainException>();
    }

    [Fact]
    public void Listing_Create_EmptyDonorId_Throws()
    {
        var loc = Location.Create("City");
        FluentActions.Invoking(() => Listing.Create("t", "d", Guid.NewGuid(), ItemCondition.Good,
                TransferType.Gift, TransferMethod.InPerson, loc, Guid.Empty, 100, 1, 1))
            .Should().Throw<DomainException>();
    }

    [Fact]
    public void Listing_Create_ZeroWeight_Throws()
    {
        var loc = Location.Create("City");
        FluentActions.Invoking(() => Listing.Create("t", "d", Guid.NewGuid(), ItemCondition.Good,
                TransferType.Gift, TransferMethod.InPerson, loc, Guid.NewGuid(), 0, 1, 1))
            .Should().Throw<DomainException>();
    }

    [Fact]
    public void Listing_ChangeStatus_DraftToActive_Allowed()
    {
        var listing = CreateValidListing();
        listing.ChangeStatus(ListingStatus.Active);
        listing.Status.Should().Be(ListingStatus.Active);
    }

    [Fact]
    public void Listing_ChangeStatus_DraftToReserved_NotAllowed()
    {
        var listing = CreateValidListing();
        FluentActions.Invoking(() => listing.ChangeStatus(ListingStatus.Reserved))
            .Should().Throw<DomainException>();
    }

    [Fact]
    public void Listing_AddPhoto_AppendsAndUpdatesTimestamp()
    {
        var listing = CreateValidListing();
        var ph = listing.AddPhoto("url", 1);
        ph.Url.Should().Be("url");
        listing.Photos.Should().HaveCount(1);
    }

    [Fact]
    public void Listing_AddPhoto_BeyondTenLimit_Throws()
    {
        var listing = CreateValidListing();
        for (int i = 0; i < 10; i++) listing.AddPhoto($"u{i}", i);
        FluentActions.Invoking(() => listing.AddPhoto("11th", 11)).Should().Throw<DomainException>();
    }

    [Fact]
    public void Listing_RemovePhoto_NotFound_Throws()
    {
        var listing = CreateValidListing();
        FluentActions.Invoking(() => listing.RemovePhoto(Guid.NewGuid())).Should().Throw<NotFoundException>();
    }

    [Fact]
    public void Listing_RemovePhoto_Existing_Removes()
    {
        var listing = CreateValidListing();
        var p = listing.AddPhoto("u", 1);
        listing.RemovePhoto(p.Id);
        listing.Photos.Should().BeEmpty();
    }

    [Fact]
    public void Listing_IncrementViewCount_RaisesEvent()
    {
        var listing = CreateValidListing();
        listing.ClearDomainEvents();
        listing.IncrementViewCount();
        listing.ViewCount.Should().Be(1);
        listing.DomainEvents.Should().Contain(e => e is ListingViewedDomainEvent);
    }

    [Fact]
    public void Listing_Cancel_FromDraft_Works()
    {
        var listing = CreateValidListing();
        listing.Cancel();
        listing.Status.Should().Be(ListingStatus.Cancelled);
    }

    [Fact]
    public void Listing_Cancel_FromCompleted_Throws()
    {
        var listing = CreateValidListing();
        listing.ChangeStatus(ListingStatus.Active);
        listing.ChangeStatus(ListingStatus.Reserved);
        listing.ChangeStatus(ListingStatus.Completed);
        FluentActions.Invoking(() => listing.Cancel()).Should().Throw<DomainException>();
    }

    [Fact]
    public void Listing_Update_OnCompleted_Throws()
    {
        var listing = CreateValidListing();
        listing.ChangeStatus(ListingStatus.Active);
        listing.ChangeStatus(ListingStatus.Reserved);
        listing.ChangeStatus(ListingStatus.Completed);
        FluentActions.Invoking(() => listing.Update("t", "d", Guid.NewGuid(), ItemCondition.Good,
                TransferType.Gift, TransferMethod.InPerson, Location.Create("C"), 100, 1, 1))
            .Should().Throw<DomainException>();
    }

    [Fact]
    public void Listing_Update_Valid_UpdatesValues()
    {
        var listing = CreateValidListing();
        var newCat = Guid.NewGuid();
        listing.Update("t2", "d2", newCat, ItemCondition.New, TransferType.Gift, TransferMethod.InPerson,
            Location.Create("Spb"), 2000, 100, 50, new[] { "x" });
        listing.Title.Should().Be("t2");
        listing.CategoryId.Should().Be(newCat);
        listing.WeightGrams.Should().Be(2000);
        listing.Co2SavedG.Should().Be(2000);
    }

    [Fact]
    public void Category_Create_DefaultsActive()
    {
        var c = Category.Create("Cat", "desc", null, "icon", 1, 100, 50);
        c.IsActive.Should().BeTrue();
        c.Name.Should().Be("Cat");
    }

    [Fact]
    public void Category_Create_EmptyName_Throws()
    {
        FluentActions.Invoking(() => Category.Create("", "", null, null, 0, 0, 0))
            .Should().Throw<DomainException>();
    }

    [Fact]
    public void Category_Create_NegativeDisplayOrder_Throws()
    {
        FluentActions.Invoking(() => Category.Create("Cat", "", null, null, -1, 0, 0))
            .Should().Throw<DomainException>();
    }

    [Fact]
    public void Category_Update_SelfParent_Throws()
    {
        var c = Category.Create("Cat", "", null, null, 0, 0, 0);
        FluentActions.Invoking(() => c.Update("Cat", "", c.Id, null, 0, 0, 0))
            .Should().Throw<DomainException>();
    }

    [Fact]
    public void Category_Update_Valid_UpdatesValues()
    {
        var c = Category.Create("Cat", "", null, null, 0, 0, 0);
        c.Update("New", "desc", null, "icon", 5, 200, 100);
        c.Name.Should().Be("New");
        c.DisplayOrder.Should().Be(5);
        c.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Category_Deactivate_TwiceThrows()
    {
        var c = Category.Create("Cat", "", null, null, 0, 0, 0);
        c.Deactivate();
        c.IsActive.Should().BeFalse();
        FluentActions.Invoking(() => c.Deactivate()).Should().Throw<DomainException>();
    }

    [Fact]
    public void Category_Reactivate()
    {
        var c = Category.Create("Cat", "", null, null, 0, 0, 0);
        c.Deactivate();
        c.Reactivate();
        c.IsActive.Should().BeTrue();
        FluentActions.Invoking(() => c.Reactivate()).Should().Throw<DomainException>();
    }

    [Fact]
    public void Location_Create_Valid()
    {
        var loc = Location.Create("Moscow", "Central", 55.7, 37.6);
        loc.City.Should().Be("Moscow");
    }

    [Fact]
    public void Location_Create_EmptyCity_Throws()
    {
        FluentActions.Invoking(() => Location.Create("")).Should().Throw<DomainException>();
    }

    [Theory]
    [InlineData(-91, 0)]
    [InlineData(91, 0)]
    public void Location_Create_InvalidLat_Throws(double lat, double lon)
    {
        FluentActions.Invoking(() => Location.Create("City", null, lat, lon))
            .Should().Throw<DomainException>();
    }

    [Theory]
    [InlineData(0, -181)]
    [InlineData(0, 181)]
    public void Location_Create_InvalidLon_Throws(double lat, double lon)
    {
        FluentActions.Invoking(() => Location.Create("City", null, lat, lon))
            .Should().Throw<DomainException>();
    }

    [Fact]
    public void CategoryHistory_Create_SetsFields()
    {
        var h = CategoryHistory.Create(Guid.NewGuid(), Guid.NewGuid(), CategoryChangeType.Created, null, "{}");
        h.ChangeType.Should().Be(CategoryChangeType.Created);
        h.NewValuesJson.Should().Be("{}");
    }

    [Fact]
    public void ListingPhoto_Create_SetsFields()
    {
        var p = ListingPhoto.Create(Guid.NewGuid(), "url", 1);
        p.Url.Should().Be("url");
        p.DisplayOrder.Should().Be(1);
    }
}
