using MediatR;
using ResX.Common.Exceptions;
using ResX.Common.Persistence;
using ResX.Disputes.Application.Repositories;
using ResX.Disputes.Domain.AggregateRoots;

namespace ResX.Disputes.Application.Commands.CloseDispute;

public class CloseDisputeCommandHandler : IRequestHandler<CloseDisputeCommand, Unit>
{
    private readonly IDisputeRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public CloseDisputeCommandHandler(IDisputeRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(CloseDisputeCommand request, CancellationToken cancellationToken)
    {
        var dispute = await _repository.GetByIdAsync(request.DisputeId, cancellationToken)
                      ?? throw new NotFoundException(nameof(Dispute), request.DisputeId);

        dispute.Close();

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
