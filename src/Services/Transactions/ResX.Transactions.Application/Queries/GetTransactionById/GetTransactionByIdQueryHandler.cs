using MediatR;
using ResX.Common.Exceptions;
using ResX.Transactions.Application.DTOs;
using ResX.Transactions.Application.Repositories;
using ResX.Transactions.Domain.AggregateRoots;

namespace ResX.Transactions.Application.Queries.GetTransactionById;

public class GetTransactionByIdQueryHandler : IRequestHandler<GetTransactionByIdQuery, TransactionDto>
{
    private readonly ITransactionRepository _repository;

    public GetTransactionByIdQueryHandler(ITransactionRepository repository)
    {
        _repository = repository;
    }

    public async Task<TransactionDto> Handle(GetTransactionByIdQuery request, CancellationToken cancellationToken)
    {
        var transaction = await _repository.GetByIdAsync(request.TransactionId, cancellationToken)
                          ?? throw new NotFoundException(nameof(Transaction), request.TransactionId);

        return MapToDto(transaction);
    }

    private static TransactionDto MapToDto(Transaction t)
    {
        return new TransactionDto(
            t.Id, t.ListingId, t.DonorId, t.RecipientId,
            t.Type.ToString(), t.Status.ToString(), t.Notes,
            t.CreatedAt, t.UpdatedAt, t.CompletedAt);
    }
}