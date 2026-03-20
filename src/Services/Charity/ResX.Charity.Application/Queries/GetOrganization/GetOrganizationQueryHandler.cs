using MediatR;
using ResX.Charity.Application.DTOs;
using ResX.Charity.Application.Repositories;
using ResX.Charity.Domain.AggregateRoots;
using ResX.Common.Exceptions;

namespace ResX.Charity.Application.Queries.GetOrganization;

public class GetOrganizationQueryHandler : IRequestHandler<GetOrganizationQuery, OrganizationDto>
{
    private readonly IOrganizationRepository _repository;

    public GetOrganizationQueryHandler(IOrganizationRepository repository)
    {
        _repository = repository;
    }

    public async Task<OrganizationDto> Handle(GetOrganizationQuery request, CancellationToken cancellationToken)
    {
        var organization = await _repository.GetByIdAsync(request.OrganizationId, cancellationToken)
            ?? throw new NotFoundException(nameof(Organization), request.OrganizationId);

        return new OrganizationDto(
            organization.Id,
            organization.UserId,
            organization.Name,
            organization.Description,
            organization.VerificationStatus.ToString(),
            organization.LegalDocumentUrl,
            organization.CreatedAt);
    }
}
