using MediatR;
using ResX.Notifications.Domain.Enums;

namespace ResX.Notifications.Application.Commands.CreateNotification;

public record CreateNotificationCommand(
    Guid UserId,
    NotificationType Type,
    string Title,
    string Body,
    object? Payload = null) : IRequest<Guid>;