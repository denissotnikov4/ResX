using MediatR;
using ResX.Notifications.Application.DTOs;

namespace ResX.Notifications.Application.Queries.GetMyNotifications;

public record GetMyNotificationsQuery(
    Guid UserId,
    int PageNumber,
    int PageSize,
    bool OnlyUnread) : IRequest<NotificationsPageDto>;
