using MediatR;
using Microsoft.Extensions.Logging;
using ResX.Common.Exceptions;
using ResX.EventBus.RabbitMQ.Abstractions;
using ResX.Messaging.Application.DTOs;
using ResX.Messaging.Application.IntegrationEvents;
using ResX.Messaging.Application.Repositories;
using ResX.Messaging.Domain.AggregateRoots;

namespace ResX.Messaging.Application.Commands.SendMessage;

public class SendMessageCommandHandler : IRequestHandler<SendMessageCommand, MessageDto>
{
    private readonly IConversationRepository _repository;
    private readonly IEventBus _eventBus;
    private readonly ILogger<SendMessageCommandHandler> _logger;

    public SendMessageCommandHandler(
        IConversationRepository repository,
        IEventBus eventBus,
        ILogger<SendMessageCommandHandler> logger)
    {
        _repository = repository;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task<MessageDto> Handle(SendMessageCommand request, CancellationToken cancellationToken)
    {
        var conversation = await _repository.GetByIdAsync(request.ConversationId, cancellationToken)
                           ?? throw new NotFoundException(nameof(Conversation), request.ConversationId);

        var message = conversation.SendMessage(request.SenderId, request.Content);

        await _repository.AppendMessageAsync(
            conversation.Id,
            message,
            conversation.LastMessageAt!.Value,
            cancellationToken);

        var recipientId = conversation.Participants.First(p => p != request.SenderId);
        await _eventBus.PublishAsync(new MessageSentIntegrationEvent(
                conversation.Id, request.SenderId, recipientId, request.Content, message.SentAt),
            cancellationToken);

        _logger.LogInformation("Message {MessageId} sent in conversation {ConversationId}.", message.Id, conversation.Id);

        return new MessageDto(
            message.Id,
            message.ConversationId,
            message.SenderId,
            message.Content,
            message.SentAt,
            message.IsRead);
    }
}
