using ResX.Common.Domain;

namespace ResX.Disputes.Domain.Entities;

public class Evidence : Entity<Guid>
{
    private Evidence()
    {
    }

    public Guid SubmittedBy { get; private set; }

    public string Description { get; private set; } = string.Empty;

    public List<string> FileUrls { get; private set; } = [];

    public Guid DisputeId { get; private set; }

    public DateTime SubmittedAt { get; private set; }

    public static Evidence Create(
        Guid disputeId,
        Guid submittedBy,
        string description,
        IEnumerable<string>? fileUrls = null)
    {
        return new Evidence
        {
            Id = Guid.NewGuid(),
            DisputeId = disputeId,
            SubmittedBy = submittedBy,
            Description = description,
            FileUrls = fileUrls?.ToList() ?? [],
            SubmittedAt = DateTime.UtcNow
        };
    }
}