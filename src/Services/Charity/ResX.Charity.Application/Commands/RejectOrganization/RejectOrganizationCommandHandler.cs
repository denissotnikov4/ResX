using MediatR;
using Microsoft.Extensions.Logging;
using ResX.Charity.Application.Repositories;
using ResX.Charity.Domain.AggregateRoots;
using ResX.Common.Exceptions;
using ResX.Common.Persistence;

namespace ResX.Charity.Application.Commands.RejectOrganization;

public class RejectOrganizationCommandHandler : IRequestHandler<RejectOrganizationCommand, Unit>
{
    private readonly IOrganizationRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RejectOrganizationCommandHandler> _logger;

    public RejectOrganizationCommandHandler(
        IOrganizationRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<RejectOrganizationCommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Unit> Handle(RejectOrganizationCommand request, CancellationToken cancellationToken)
    {
        var organization = await _repository.GetByIdAsync(request.OrganizationId, cancellationToken)
            ?? throw new NotFoundException(nameof(Organization), request.OrganizationId);

        organization.Reject();

        await _repository.UpdateAsync(organization, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Organization {OrgId} rejected.", organization.Id);
        return Unit.Value;
    }
}
