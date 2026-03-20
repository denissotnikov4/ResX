using MediatR;
using ResX.Notifications.Application.Repositories;

namespace ResX.Notifications.Application.Commands.MarkAllNotificationsAsRead;

public class MarkAllNotificationsAsReadCommandHandler : IRequestHandler<MarkAllNotificationsAsReadCommand, Unit>
{
    private readonly INotificationRepository _repository;

    public MarkAllNotificationsAsReadCommandHandler(INotificationRepository repository)
    {
        _repository = repository;
    }

    public async Task<Unit> Handle(MarkAllNotificationsAsReadCommand request, CancellationToken cancellationToken)
    {
        await _repository.MarkAllAsReadAsync(request.UserId, cancellationToken);

        return Unit.Value;
    }
}