using MediatR;
using ResX.Charity.Application.DTOs;

namespace ResX.Charity.Application.Queries.GetOrganization;

public record GetOrganizationQuery(Guid OrganizationId) : IRequest<OrganizationDto>;
