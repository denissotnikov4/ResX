using MediatR;

namespace ResX.Charity.Application.Commands.CreateOrganization;

public record CreateOrganizationCommand(
    Guid UserId,
    string Name,
    string Description,
    string? LegalDocumentUrl) : IRequest<Guid>;