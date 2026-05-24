using FluentAssertions;
using ResX.Common.Exceptions;
using ResX.Messaging.Domain.AggregateRoots;
using ResX.Messaging.Domain.Entities;
using Xunit;

namespace ResX.Messaging.UnitTests.Domain;

public class MessagingDomainTests
{
    [Fact]
    public void Conversation_Create_TwoParticipants()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var c = Conversation.Create(new[] { a, b });
        c.Participants.Should().HaveCount(2);
        c.LastMessageAt.Should().BeNull();
    }

    [Fact]
    public void Conversation_Create_RemovesDuplicates_OnlyOneParticipant_Throws()
    {
        var a = Guid.NewGuid();
        FluentActions.Invoking(() => Conversation.Create(new[] { a, a })).Should().Throw<DomainException>();
    }

    [Fact]
    public void Conversation_SendMessage_NonParticipant_Throws()
    {
        var c = Conversation.Create(new[] { Guid.NewGuid(), Guid.NewGuid() });
        FluentActions.Invoking(() => c.SendMessage(Guid.NewGuid(), "Hi"))
            .Should().Throw<ForbiddenException>();
    }

    [Fact]
    public void Conversation_SendMessage_Participant_Works()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var c = Conversation.Create(new[] { a, b });
        var m = c.SendMessage(a, "Hello");
        m.Content.Should().Be("Hello");
        c.LastMessageAt.Should().NotBeNull();
        c.HasParticipant(a).Should().BeTrue();
        c.HasParticipant(Guid.NewGuid()).Should().BeFalse();
    }

    [Fact]
    public void Message_Create_EmptyContent_Throws()
    {
        FluentActions.Invoking(() => Message.Create(Guid.NewGuid(), Guid.NewGuid(), ""))
            .Should().Throw<DomainException>();
    }

    [Fact]
    public void Message_Create_TooLong_Throws()
    {
        var s = new string('x', 2001);
        FluentActions.Invoking(() => Message.Create(Guid.NewGuid(), Guid.NewGuid(), s))
            .Should().Throw<DomainException>();
    }

    [Fact]
    public void Message_MarkAsRead_SetsTrue()
    {
        var m = Message.Create(Guid.NewGuid(), Guid.NewGuid(), "hi");
        m.IsRead.Should().BeFalse();
        m.MarkAsRead();
        m.IsRead.Should().BeTrue();
    }
}
