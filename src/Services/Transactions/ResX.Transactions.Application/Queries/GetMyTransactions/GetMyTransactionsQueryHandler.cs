using MediatR;
using ResX.Common.Models;
using ResX.Transactions.Application.DTOs;
using ResX.Transactions.Application.Repositories;

namespace ResX.Transactions.Application.Queries.GetMyTransactions;

public class GetMyTransactionsQueryHandler : IRequestHandler<GetMyTransactionsQuery, PagedList<TransactionDto>>
{
    private readonly ITransactionRepository _repository;

    public GetMyTransactionsQueryHandler(ITransactionRepository repository)
    {
        _repository = repository;
    }

    public async Task<PagedList<TransactionDto>> Handle(GetMyTransactionsQuery request, CancellationToken cancellationToken)
    {
        var transactions = await _repository.GetByUserIdAsync(
            request.UserId,
            request.PageNumber,
            request.PageSize, cancellationToken);
        
        var transactionDtos = transactions.Items
            .Select(t => 
                new TransactionDto(
                    t.Id,
                    t.ListingId,
                    t.DonorId,
                    t.RecipientId,
                    t.Type.ToString(),
                    t.Status.ToString(),
                    t.Notes,
                    t.CreatedAt,
                    t.UpdatedAt,
                    t.CompletedAt))
            .ToList()
            .AsReadOnly();

        return new PagedList<TransactionDto>(
            transactionDtos,
            transactions.TotalCount,
            request.PageNumber,
            request.PageSize);
    }
}
