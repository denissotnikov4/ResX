using MediatR;
using ResX.Common.Exceptions;
using ResX.Common.Persistence;
using ResX.Disputes.Application.Repositories;
using ResX.Disputes.Domain.AggregateRoots;

namespace ResX.Disputes.Application.Commands.ResolveDispute;


public class ResolveDisputeCommandHandler : IRequestHandler<ResolveDisputeCommand, Unit>
{
    private readonly IDisputeRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public ResolveDisputeCommandHandler(IDisputeRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(ResolveDisputeCommand request, CancellationToken cancellationToken)
    {
        var dispute = await _repository.GetByIdAsync(request.DisputeId, cancellationToken)
                      ?? throw new NotFoundException(nameof(Dispute), request.DisputeId);

        dispute.Resolve(request.Resolution);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}