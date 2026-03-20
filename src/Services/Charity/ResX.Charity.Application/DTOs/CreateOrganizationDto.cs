namespace ResX.Charity.Application.DTOs;

public record CreateOrganizationDto(
    string Name,
    string Description,
    string? LegalDocumentUrl);