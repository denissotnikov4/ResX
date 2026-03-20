using MediatR;
using Microsoft.Extensions.Logging;
using ResX.Common.Exceptions;
using ResX.Common.Persistence;
using ResX.EventBus.RabbitMQ.Abstractions;
using ResX.Messaging.Application.DTOs;
using ResX.Messaging.Application.IntegrationEvents;
using ResX.Messaging.Application.Repositories;
using ResX.Messaging.Domain.AggregateRoots;

namespace ResX.Messaging.Application.Commands.SendMessage;

public class SendMessageCommandHandler : IRequestHandler<SendMessageCommand, MessageDto>
{
    private readonly IConversationRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventBus _eventBus;
    private readonly ILogger<SendMessageCommandHandler> _logger;

    public SendMessageCommandHandler(
        IConversationRepository repository,
        IUnitOfWork unitOfWork,
        IEventBus eventBus,
        ILogger<SendMessageCommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task<MessageDto> Handle(SendMessageCommand request, CancellationToken cancellationToken)
    {
        var conversation = await _repository.GetByIdAsync(request.ConversationId, cancellationToken)
                           ?? throw new NotFoundException(nameof(Conversation), request.ConversationId);

        var message = conversation.SendMessage(request.SenderId, request.Content);
        await _repository.UpdateAsync(conversation, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Notify other participants
        var recipientId = conversation.Participants.First(p => p != request.SenderId);
        await _eventBus.PublishAsync(new MessageSentIntegrationEvent(
                conversation.Id, request.SenderId, recipientId, request.Content, message.SentAt),
            cancellationToken);

        return new MessageDto(
            message.Id,
            message.ConversationId,
            message.SenderId,
            message.Content,
            message.SentAt,
            message.IsRead);
    }
}