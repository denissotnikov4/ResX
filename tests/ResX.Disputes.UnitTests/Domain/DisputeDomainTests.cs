using FluentAssertions;
using ResX.Common.Exceptions;
using ResX.Disputes.Domain.AggregateRoots;
using ResX.Disputes.Domain.Enums;
using Xunit;

namespace ResX.Disputes.UnitTests.Domain;

public class DisputeDomainTests
{
    [Fact]
    public void Create_ValidArgs_ReturnsOpenDispute()
    {
        var d = Dispute.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "reason");
        d.Status.Should().Be(DisputeStatus.Open);
        d.Reason.Should().Be("reason");
        d.Evidences.Should().BeEmpty();
    }

    [Fact]
    public void Create_EmptyReason_Throws()
    {
        var act = () => Dispute.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "");
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void StartReview_FromOpen_Works()
    {
        var d = Dispute.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "x");
        d.StartReview();
        d.Status.Should().Be(DisputeStatus.UnderReview);
    }

    [Fact]
    public void StartReview_NotOpen_Throws()
    {
        var d = Dispute.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "x");
        d.Resolve("done");
        var act = () => d.StartReview();
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Resolve_ValidArgs_SetsResolved()
    {
        var d = Dispute.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "x");
        d.Resolve("OK");
        d.Status.Should().Be(DisputeStatus.Resolved);
        d.Resolution.Should().Be("OK");
        d.ResolvedAt.Should().NotBeNull();
    }

    [Fact]
    public void Resolve_AlreadyResolved_Throws()
    {
        var d = Dispute.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "x");
        d.Resolve("a");
        var act = () => d.Resolve("b");
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Resolve_EmptyResolution_Throws()
    {
        var d = Dispute.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "x");
        var act = () => d.Resolve("");
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Close_SetsClosedStatus()
    {
        var d = Dispute.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "x");
        d.Close();
        d.Status.Should().Be(DisputeStatus.Closed);
    }

    [Fact]
    public void AddEvidence_OnOpen_Adds()
    {
        var d = Dispute.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "x");
        var ev = d.AddEvidence(Guid.NewGuid(), "desc", new[] { "url1" });
        ev.FileUrls.Should().HaveCount(1);
        d.Evidences.Should().HaveCount(1);
    }

    [Fact]
    public void AddEvidence_OnClosed_Throws()
    {
        var d = Dispute.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "x");
        d.Close();
        var act = () => d.AddEvidence(Guid.NewGuid(), "desc");
        act.Should().Throw<DomainException>();
    }
}
