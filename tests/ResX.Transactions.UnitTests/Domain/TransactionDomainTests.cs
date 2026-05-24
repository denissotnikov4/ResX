using FluentAssertions;
using ResX.Common.Exceptions;
using ResX.Transactions.Domain.AggregateRoots;
using ResX.Transactions.Domain.Enums;
using ResX.Transactions.Domain.Events;
using Xunit;

namespace ResX.Transactions.UnitTests.Domain;

public class TransactionDomainTests
{
    [Fact]
    public void Create_RaisesEventAndStartsPending()
    {
        var t = Transaction.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), TransactionType.Gift);
        t.Status.Should().Be(TransactionStatus.Pending);
        t.DomainEvents.Should().ContainSingle(e => e is TransactionCreatedDomainEvent);
    }

    [Theory]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(false, false, true)]
    public void Create_EmptyIds_Throws(bool emptyListing, bool emptyDonor, bool emptyRecipient)
    {
        var listing = emptyListing ? Guid.Empty : Guid.NewGuid();
        var donor = emptyDonor ? Guid.Empty : Guid.NewGuid();
        var recipient = emptyRecipient ? Guid.Empty : Guid.NewGuid();

        FluentActions.Invoking(() => Transaction.Create(listing, donor, recipient, TransactionType.Gift))
            .Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_DonorEqualsRecipient_Throws()
    {
        var id = Guid.NewGuid();
        FluentActions.Invoking(() => Transaction.Create(Guid.NewGuid(), id, id, TransactionType.Gift))
            .Should().Throw<DomainException>();
    }

    [Fact]
    public void DonorAgree_FromPending_Works()
    {
        var t = Transaction.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), TransactionType.Gift);
        t.DonorAgree();
        t.Status.Should().Be(TransactionStatus.DonorAgreed);
    }

    [Fact]
    public void DonorAgree_NotPending_Throws()
    {
        var t = Transaction.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), TransactionType.Gift);
        t.DonorAgree();
        FluentActions.Invoking(() => t.DonorAgree()).Should().Throw<DomainException>();
    }

    [Fact]
    public void RecipientConfirm_AfterDonorAgree_Completes()
    {
        var t = Transaction.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), TransactionType.Gift);
        t.DonorAgree();
        t.RecipientConfirmReceipt();
        t.Status.Should().Be(TransactionStatus.Completed);
        t.CompletedAt.Should().NotBeNull();
        t.DomainEvents.Should().Contain(e => e is TransactionCompletedDomainEvent);
    }

    [Fact]
    public void RecipientConfirm_NotInDonorAgreed_Throws()
    {
        var t = Transaction.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), TransactionType.Gift);
        FluentActions.Invoking(() => t.RecipientConfirmReceipt()).Should().Throw<DomainException>();
    }

    [Fact]
    public void Cancel_OutsideParticipants_Throws()
    {
        var t = Transaction.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), TransactionType.Gift);
        FluentActions.Invoking(() => t.Cancel(Guid.NewGuid())).Should().Throw<ForbiddenException>();
    }

    [Fact]
    public void Cancel_Participant_RaisesEvent()
    {
        var donor = Guid.NewGuid();
        var t = Transaction.Create(Guid.NewGuid(), donor, Guid.NewGuid(), TransactionType.Gift);
        t.Cancel(donor);
        t.Status.Should().Be(TransactionStatus.Cancelled);
        t.DomainEvents.Should().Contain(e => e is TransactionCancelledDomainEvent);
    }

    [Fact]
    public void Cancel_AlreadyCompleted_Throws()
    {
        var donor = Guid.NewGuid();
        var t = Transaction.Create(Guid.NewGuid(), donor, Guid.NewGuid(), TransactionType.Gift);
        t.DonorAgree();
        t.RecipientConfirmReceipt();
        FluentActions.Invoking(() => t.Cancel(donor)).Should().Throw<DomainException>();
    }

    [Fact]
    public void Dispute_OnPending_Works()
    {
        var t = Transaction.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), TransactionType.Gift);
        t.Dispute();
        t.Status.Should().Be(TransactionStatus.Disputed);
    }

    [Fact]
    public void Dispute_TwiceThrows()
    {
        var t = Transaction.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), TransactionType.Gift);
        t.Dispute();
        FluentActions.Invoking(() => t.Dispute()).Should().Throw<DomainException>();
    }
}
