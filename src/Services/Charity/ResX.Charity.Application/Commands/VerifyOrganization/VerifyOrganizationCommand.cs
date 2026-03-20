using MediatR;

namespace ResX.Charity.Application.Commands.VerifyOrganization;

public record VerifyOrganizationCommand(Guid OrganizationId) : IRequest<Unit>;
