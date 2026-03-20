using MediatR;
using Microsoft.Extensions.Logging;
using ResX.Charity.Application.Repositories;
using ResX.Charity.Domain.AggregateRoots;
using ResX.Common.Persistence;

namespace ResX.Charity.Application.Commands.CreateOrganization;

public sealed class CreateOrganizationCommandHandler : IRequestHandler<CreateOrganizationCommand, Guid>
{
    private readonly ILogger<CreateOrganizationCommandHandler> _logger;
    private readonly IOrganizationRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateOrganizationCommandHandler(
        IOrganizationRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<CreateOrganizationCommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Guid> Handle(CreateOrganizationCommand request, CancellationToken cancellationToken)
    {
        var organization = Organization.Create(
            request.UserId,
            request.Name,
            request.Description,
            request.LegalDocumentUrl);

        await _repository.AddAsync(organization, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Organization {OrgId} created.", organization.Id);

        return organization.Id;
    }
}