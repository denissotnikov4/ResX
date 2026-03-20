using MediatR;
using Microsoft.Extensions.Logging;
using ResX.Common.Exceptions;
using ResX.Common.Persistence;
using ResX.Transactions.Application.Repositories;
using ResX.Transactions.Domain.AggregateRoots;

namespace ResX.Transactions.Application.Commands.AgreeTransaction;

public class AgreeTransactionCommandHandler : IRequestHandler<AgreeTransactionCommand, Unit>
{
    private readonly ITransactionRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AgreeTransactionCommandHandler> _logger;

    public AgreeTransactionCommandHandler(
        ITransactionRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<AgreeTransactionCommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Unit> Handle(AgreeTransactionCommand request, CancellationToken cancellationToken)
    {
        var transaction = await _repository.GetByIdAsync(request.TransactionId, cancellationToken)
                          ?? throw new NotFoundException(nameof(Transaction), request.TransactionId);

        if (transaction.DonorId != request.DonorId)
        {
            throw new ForbiddenException("Only the donor can agree to this transaction.");
        }

        transaction.DonorAgree();

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        return Unit.Value;
    }
}