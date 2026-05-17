using MediatR;
using Microsoft.Extensions.Logging;
using ResX.Common.Exceptions;
using ResX.Common.Persistence;
using ResX.EventBus.RabbitMQ.Abstractions;
using ResX.Transactions.Application.IntegrationEvents;
using ResX.Transactions.Application.Repositories;
using ResX.Transactions.Application.Services;
using ResX.Transactions.Domain.AggregateRoots;

namespace ResX.Transactions.Application.Commands.ConfirmReceipt;

public class ConfirmReceiptCommandHandler : IRequestHandler<ConfirmReceiptCommand, Unit>
{
    private readonly ITransactionRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventBus _eventBus;
    private readonly IMediator _mediator;
    private readonly IListingsEcoClient _listingsEco;
    private readonly ILogger<ConfirmReceiptCommandHandler> _logger;

    public ConfirmReceiptCommandHandler(
        ITransactionRepository repository,
        IUnitOfWork unitOfWork,
        IEventBus eventBus,
        IMediator mediator,
        IListingsEcoClient listingsEco,
        ILogger<ConfirmReceiptCommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _eventBus = eventBus;
        _mediator = mediator;
        _listingsEco = listingsEco;
        _logger = logger;
    }

    public async Task<Unit> Handle(ConfirmReceiptCommand request, CancellationToken cancellationToken)
    {
        var transaction = await _repository.GetByIdAsync(request.TransactionId, cancellationToken)
                          ?? throw new NotFoundException(nameof(Transaction), request.TransactionId);

        if (transaction.RecipientId != request.RecipientId)
        {
            throw new ForbiddenException("Only the recipient can confirm receipt.");
        }

        transaction.RecipientConfirmReceipt();

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        foreach (var domainEvent in transaction.DomainEvents)
        {
            await _mediator.Publish(domainEvent, cancellationToken);
        }
        transaction.ClearDomainEvents();
        
        var eco = await _listingsEco.GetEcoAsync(transaction.ListingId, cancellationToken);
        if (eco is null)
        {
            _logger.LogWarning(
                "Listing {ListingId} not found while completing transaction {TransactionId}; eco-impact set to 0.",
                transaction.ListingId, transaction.Id);
        }

        await _eventBus.PublishAsync(
            new TransactionCompletedIntegrationEvent(
                transaction.Id,
                transaction.ListingId,
                transaction.DonorId,
                transaction.RecipientId,
                eco?.WeightGrams ?? 0,
                eco?.Co2SavedG ?? 0,
                eco?.WasteSavedG ?? 0),
            cancellationToken);

        return Unit.Value;
    }
}
