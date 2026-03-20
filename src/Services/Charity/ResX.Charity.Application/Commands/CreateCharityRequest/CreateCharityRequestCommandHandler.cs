using MediatR;
using Microsoft.Extensions.Logging;
using ResX.Charity.Application.Repositories;
using ResX.Charity.Domain.AggregateRoots;
using ResX.Charity.Domain.Enums;
using ResX.Common.Exceptions;
using ResX.Common.Persistence;

namespace ResX.Charity.Application.Commands.CreateCharityRequest;

public class CreateCharityRequestCommandHandler : IRequestHandler<CreateCharityRequestCommand, Guid>
{
    private readonly ILogger<CreateCharityRequestCommandHandler> _logger;
    private readonly ICharityRequestRepository _repository;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateCharityRequestCommandHandler(
        ICharityRequestRepository repository,
        IOrganizationRepository organizationRepository,
        IUnitOfWork unitOfWork,
        ILogger<CreateCharityRequestCommandHandler> logger)
    {
        _repository = repository;
        _organizationRepository = organizationRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Guid> Handle(CreateCharityRequestCommand request, CancellationToken cancellationToken)
    {
        var organization = await _organizationRepository.GetByIdAsync(request.OrganizationId, cancellationToken)
            ?? throw new NotFoundException(nameof(Organization), request.OrganizationId);

        if (organization.VerificationStatus != OrganizationVerificationStatus.Verified)
            throw new ForbiddenException("Only verified organizations can create charity requests.");

        var charityRequest = CharityRequest.Create(
            request.OrganizationId,
            request.Title,
            request.Description,
            request.DeadlineDate);

        foreach (var item in request.Items)
            charityRequest.AddRequestedItem(item.CategoryId, item.CategoryName, item.QuantityNeeded, item.Condition);

        await _repository.AddAsync(charityRequest, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("CharityRequest {RequestId} created.", charityRequest.Id);

        return charityRequest.Id;
    }
}
