using MediatR;
using ResX.Common.Exceptions;
using ResX.Common.Persistence;
using ResX.Transactions.Application.Repositories;
using ResX.Transactions.Domain.AggregateRoots;

namespace ResX.Transactions.Application.Commands.DisputeTransaction;

public class DisputeTransactionCommandHandler : IRequestHandler<DisputeTransactionCommand, Unit>
{
    private readonly ITransactionRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public DisputeTransactionCommandHandler(
        ITransactionRepository repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(DisputeTransactionCommand request, CancellationToken cancellationToken)
    {
        var transaction = await _repository.GetByIdAsync(request.TransactionId, cancellationToken)
                          ?? throw new NotFoundException(nameof(Transaction), request.TransactionId);

        if (transaction.DonorId != request.RequestingUserId &&
            transaction.RecipientId != request.RequestingUserId)
        {
            throw new ForbiddenException("Only transaction participants can dispute it.");
        }

        transaction.Dispute();

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
