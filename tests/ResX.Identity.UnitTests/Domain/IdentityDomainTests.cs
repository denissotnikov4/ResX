using FluentAssertions;
using ResX.Common.Exceptions;
using ResX.Identity.Domain.AggregateRoots;
using ResX.Identity.Domain.Enums;
using ResX.Identity.Domain.Events;
using ResX.Identity.Domain.ValueObjects;
using Xunit;

namespace ResX.Identity.UnitTests.Domain;

public class IdentityDomainTests
{
    [Fact]
    public void Email_Create_ValidEmail_Normalizes()
    {
        var e = Email.Create("  Test@Example.COM ");
        e.Value.Should().Be("test@example.com");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("not-an-email")]
    [InlineData("foo@")]
    public void Email_Create_Invalid_Throws(string s)
    {
        FluentActions.Invoking(() => Email.Create(s)).Should().Throw<DomainException>();
    }

    [Fact]
    public void Email_ImplicitToString_Works()
    {
        var e = Email.Create("a@b.com");
        string s = e;
        s.Should().Be("a@b.com");
    }

    [Theory]
    [InlineData("+1234567890")]
    [InlineData("+7 (999) 123-45-67")]
    public void PhoneNumber_Create_Valid(string p)
    {
        var phone = PhoneNumber.Create(p);
        phone.Value.Should().NotBeNullOrEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("abc")]
    public void PhoneNumber_Create_Invalid_Throws(string s)
    {
        FluentActions.Invoking(() => PhoneNumber.Create(s)).Should().Throw<DomainException>();
    }

    [Fact]
    public void User_Create_RaisesRegisteredEvent()
    {
        var u = User.Create("u@x.com", null, "hash", "F", "L", UserRole.Donor);

        u.IsActive.Should().BeTrue();
        u.Role.Should().Be(UserRole.Donor);
        u.DomainEvents.Should().ContainSingle(e => e is UserRegisteredDomainEvent);
    }

    [Fact]
    public void User_AddAndRevokeRefreshToken()
    {
        var u = User.Create("u@x.com", null, "h", "F", "L", UserRole.Donor);
        var t = u.AddRefreshToken("tok", DateTime.UtcNow.AddDays(1));
        t.IsActive.Should().BeTrue();
        u.RevokeRefreshToken("tok");
        t.IsRevoked.Should().BeTrue();
    }

    [Fact]
    public void User_RevokeRefreshToken_Unknown_Throws()
    {
        var u = User.Create("u@x.com", null, "h", "F", "L", UserRole.Donor);
        FluentActions.Invoking(() => u.RevokeRefreshToken("none")).Should().Throw<DomainException>();
    }

    [Fact]
    public void User_GetActiveRefreshToken_Found_ReturnsToken()
    {
        var u = User.Create("u@x.com", null, "h", "F", "L", UserRole.Donor);
        u.AddRefreshToken("tok", DateTime.UtcNow.AddDays(1));
        u.GetActiveRefreshToken("tok").Should().NotBeNull();
        u.GetActiveRefreshToken("missing").Should().BeNull();
    }

    [Fact]
    public void User_ChangePassword_RevokesAllRefreshTokens()
    {
        var u = User.Create("u@x.com", null, "h", "F", "L", UserRole.Donor);
        var t = u.AddRefreshToken("tok", DateTime.UtcNow.AddDays(1));
        u.ChangePassword("newhash");
        u.PasswordHash.Should().Be("newhash");
        t.IsRevoked.Should().BeTrue();
    }

    [Fact]
    public void User_Deactivate_RevokesTokens()
    {
        var u = User.Create("u@x.com", null, "h", "F", "L", UserRole.Donor);
        var t = u.AddRefreshToken("tok", DateTime.UtcNow.AddDays(1));
        u.Deactivate();
        u.IsActive.Should().BeFalse();
        t.IsRevoked.Should().BeTrue();
    }

    [Fact]
    public void User_RecordLogin_RaisesLoginEvent()
    {
        var u = User.Create("u@x.com", null, "h", "F", "L", UserRole.Donor);
        u.ClearDomainEvents();
        u.RecordLogin();
        u.DomainEvents.Should().ContainSingle(e => e is UserLoggedInDomainEvent);
    }
}
