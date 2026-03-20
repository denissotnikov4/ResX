using Microsoft.EntityFrameworkCore;
using ResX.Common.Models;
using ResX.Transactions.Application.Repositories;
using ResX.Transactions.Domain.AggregateRoots;

namespace ResX.Transactions.Infrastructure.Persistence.Repositories;

public class TransactionRepository : ITransactionRepository
{
    private readonly TransactionsDbContext _context;

    public TransactionRepository(TransactionsDbContext context)
    {
        _context = context;
    }

    public async Task<Transaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Transactions.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<PagedList<Transaction>> GetByUserIdAsync(
        Guid userId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Transactions
            .Where(t => t.DonorId == userId || t.RecipientId == userId)
            .OrderByDescending(t => t.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedList<Transaction>(items.AsReadOnly(), totalCount, pageNumber, pageSize);
    }

    public Task AddAsync(Transaction transaction, CancellationToken cancellationToken = default)
    {
        _context.Transactions.Add(transaction);

        return Task.CompletedTask;
    }
}