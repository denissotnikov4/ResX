using MediatR;
using ResX.Notifications.Application.DTOs;
using ResX.Notifications.Application.Repositories;

namespace ResX.Notifications.Application.Queries.GetMyNotifications;

public class GetMyNotificationsQueryHandler : IRequestHandler<GetMyNotificationsQuery, NotificationsPageDto>
{
    private readonly INotificationRepository _repository;

    public GetMyNotificationsQueryHandler(INotificationRepository repository)
    {
        _repository = repository;
    }

    public async Task<NotificationsPageDto> Handle(GetMyNotificationsQuery request, CancellationToken cancellationToken)
    {
        var notifications = await _repository.GetByUserIdAsync(
            request.UserId,
            request.PageNumber,
            request.PageSize,
            request.OnlyUnread,
            cancellationToken);

        var unreadCount = await _repository.GetUnreadCountAsync(request.UserId, cancellationToken);

        var items = notifications.Select(n => new NotificationDto(
            n.Id,
            n.UserId,
            n.Type.ToString(),
            n.Title,
            n.Body,
            n.IsRead,
            n.Payload?.RootElement.ToString(),
            n.CreatedAt,
            n.ReadAt)).ToList().AsReadOnly();

        return new NotificationsPageDto(items, unreadCount);
    }
}
