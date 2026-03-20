namespace ResX.Charity.Application.DTOs;

public record OrganizationDto(
    Guid Id,
    Guid UserId,
    string Name,
    string Description,
    string VerificationStatus,
    string? LegalDocumentUrl,
    DateTime CreatedAt);
