using MediatR;

namespace ResX.Notifications.Application.Commands.MarkAllNotificationsAsRead;

public record MarkAllNotificationsAsReadCommand(Guid UserId) : IRequest<Unit>;