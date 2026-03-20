using ResX.Common.Domain;
using ResX.Common.Exceptions;
using ResX.Transactions.Domain.Enums;
using ResX.Transactions.Domain.Events;

namespace ResX.Transactions.Domain.AggregateRoots;

public class Transaction : AggregateRoot<Guid>
{
    private Transaction()
    {
    }

    public Guid ListingId { get; private set; }

    public Guid DonorId { get; private set; }

    public Guid RecipientId { get; private set; }

    public TransactionType Type { get; private set; }

    public TransactionStatus Status { get; private set; }

    public string? Notes { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime? UpdatedAt { get; private set; }

    public DateTime? CompletedAt { get; private set; }

    public static Transaction Create(
        Guid listingId,
        Guid donorId,
        Guid recipientId,
        TransactionType type,
        string? notes = null)
    {
        if (listingId == Guid.Empty)
        {
            throw new DomainException("ListingId cannot be empty.");
        }

        if (donorId == Guid.Empty)
        {
            throw new DomainException("DonorId cannot be empty.");
        }

        if (recipientId == Guid.Empty)
        {
            throw new DomainException("RecipientId cannot be empty.");
        }

        if (donorId == recipientId)
        {
            throw new DomainException("Donor and recipient cannot be the same user.");
        }

        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            ListingId = listingId,
            DonorId = donorId,
            RecipientId = recipientId,
            Type = type,
            Status = TransactionStatus.Pending,
            Notes = notes,
            CreatedAt = DateTime.UtcNow
        };

        transaction.RaiseDomainEvent(new TransactionCreatedDomainEvent(
            transaction.Id, transaction.ListingId, transaction.DonorId, transaction.RecipientId));

        return transaction;
    }

    public void DonorAgree()
    {
        if (Status != TransactionStatus.Pending)
        {
            throw new DomainException($"Cannot agree to transaction in status {Status}.");
        }

        Status = TransactionStatus.DonorAgreed;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RecipientConfirmReceipt()
    {
        if (Status != TransactionStatus.DonorAgreed)
        {
            throw new DomainException($"Cannot confirm receipt in status {Status}.");
        }

        Status = TransactionStatus.Completed;
        UpdatedAt = DateTime.UtcNow;
        CompletedAt = DateTime.UtcNow;

        RaiseDomainEvent(new TransactionCompletedDomainEvent(Id, DonorId, RecipientId));
    }

    public void Cancel(Guid requestingUserId)
    {
        if (Status is TransactionStatus.Completed or TransactionStatus.Cancelled)
        {
            throw new DomainException($"Cannot cancel a transaction in status {Status}.");
        }

        if (requestingUserId != DonorId && requestingUserId != RecipientId)
        {
            throw new ForbiddenException("Only transaction participants can cancel it.");
        }

        Status = TransactionStatus.Cancelled;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new TransactionCancelledDomainEvent(Id));
    }

    public void Dispute()
    {
        if (Status is TransactionStatus.Completed or TransactionStatus.Cancelled or TransactionStatus.Disputed)
        {
            throw new DomainException($"Cannot dispute a transaction in status {Status}.");
        }

        Status = TransactionStatus.Disputed;
        UpdatedAt = DateTime.UtcNow;
    }
}