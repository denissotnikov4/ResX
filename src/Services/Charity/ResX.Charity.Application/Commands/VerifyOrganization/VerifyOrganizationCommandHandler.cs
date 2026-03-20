using MediatR;
using Microsoft.Extensions.Logging;
using ResX.Charity.Application.Repositories;
using ResX.Charity.Domain.AggregateRoots;
using ResX.Common.Exceptions;
using ResX.Common.Persistence;

namespace ResX.Charity.Application.Commands.VerifyOrganization;

public class VerifyOrganizationCommandHandler : IRequestHandler<VerifyOrganizationCommand, Unit>
{
    private readonly IOrganizationRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<VerifyOrganizationCommandHandler> _logger;

    public VerifyOrganizationCommandHandler(
        IOrganizationRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<VerifyOrganizationCommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Unit> Handle(VerifyOrganizationCommand request, CancellationToken cancellationToken)
    {
        var organization = await _repository.GetByIdAsync(request.OrganizationId, cancellationToken)
            ?? throw new NotFoundException(nameof(Organization), request.OrganizationId);

        organization.Verify();

        await _repository.UpdateAsync(organization, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Organization {OrgId} verified.", organization.Id);
        return Unit.Value;
    }
}
