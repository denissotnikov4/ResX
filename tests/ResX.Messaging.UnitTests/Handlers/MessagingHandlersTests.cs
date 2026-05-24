using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;
using ResX.Common.Exceptions;
using ResX.Common.Models;
using ResX.Common.Persistence;
using ResX.EventBus.RabbitMQ.Abstractions;
using ResX.Messaging.Application.Commands.CreateConversation;
using ResX.Messaging.Application.Commands.MarkMessagesAsRead;
using ResX.Messaging.Application.Commands.SendMessage;
using ResX.Messaging.Application.DTOs;
using ResX.Messaging.Application.IntegrationEvents;
using ResX.Messaging.Application.Queries.GetConversations;
using ResX.Messaging.Application.Queries.GetMessages;
using ResX.Messaging.Application.Repositories;
using ResX.Messaging.Application.Services;
using ResX.Messaging.Domain.AggregateRoots;
using ResX.Messaging.Domain.Entities;
using Xunit;

namespace ResX.Messaging.UnitTests.Handlers;

public class MessagingHandlersTests
{
    private readonly IConversationRepository _repo = Substitute.For<IConversationRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IEventBus _eventBus = Substitute.For<IEventBus>();
    private readonly IUsersClient _users = Substitute.For<IUsersClient>();
    private readonly IListingsClient _listings = Substitute.For<IListingsClient>();

