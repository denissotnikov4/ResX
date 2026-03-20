using MediatR;

namespace ResX.Charity.Application.Commands.RejectOrganization;

public record RejectOrganizationCommand(Guid OrganizationId) : IRequest<Unit>;
