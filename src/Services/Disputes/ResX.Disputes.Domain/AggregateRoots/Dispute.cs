using ResX.Common.Domain;
using ResX.Common.Exceptions;
using ResX.Disputes.Domain.Entities;
using ResX.Disputes.Domain.Enums;

namespace ResX.Disputes.Domain.AggregateRoots;

public class Dispute : AggregateRoot<Guid>
{
    private readonly List<Evidence> _evidences = [];

    private Dispute()
    {
    }

    public Guid TransactionId { get; private set; }

    public Guid InitiatorId { get; private set; }

    public Guid RespondentId { get; private set; }

    public string Reason { get; private set; } = string.Empty;

    public DisputeStatus Status { get; private set; }

    public string? Resolution { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime? ResolvedAt { get; private set; }

    public IReadOnlyCollection<Evidence> Evidences => _evidences.AsReadOnly();

    public static Dispute Create(Guid transactionId, Guid initiatorId, Guid respondentId, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainException("Reason is required.");
        }

        return new Dispute
        {
            Id = Guid.NewGuid(),
            TransactionId = transactionId,
            InitiatorId = initiatorId,
            RespondentId = respondentId,
            Reason = reason,
            Status = DisputeStatus.Open,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void StartReview()
    {
        if (Status != DisputeStatus.Open)
        {
            throw new DomainException("Dispute is not open.");
        }

        Status = DisputeStatus.UnderReview;
    }

    public void Resolve(string resolution)
    {
        if (Status is DisputeStatus.Resolved or DisputeStatus.Closed)
            throw new DomainException("Dispute is already resolved.");

        if (string.IsNullOrWhiteSpace(resolution))
        {
            throw new DomainException("Resolution text is required.");
        }

        Resolution = resolution;
        Status = DisputeStatus.Resolved;
        ResolvedAt = DateTime.UtcNow;
    }

    public void Close()
    {
        Status = DisputeStatus.Closed;
        ResolvedAt = DateTime.UtcNow;
    }

    public Evidence AddEvidence(Guid submittedBy, string description, IEnumerable<string>? fileUrls = null)
    {
        if (Status is DisputeStatus.Resolved or DisputeStatus.Closed)
        {
            throw new DomainException("Cannot add evidence to a closed dispute.");
        }

        var evidence = Evidence.Create(Id, submittedBy, description, fileUrls);
        _evidences.Add(evidence);

        return evidence;
    }
}