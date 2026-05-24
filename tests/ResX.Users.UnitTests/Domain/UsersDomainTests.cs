using FluentAssertions;
using ResX.Common.Exceptions;
using ResX.Users.Domain.Aggregates;
using ResX.Users.Domain.Entities;
using ResX.Users.Domain.ValueObjects;
using Xunit;

namespace ResX.Users.UnitTests.Domain;

public class UsersDomainTests
{
    [Fact]
    public void UserProfile_Create_StartsWithZeroRating()
    {
        var p = UserProfile.Create(Guid.NewGuid(), "F", "L", "City");
        p.Rating.Should().Be(0);
        p.ReviewCount.Should().Be(0);
        p.EcoStats.ItemsGifted.Should().Be(0);
        p.City.Should().Be("City");
    }

    [Fact]
    public void UserProfile_Update_ChangesFields()
    {
        var p = UserProfile.Create(Guid.NewGuid(), "F", "L");
        p.Update("New", "Last", "bio", "city");
        p.FirstName.Should().Be("New");
        p.Bio.Should().Be("bio");
        p.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void UserProfile_UpdateAvatar_SetsAvatarUrl()
    {
        var p = UserProfile.Create(Guid.NewGuid(), "F", "L");
        p.UpdateAvatar("http://a");
        p.AvatarUrl.Should().Be("http://a");
    }

    [Fact]
    public void UserProfile_AddReview_RecalculatesRating()
    {
        var p = UserProfile.Create(Guid.NewGuid(), "F", "L");
        p.AddReview(Guid.NewGuid(), "R", 5, "Great");
        p.AddReview(Guid.NewGuid(), "R2", 3, "OK");
        p.ReviewCount.Should().Be(2);
        p.Rating.Should().Be(4m);
    }

    [Fact]
    public void UserProfile_UpdateEcoStats_Aggregates()
    {
        var p = UserProfile.Create(Guid.NewGuid(), "F", "L");
        p.UpdateEcoStats(2, 1, 5m, 2m);
        p.EcoStats.ItemsGifted.Should().Be(2);
        p.EcoStats.ItemsReceived.Should().Be(1);
        p.EcoStats.Co2SavedKg.Should().Be(5m);
        p.EcoStats.WasteSavedKg.Should().Be(2m);
    }

    [Fact]
    public void Review_Create_Valid()
    {
        var r = Review.Create(Guid.NewGuid(), Guid.NewGuid(), "Name", 4, "good");
        r.Rating.Should().Be(4);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void Review_Create_InvalidRating_Throws(int rating)
    {
        FluentActions.Invoking(() => Review.Create(Guid.NewGuid(), Guid.NewGuid(), "n", rating, "c"))
            .Should().Throw<DomainException>();
    }

    [Fact]
    public void Review_Create_EmptyComment_Throws()
    {
        FluentActions.Invoking(() => Review.Create(Guid.NewGuid(), Guid.NewGuid(), "n", 3, ""))
            .Should().Throw<DomainException>();
    }

    [Fact]
    public void EcoStats_Create_NegativeBecomesZero()
    {
        var s = EcoStats.Create(-1, -2, -3m, -4m);
        s.ItemsGifted.Should().Be(0);
        s.ItemsReceived.Should().Be(0);
        s.Co2SavedKg.Should().Be(0);
        s.WasteSavedKg.Should().Be(0);
    }

    [Fact]
    public void EcoStats_AddGiftedAndReceived()
    {
        var s = EcoStats.Create(1, 1, 1m, 1m);
        var s2 = s.AddGifted(2, 5m, 3m).AddReceived(1);
        s2.ItemsGifted.Should().Be(3);
        s2.ItemsReceived.Should().Be(2);
        s2.Co2SavedKg.Should().Be(6m);
    }
}
