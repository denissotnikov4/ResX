using ResX.Disputes.Domain.AggregateRoots;
using ResX.Disputes.Domain.Entities;

namespace ResX.Disputes.Application.Repositories;

public interface IDisputeRepository
{
    Task<Dispute?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<Dispute>> GetByUserIdAsync(Guid userId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<List<Dispute>> GetOpenDisputesAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task AddAsync(Dispute dispute, CancellationToken cancellationToken = default);
    Task AddEvidenceAsync(Evidence evidence, CancellationToken cancellationToken = default);
}