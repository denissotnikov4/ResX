using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Logging;
using ResX.Common.Persistence;
using ResX.Notifications.Application.Repositories;
using ResX.Notifications.Domain.AggregateRoots;

namespace ResX.Notifications.Application.Commands.CreateNotification;

public class CreateNotificationCommandHandler : IRequestHandler<CreateNotificationCommand, Guid>
{
    private readonly INotificationRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateNotificationCommandHandler> _logger;

    public CreateNotificationCommandHandler(
        INotificationRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<CreateNotificationCommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Guid> Handle(CreateNotificationCommand request, CancellationToken cancellationToken)
    {
        JsonDocument? payload = null;
        if (request.Payload != null)
        {
            payload = JsonDocument.Parse(JsonSerializer.Serialize(request.Payload));
        }

        var notification = Notification.Create(request.UserId, request.Type, request.Title, request.Body, payload);
        await _repository.AddAsync(notification, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Notification {NotificationId} created for user {UserId}.",
            notification.Id,
            request.UserId);
        
        return notification.Id;
    }
}