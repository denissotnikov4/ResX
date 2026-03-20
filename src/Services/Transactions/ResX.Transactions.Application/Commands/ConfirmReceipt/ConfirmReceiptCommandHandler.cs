using MediatR;
using Microsoft.Extensions.Logging;
using ResX.Common.Exceptions;
using ResX.Common.Persistence;
using ResX.EventBus.RabbitMQ.Abstractions;
using ResX.Transactions.Application.IntegrationEvents;
using ResX.Transactions.Application.Repositories;
using ResX.Transactions.Domain.AggregateRoots;

namespace ResX.Transactions.Application.Commands.ConfirmReceipt;

public class ConfirmReceiptCommandHandler : IRequestHandler<ConfirmReceiptCommand, Unit>
{
    private readonly ITransactionRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventBus _eventBus;
    private readonly IMediator _mediator;

    public ConfirmReceiptCommandHandler(
        ITransactionRepository repository,
        IUnitOfWork unitOfWork,
        IEventBus eventBus,
        IMediator mediator)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _eventBus = eventBus;
        _mediator = mediator;
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

        await _eventBus.PublishAsync(
            new TransactionCompletedIntegrationEvent(
                transaction.Id,
                transaction.DonorId,
                transaction.RecipientId),
            cancellationToken);

        return Unit.Value;
    }
}