using MediatR;
using Microsoft.Extensions.Logging;
using ResX.Charity.Application.Repositories;
using ResX.Charity.Domain.AggregateRoots;
using ResX.Common.Exceptions;
using ResX.Common.Persistence;

namespace ResX.Charity.Application.Commands.CancelCharityRequest;

public class CancelCharityRequestCommandHandler : IRequestHandler<CancelCharityRequestCommand, Unit>
{
    private readonly ICharityRequestRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CancelCharityRequestCommandHandler> _logger;

    public CancelCharityRequestCommandHandler(
        ICharityRequestRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<CancelCharityRequestCommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Unit> Handle(CancelCharityRequestCommand request, CancellationToken cancellationToken)
    {
        var charityRequest = await _repository.GetByIdAsync(request.RequestId, cancellationToken)
            ?? throw new NotFoundException(nameof(CharityRequest), request.RequestId);

        charityRequest.Cancel();

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("CharityRequest {RequestId} cancelled.", charityRequest.Id);

        return Unit.Value;
    }
}
