using MediatR;
using Microsoft.Extensions.Logging;
using ResX.Common.Persistence;
using ResX.Messaging.Application.Repositories;
using ResX.Messaging.Domain.AggregateRoots;

namespace ResX.Messaging.Application.Commands.CreateConversation;

public class CreateConversationCommandHandler : IRequestHandler<CreateConversationCommand, Guid>
{
    private readonly IConversationRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateConversationCommandHandler> _logger;

    public CreateConversationCommandHandler(
        IConversationRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<CreateConversationCommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Guid> Handle(CreateConversationCommand request, CancellationToken cancellationToken)
    {
        var existing = await _repository.GetByParticipantsAndListingAsync(
            request.InitiatorId,
            request.RecipientId,
            request.ListingId,
            cancellationToken);

        if (existing != null)
        {
            return existing.Id;
        }

        var conversation = Conversation.Create(
            participants: [request.InitiatorId, request.RecipientId],
            request.ListingId);

        if (!string.IsNullOrWhiteSpace(request.InitialMessage))
        {
            conversation.SendMessage(request.InitiatorId, request.InitialMessage);
        }

        await _repository.AddAsync(conversation, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Conversation {ConversationId} created.", conversation.Id);
        
        return conversation.Id;
    }
}