    [Fact]
    public async Task CreateConversation_Existing_ReturnsId()
    {
        var existing = Conversation.Create(new[] { Guid.NewGuid(), Guid.NewGuid() });
        _repo.GetByParticipantsAndListingAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(existing);
        var handler = new CreateConversationCommandHandler(_repo, _uow,
            Substitute.For<ILogger<CreateConversationCommandHandler>>());

        var id = await handler.Handle(
            new CreateConversationCommand(Guid.NewGuid(), Guid.NewGuid(), null, null), CancellationToken.None);

        id.Should().Be(existing.Id);
        await _repo.DidNotReceive().AddAsync(Arg.Any<Conversation>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateConversation_New_AddsAndOptionallySendsFirst()
    {
        _repo.GetByParticipantsAndListingAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns((Conversation?)null);
        var handler = new CreateConversationCommandHandler(_repo, _uow,
            Substitute.For<ILogger<CreateConversationCommandHandler>>());
        var initiator = Guid.NewGuid();
        var recipient = Guid.NewGuid();

        var id = await handler.Handle(
            new CreateConversationCommand(initiator, recipient, null, "Hi"), CancellationToken.None);

        id.Should().NotBe(Guid.Empty);
        await _repo.Received(1).AddAsync(Arg.Is<Conversation>(c => c.Messages.Count == 1), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendMessage_NotFound_Throws()
    {
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Conversation?)null);
        var handler = new SendMessageCommandHandler(_repo, _eventBus,
            Substitute.For<ILogger<SendMessageCommandHandler>>());

        var act = () => handler.Handle(new SendMessageCommand(Guid.NewGuid(), Guid.NewGuid(), "hi"), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task SendMessage_Found_PublishesAndReturnsDto()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var conv = Conversation.Create(new[] { a, b });
        _repo.GetByIdAsync(conv.Id, Arg.Any<CancellationToken>()).Returns(conv);
        var handler = new SendMessageCommandHandler(_repo, _eventBus,
            Substitute.For<ILogger<SendMessageCommandHandler>>());

        var dto = await handler.Handle(new SendMessageCommand(conv.Id, a, "hi"), CancellationToken.None);

        dto.Content.Should().Be("hi");
        await _eventBus.Received(1).PublishAsync(Arg.Any<MessageSentIntegrationEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MarkMessagesAsRead_NotFound_Throws()
    {
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Conversation?)null);
        var handler = new MarkMessagesAsReadCommandHandler(_repo);

        var act = () => handler.Handle(new MarkMessagesAsReadCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task MarkMessagesAsRead_NotParticipant_Throws()
    {
        var conv = Conversation.Create(new[] { Guid.NewGuid(), Guid.NewGuid() });
        _repo.GetByIdAsync(conv.Id, Arg.Any<CancellationToken>()).Returns(conv);
        var handler = new MarkMessagesAsReadCommandHandler(_repo);

        var act = () => handler.Handle(new MarkMessagesAsReadCommand(conv.Id, Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task MarkMessagesAsRead_Participant_CallsRepo()
    {
        var u = Guid.NewGuid();
        var conv = Conversation.Create(new[] { u, Guid.NewGuid() });
        _repo.GetByIdAsync(conv.Id, Arg.Any<CancellationToken>()).Returns(conv);
        var handler = new MarkMessagesAsReadCommandHandler(_repo);

        await handler.Handle(new MarkMessagesAsReadCommand(conv.Id, u), CancellationToken.None);

        await _repo.Received(1).MarkMessagesAsReadAsync(conv.Id, u, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetMessages_NotFound_Throws()
    {
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Conversation?)null);
        var handler = new GetMessagesQueryHandler(_repo);

        var act = () => handler.Handle(new GetMessagesQuery(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetMessages_NotParticipant_Throws()
    {
        var conv = Conversation.Create(new[] { Guid.NewGuid(), Guid.NewGuid() });
        _repo.GetByIdAsync(conv.Id, Arg.Any<CancellationToken>()).Returns(conv);
        var handler = new GetMessagesQueryHandler(_repo);

        var act = () => handler.Handle(new GetMessagesQuery(conv.Id, Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task GetMessages_Participant_ReturnsMapped()
    {
        var u = Guid.NewGuid();
        var conv = Conversation.Create(new[] { u, Guid.NewGuid() });
        _repo.GetByIdAsync(conv.Id, Arg.Any<CancellationToken>()).Returns(conv);
        var m = Message.Create(conv.Id, u, "hi");
        var paged = new PagedList<Message>(new List<Message> { m }, 1, 1, 50);
        _repo.GetMessagesAsync(conv.Id, 1, 50, Arg.Any<CancellationToken>()).Returns(paged);
        var handler = new GetMessagesQueryHandler(_repo);

        var result = await handler.Handle(new GetMessagesQuery(conv.Id, u), CancellationToken.None);

        result.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetConversations_Empty_ReturnsEmptyPaged()
    {
        var u = Guid.NewGuid();
        _repo.GetByUserIdAsync(u, 1, 20, Arg.Any<CancellationToken>())
            .Returns(new PagedList<Conversation>(new List<Conversation>(), 0, 1, 20));
        var handler = new GetConversationsQueryHandler(_repo, _users, _listings);

        var result = await handler.Handle(new GetConversationsQuery(u), CancellationToken.None);

        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetConversations_WithItems_MapsParticipantsAndListing()
    {
        var u = Guid.NewGuid();
        var other = Guid.NewGuid();
        var conv = Conversation.Create(new[] { u, other }, listingId: Guid.NewGuid());
        conv.SendMessage(other, "hello");
        var paged = new PagedList<Conversation>(new List<Conversation> { conv }, 1, 1, 20);
        _repo.GetByUserIdAsync(u, 1, 20, Arg.Any<CancellationToken>()).Returns(paged);
        _users.GetUserSummariesAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, ParticipantSummaryDto>
            {
                [u] = new(u, "Me", "L", null),
                [other] = new(other, "Other", "P", null)
            });
        _listings.GetListingSummariesAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, ListingSummaryDto>
            {
                [conv.ListingId!.Value] = new(conv.ListingId.Value, "Listing")
            });
        var handler = new GetConversationsQueryHandler(_repo, _users, _listings);

        var result = await handler.Handle(new GetConversationsQuery(u), CancellationToken.None);

        result.Items.Should().ContainSingle();
        result.Items[0].Listing.Should().NotBeNull();
        result.Items[0].UnreadCount.Should().Be(1);
    }
}
