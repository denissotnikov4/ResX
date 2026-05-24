using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;
using ResX.Common.Exceptions;
using ResX.Common.Models;
using ResX.Common.Persistence;
using ResX.EventBus.RabbitMQ.Abstractions;
using ResX.Transactions.Application.Commands.AgreeTransaction;
using ResX.Transactions.Application.Commands.CancelTransaction;
using ResX.Transactions.Application.Commands.ConfirmReceipt;
using ResX.Transactions.Application.Commands.CreateTransaction;
using ResX.Transactions.Application.Commands.DisputeTransaction;
using ResX.Transactions.Application.IntegrationEvents;
using ResX.Transactions.Application.Queries.GetMyTransactions;
using ResX.Transactions.Application.Queries.GetTransactionById;
using ResX.Transactions.Application.Repositories;
using ResX.Transactions.Application.Services;
using ResX.Transactions.Domain.AggregateRoots;
using ResX.Transactions.Domain.Enums;
using Xunit;

namespace ResX.Transactions.UnitTests.Handlers;

public class TransactionsHandlersTests
{
    private readonly ITransactionRepository _repo = Substitute.For<ITransactionRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IEventBus _eventBus = Substitute.For<IEventBus>();
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly IListingsEcoClient _ecoClient = Substitute.For<IListingsEcoClient>();

    private Transaction CreateTransaction(Guid? donorId = null, Guid? recipientId = null)
    {
        return Transaction.Create(Guid.NewGuid(), donorId ?? Guid.NewGuid(), recipientId ?? Guid.NewGuid(), TransactionType.Gift);
    }

