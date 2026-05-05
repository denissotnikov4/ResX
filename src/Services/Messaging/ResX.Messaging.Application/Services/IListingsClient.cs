using ResX.Messaging.Application.DTOs;

namespace ResX.Messaging.Application.Services;

public interface IListingsClient
{
    Task<IReadOnlyDictionary<Guid, ListingSummaryDto>> GetListingSummariesAsync(
        IReadOnlyCollection<Guid> listingIds,
        CancellationToken cancellationToken = default);
}
