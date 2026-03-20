using MediatR;

namespace ResX.Notifications.Application.Commands.MarkNotificationAsRead;

public record MarkNotificationAsReadCommand(
    Guid NotificationId,
    Guid UserId) : IRequest<Unit>;