    [Fact]
    public async Task CreateTransaction_CreatesAndPublishes()
    {
        var handler = new CreateTransactionCommandHandler(_repo, _uow, _eventBus, _mediator,
            Substitute.For<ILogger<CreateTransactionCommandHandler>>());
        var cmd = new CreateTransactionCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), TransactionType.Gift, null);

        var id = await handler.Handle(cmd, CancellationToken.None);

        id.Should().NotBe(Guid.Empty);
        await _repo.Received(1).AddAsync(Arg.Any<Transaction>(), Arg.Any<CancellationToken>());
        await _eventBus.Received(1).PublishAsync(Arg.Any<TransactionCreatedIntegrationEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AgreeTransaction_NotFound_Throws()
    {
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Transaction?)null);
        var handler = new AgreeTransactionCommandHandler(_repo, _uow,
            Substitute.For<ILogger<AgreeTransactionCommandHandler>>());

        var act = () => handler.Handle(new AgreeTransactionCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task AgreeTransaction_NotDonor_Throws()
    {
        var t = CreateTransaction();
        _repo.GetByIdAsync(t.Id, Arg.Any<CancellationToken>()).Returns(t);
        var handler = new AgreeTransactionCommandHandler(_repo, _uow,
            Substitute.For<ILogger<AgreeTransactionCommandHandler>>());

        var act = () => handler.Handle(new AgreeTransactionCommand(t.Id, Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task AgreeTransaction_Donor_Agrees()
    {
        var donor = Guid.NewGuid();
        var t = CreateTransaction(donor);
        _repo.GetByIdAsync(t.Id, Arg.Any<CancellationToken>()).Returns(t);
        var handler = new AgreeTransactionCommandHandler(_repo, _uow,
            Substitute.For<ILogger<AgreeTransactionCommandHandler>>());

        await handler.Handle(new AgreeTransactionCommand(t.Id, donor), CancellationToken.None);

        t.Status.Should().Be(TransactionStatus.DonorAgreed);
    }

    [Fact]
    public async Task CancelTransaction_NotFound_Throws()
    {
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Transaction?)null);
        var handler = new CancelTransactionCommandHandler(_repo, _uow, _eventBus, _mediator);

        var act = () => handler.Handle(new CancelTransactionCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task CancelTransaction_Participant_PublishesEvent()
    {
        var donor = Guid.NewGuid();
        var t = CreateTransaction(donor);
        _repo.GetByIdAsync(t.Id, Arg.Any<CancellationToken>()).Returns(t);
        var handler = new CancelTransactionCommandHandler(_repo, _uow, _eventBus, _mediator);

        await handler.Handle(new CancelTransactionCommand(t.Id, donor), CancellationToken.None);

        t.Status.Should().Be(TransactionStatus.Cancelled);
        await _eventBus.Received(1).PublishAsync(Arg.Any<TransactionCancelledIntegrationEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ConfirmReceipt_NotFound_Throws()
    {
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Transaction?)null);
        var handler = new ConfirmReceiptCommandHandler(_repo, _uow, _eventBus, _mediator, _ecoClient,
            Substitute.For<ILogger<ConfirmReceiptCommandHandler>>());

        var act = () => handler.Handle(new ConfirmReceiptCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task ConfirmReceipt_NotRecipient_Throws()
    {
        var t = CreateTransaction();
        t.DonorAgree();
        _repo.GetByIdAsync(t.Id, Arg.Any<CancellationToken>()).Returns(t);
        var handler = new ConfirmReceiptCommandHandler(_repo, _uow, _eventBus, _mediator, _ecoClient,
            Substitute.For<ILogger<ConfirmReceiptCommandHandler>>());

        var act = () => handler.Handle(new ConfirmReceiptCommand(t.Id, Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task ConfirmReceipt_Recipient_CompletesWithEcoInfo()
    {
        var recipient = Guid.NewGuid();
        var t = CreateTransaction(recipientId: recipient);
        t.DonorAgree();
        _repo.GetByIdAsync(t.Id, Arg.Any<CancellationToken>()).Returns(t);
        _ecoClient.GetEcoAsync(t.ListingId, Arg.Any<CancellationToken>())
            .Returns(new ListingEcoInfo(1000, 500, 200));
        var handler = new ConfirmReceiptCommandHandler(_repo, _uow, _eventBus, _mediator, _ecoClient,
            Substitute.For<ILogger<ConfirmReceiptCommandHandler>>());

        await handler.Handle(new ConfirmReceiptCommand(t.Id, recipient), CancellationToken.None);

        t.Status.Should().Be(TransactionStatus.Completed);
        await _eventBus.Received(1).PublishAsync(
            Arg.Is<TransactionCompletedIntegrationEvent>(e => e.Co2SavedG == 500 && e.WeightGrams == 1000),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ConfirmReceipt_Recipient_HandlesNullEco()
    {
        var recipient = Guid.NewGuid();
        var t = CreateTransaction(recipientId: recipient);
        t.DonorAgree();
        _repo.GetByIdAsync(t.Id, Arg.Any<CancellationToken>()).Returns(t);
        _ecoClient.GetEcoAsync(t.ListingId, Arg.Any<CancellationToken>()).Returns((ListingEcoInfo?)null);
        var handler = new ConfirmReceiptCommandHandler(_repo, _uow, _eventBus, _mediator, _ecoClient,
            Substitute.For<ILogger<ConfirmReceiptCommandHandler>>());

        await handler.Handle(new ConfirmReceiptCommand(t.Id, recipient), CancellationToken.None);

        await _eventBus.Received(1).PublishAsync(
            Arg.Is<TransactionCompletedIntegrationEvent>(e => e.Co2SavedG == 0),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DisputeTransaction_NotFound_Throws()
    {
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Transaction?)null);
        var handler = new DisputeTransactionCommandHandler(_repo, _uow);

        var act = () => handler.Handle(new DisputeTransactionCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task DisputeTransaction_NotParticipant_Throws()
    {
        var t = CreateTransaction();
        _repo.GetByIdAsync(t.Id, Arg.Any<CancellationToken>()).Returns(t);
        var handler = new DisputeTransactionCommandHandler(_repo, _uow);

        var act = () => handler.Handle(new DisputeTransactionCommand(t.Id, Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task DisputeTransaction_Participant_Disputes()
    {
        var donor = Guid.NewGuid();
        var t = CreateTransaction(donor);
        _repo.GetByIdAsync(t.Id, Arg.Any<CancellationToken>()).Returns(t);
        var handler = new DisputeTransactionCommandHandler(_repo, _uow);

        await handler.Handle(new DisputeTransactionCommand(t.Id, donor), CancellationToken.None);

        t.Status.Should().Be(TransactionStatus.Disputed);
    }

    [Fact]
    public async Task GetMyTransactions_ReturnsMapped()
    {
        var t = CreateTransaction();
        var paged = new PagedList<Transaction>(new List<Transaction> { t }, 1, 1, 20);
        _repo.GetByUserIdAsync(Arg.Any<Guid>(), 1, 20, Arg.Any<CancellationToken>()).Returns(paged);
        var handler = new GetMyTransactionsQueryHandler(_repo);

        var result = await handler.Handle(new GetMyTransactionsQuery(Guid.NewGuid()), CancellationToken.None);

        result.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetTransactionById_NotFound_Throws()
    {
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Transaction?)null);
        var handler = new GetTransactionByIdQueryHandler(_repo);

        var act = () => handler.Handle(new GetTransactionByIdQuery(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetTransactionById_Found_ReturnsDto()
    {
        var t = CreateTransaction();
        _repo.GetByIdAsync(t.Id, Arg.Any<CancellationToken>()).Returns(t);
        var handler = new GetTransactionByIdQueryHandler(_repo);

        var dto = await handler.Handle(new GetTransactionByIdQuery(t.Id), CancellationToken.None);

        dto.Id.Should().Be(t.Id);
    }
}
