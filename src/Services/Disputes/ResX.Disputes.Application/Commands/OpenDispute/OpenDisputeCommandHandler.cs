using MediatR;
using Microsoft.Extensions.Logging;
using ResX.Common.Persistence;
using ResX.Disputes.Application.Repositories;
using ResX.Disputes.Domain.AggregateRoots;

namespace ResX.Disputes.Application.Commands.OpenDispute;

public class OpenDisputeCommandHandler : IRequestHandler<OpenDisputeCommand, Guid>
{
    private readonly ILogger<OpenDisputeCommandHandler> _logger;
    private readonly IDisputeRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public OpenDisputeCommandHandler(
        IDisputeRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<OpenDisputeCommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Guid> Handle(OpenDisputeCommand request, CancellationToken cancellationToken)
    {
        var dispute = Dispute.Create(request.TransactionId, request.InitiatorId, request.RespondentId, request.Reason);

        await _repository.AddAsync(dispute, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Dispute {DisputeId} opened.", dispute.Id);

        return dispute.Id;
    }
}