using ResX.Notifications.Domain.AggregateRoots;

namespace ResX.Notifications.Application.Repositories;

public interface INotificationRepository
{
    Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<List<Notification>> GetByUserIdAsync(
        Guid userId,
        int pageNumber,
        int pageSize,
        bool onlyUnread = false,
        CancellationToken cancellationToken = default);

    Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default);

    Task AddAsync(Notification notification, CancellationToken cancellationToken = default);

    Task MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default);
}