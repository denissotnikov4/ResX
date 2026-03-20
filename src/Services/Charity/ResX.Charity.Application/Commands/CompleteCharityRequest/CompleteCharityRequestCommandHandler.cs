using MediatR;
using Microsoft.Extensions.Logging;
using ResX.Charity.Application.Repositories;
using ResX.Charity.Domain.AggregateRoots;
using ResX.Common.Exceptions;
using ResX.Common.Persistence;

namespace ResX.Charity.Application.Commands.CompleteCharityRequest;

public class CompleteCharityRequestCommandHandler : IRequestHandler<CompleteCharityRequestCommand, Unit>
{
    private readonly ICharityRequestRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CompleteCharityRequestCommandHandler> _logger;

    public CompleteCharityRequestCommandHandler(
        ICharityRequestRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<CompleteCharityRequestCommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Unit> Handle(CompleteCharityRequestCommand request, CancellationToken cancellationToken)
    {
        var charityRequest = await _repository.GetByIdAsync(request.RequestId, cancellationToken)
            ?? throw new NotFoundException(nameof(CharityRequest), request.RequestId);

        charityRequest.Complete();

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("CharityRequest {RequestId} completed.", charityRequest.Id);

        return Unit.Value;
    }
}
