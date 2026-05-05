using ResX.Messaging.Application.DTOs;

namespace ResX.Messaging.Application.Services;

public interface IUsersClient
{
    Task<IReadOnlyDictionary<Guid, ParticipantSummaryDto>> GetUserSummariesAsync(
        IReadOnlyCollection<Guid> userIds,
        CancellationToken cancellationToken = default);
}
