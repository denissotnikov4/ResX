using MediatR;
using Microsoft.Extensions.Logging;
using ResX.Common.Persistence;
using ResX.EventBus.RabbitMQ.Abstractions;
using ResX.Transactions.Application.IntegrationEvents;
using ResX.Transactions.Application.Repositories;
using ResX.Transactions.Domain.AggregateRoots;

namespace ResX.Transactions.Application.Commands.CreateTransaction;

public class CreateTransactionCommandHandler : IRequestHandler<CreateTransactionCommand, Guid>
{
    private readonly IEventBus _eventBus;
    private readonly ILogger<CreateTransactionCommandHandler> _logger;
    private readonly IMediator _mediator;
    private readonly ITransactionRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateTransactionCommandHandler(
        ITransactionRepository repository,
        IUnitOfWork unitOfWork,
        IEventBus eventBus,
        IMediator mediator,
        ILogger<CreateTransactionCommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _eventBus = eventBus;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<Guid> Handle(CreateTransactionCommand request, CancellationToken cancellationToken)
    {
        var transaction = Transaction.Create(
            request.ListingId,
            request.DonorId,
            request.RecipientId,
            request.Type,
            request.Notes);

        await _repository.AddAsync(transaction, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        foreach (var domainEvent in transaction.DomainEvents)
        {
            await _mediator.Publish(domainEvent, cancellationToken);
        }
        transaction.ClearDomainEvents();

        await _eventBus.PublishAsync(new TransactionCreatedIntegrationEvent(
                transaction.Id, transaction.ListingId, transaction.DonorId, transaction.RecipientId),
            cancellationToken);

        _logger.LogInformation("Transaction {TransactionId} created.", transaction.Id);

        return transaction.Id;
    }
}