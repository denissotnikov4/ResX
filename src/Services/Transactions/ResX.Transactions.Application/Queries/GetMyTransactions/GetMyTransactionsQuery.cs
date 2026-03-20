using MediatR;
using ResX.Common.Models;
using ResX.Transactions.Application.DTOs;

namespace ResX.Transactions.Application.Queries.GetMyTransactions;

public record GetMyTransactionsQuery(
    Guid UserId,
    int PageNumber = 1,
    int PageSize = 20) : IRequest<PagedList<TransactionDto>>;