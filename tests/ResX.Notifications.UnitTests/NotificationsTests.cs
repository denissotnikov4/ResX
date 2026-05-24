using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;
using ResX.Common.Persistence;
using ResX.Notifications.Application.Commands.CreateNotification;
using ResX.Notifications.Application.Commands.MarkAllNotificationsAsRead;
using ResX.Notifications.Application.Commands.MarkNotificationAsRead;
using ResX.Notifications.Application.IntegrationEvents.MessageSent;
using ResX.Notifications.Application.IntegrationEvents.TransactionCompleted;
using ResX.Notifications.Application.IntegrationEvents.TransactionCreated;
using ResX.Notifications.Application.Queries.GetMyNotifications;
using ResX.Notifications.Application.Repositories;
using ResX.Notifications.Domain.AggregateRoots;
using ResX.Notifications.Domain.Enums;
using Xunit;

namespace ResX.Notifications.UnitTests;

public class NotificationsTests
{
    private readonly INotificationRepository _repo = Substitute.For<INotificationRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IMediator _mediator = Substitute.For<IMediator>();

    [Fact]
    public void Notification_Create_DefaultsUnread()
    {
        var n = Notification.Create(Guid.NewGuid(), NotificationType.SystemNotification, "T", "B");
        n.IsRead.Should().BeFalse();
        n.Title.Should().Be("T");
    }

    [Fact]
    public void Notification_MarkAsRead_SetsReadFields()
    {
        var n = Notification.Create(Guid.NewGuid(), NotificationType.SystemNotification, "T", "B");
        n.MarkAsRead();
        n.IsRead.Should().BeTrue();
        n.ReadAt.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateNotification_NoPayload_AddsAndSaves()
    {
        var handler = new CreateNotificationCommandHandler(_repo, _uow,
            Substitute.For<ILogger<CreateNotificationCommandHandler>>());

        var id = await handler.Handle(
            new CreateNotificationCommand(Guid.NewGuid(), NotificationType.SystemNotification, "T", "B"),
            CancellationToken.None);

        id.Should().NotBe(Guid.Empty);
        await _repo.Received(1).AddAsync(Arg.Any<Notification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateNotification_WithPayload_AddsAndSaves()
    {
        var handler = new CreateNotificationCommandHandler(_repo, _uow,
            Substitute.For<ILogger<CreateNotificationCommandHandler>>());

        var id = await handler.Handle(
            new CreateNotificationCommand(Guid.NewGuid(), NotificationType.MessageReceived, "T", "B",
                new { foo = "bar" }), CancellationToken.None);

        id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task MarkAllNotificationsAsRead_CallsRepo()
    {
        var handler = new MarkAllNotificationsAsReadCommandHandler(_repo);
        var u = Guid.NewGuid();

        await handler.Handle(new MarkAllNotificationsAsReadCommand(u), CancellationToken.None);

        await _repo.Received(1).MarkAllAsReadAsync(u, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MarkNotificationAsRead_NotFound_NoOp()
    {
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Notification?)null);
        var handler = new MarkNotificationAsReadCommandHandler(_repo, _uow);

        await handler.Handle(new MarkNotificationAsReadCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MarkNotificationAsRead_WrongUser_NoOp()
    {
        var n = Notification.Create(Guid.NewGuid(), NotificationType.SystemNotification, "T", "B");
        _repo.GetByIdAsync(n.Id, Arg.Any<CancellationToken>()).Returns(n);
        var handler = new MarkNotificationAsReadCommandHandler(_repo, _uow);

        await handler.Handle(new MarkNotificationAsReadCommand(n.Id, Guid.NewGuid()), CancellationToken.None);

        n.IsRead.Should().BeFalse();
    }

    [Fact]
    public async Task MarkNotificationAsRead_OwnerMarks()
    {
        var userId = Guid.NewGuid();
        var n = Notification.Create(userId, NotificationType.SystemNotification, "T", "B");
        _repo.GetByIdAsync(n.Id, Arg.Any<CancellationToken>()).Returns(n);
        var handler = new MarkNotificationAsReadCommandHandler(_repo, _uow);

        await handler.Handle(new MarkNotificationAsReadCommand(n.Id, userId), CancellationToken.None);

        n.IsRead.Should().BeTrue();
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetMyNotifications_ReturnsItems()
    {
        var n = Notification.Create(Guid.NewGuid(), NotificationType.SystemNotification, "T", "B");
        _repo.GetByUserIdAsync(Arg.Any<Guid>(), 1, 10, false, Arg.Any<CancellationToken>())
            .Returns(new List<Notification> { n });
        _repo.GetUnreadCountAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(1);
        var handler = new GetMyNotificationsQueryHandler(_repo);

        var dto = await handler.Handle(new GetMyNotificationsQuery(Guid.NewGuid(), 1, 10, false), CancellationToken.None);

        dto.Items.Should().HaveCount(1);
        dto.UnreadCount.Should().Be(1);
    }

    [Fact]
    public async Task MessageReceivedNotificationHandler_SendsCommand()
    {
        var handler = new MessageReceivedNotificationHandler(_mediator);
        var evt = new MessageSentIntegrationEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "hi", DateTime.UtcNow);

        await handler.HandleAsync(evt);

        await _mediator.Received(1).Send(Arg.Any<CreateNotificationCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TransactionCompletedNotificationHandler_SendsTwoCommands()
    {
        var handler = new TransactionCompletedNotificationHandler(_mediator);
        var evt = new TransactionCompletedIntegrationEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        await handler.HandleAsync(evt);

        await _mediator.Received(2).Send(Arg.Any<CreateNotificationCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TransactionCreatedNotificationHandler_SendsCommand()
    {
        var handler = new TransactionCreatedNotificationHandler(_mediator,
            Substitute.For<ILogger<TransactionCreatedNotificationHandler>>());
        var evt = new TransactionCreatedIntegrationEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        await handler.HandleAsync(evt);

        await _mediator.Received(1).Send(Arg.Any<CreateNotificationCommand>(), Arg.Any<CancellationToken>());
    }
}
