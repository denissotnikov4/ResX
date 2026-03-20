using ResX.Charity.Domain.Enums;
using ResX.Common.Domain;

namespace ResX.Charity.Domain.AggregateRoots;

public class Organization : AggregateRoot<Guid>
{
    private Organization()
    {
    }

    public Guid UserId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public OrganizationVerificationStatus VerificationStatus { get; private set; }

    public string? LegalDocumentUrl { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public static Organization Create(Guid userId, string name, string description, string? legalDocumentUrl = null)
    {
        return new Organization
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = name,
            Description = description,
            VerificationStatus = OrganizationVerificationStatus.Pending,
            LegalDocumentUrl = legalDocumentUrl,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Verify()
    {
        VerificationStatus = OrganizationVerificationStatus.Verified;
    }

    public void Reject()
    {
        VerificationStatus = OrganizationVerificationStatus.Rejected;
    }
}