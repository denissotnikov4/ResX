using FluentAssertions;
using ResX.Charity.Domain.AggregateRoots;
using ResX.Charity.Domain.Entities;
using ResX.Charity.Domain.Enums;
using ResX.Common.Exceptions;
using Xunit;

namespace ResX.Charity.UnitTests.Domain;

public class CharityDomainTests
{
    [Fact]
    public void CharityRequest_Create_SetsActiveStatus()
    {
        var r = CharityRequest.Create(Guid.NewGuid(), "Title", "Desc", DateTime.UtcNow.AddDays(5));
        r.Status.Should().Be(CharityRequestStatus.Active);
        r.Title.Should().Be("Title");
    }

    [Fact]
    public void CharityRequest_Create_EmptyTitle_Throws()
    {
        var act = () => CharityRequest.Create(Guid.NewGuid(), "  ", "Desc");
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void CharityRequest_Create_EmptyOrgId_Throws()
    {
        var act = () => CharityRequest.Create(Guid.Empty, "Title", "Desc");
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void CharityRequest_AddItem_AppendsToCollection()
    {
        var r = CharityRequest.Create(Guid.NewGuid(), "Title", "Desc");
        r.AddRequestedItem(Guid.NewGuid(), "Cat", 5, "Used");
        r.RequestedItems.Should().HaveCount(1);
        r.RequestedItems.First().QuantityNeeded.Should().Be(5);
    }

    [Fact]
    public void CharityRequest_Complete_SetsStatus()
    {
        var r = CharityRequest.Create(Guid.NewGuid(), "T", "D");
        r.Complete();
        r.Status.Should().Be(CharityRequestStatus.Completed);
        r.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void CharityRequest_Cancel_SetsStatus()
    {
        var r = CharityRequest.Create(Guid.NewGuid(), "T", "D");
        r.Cancel();
        r.Status.Should().Be(CharityRequestStatus.Cancelled);
    }

    [Fact]
    public void Organization_Create_DefaultsToPending()
    {
        var org = Organization.Create(Guid.NewGuid(), "Org", "Desc", "doc-url");
        org.VerificationStatus.Should().Be(OrganizationVerificationStatus.Pending);
        org.LegalDocumentUrl.Should().Be("doc-url");
    }

    [Fact]
    public void Organization_Verify_ChangesStatus()
    {
        var org = Organization.Create(Guid.NewGuid(), "Org", "Desc");
        org.Verify();
        org.VerificationStatus.Should().Be(OrganizationVerificationStatus.Verified);
    }

    [Fact]
    public void Organization_Reject_ChangesStatus()
    {
        var org = Organization.Create(Guid.NewGuid(), "Org", "Desc");
        org.Reject();
        org.VerificationStatus.Should().Be(OrganizationVerificationStatus.Rejected);
    }

    [Fact]
    public void RequestedItem_IncrementReceived_CapsAtQuantityNeeded()
    {
        var item = RequestedItem.Create(Guid.NewGuid(), Guid.NewGuid(), "Cat", 5, "New");
        item.IncrementReceived(3);
        item.QuantityReceived.Should().Be(3);
        item.IncrementReceived(10);
        item.QuantityReceived.Should().Be(5);
    }
}
