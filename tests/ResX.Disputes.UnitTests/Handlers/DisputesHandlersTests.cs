using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using ResX.Common.Exceptions;
using ResX.Common.Persistence;
using ResX.Disputes.Application.Commands.AddEvidence;
using ResX.Disputes.Application.Commands.CloseDispute;
using ResX.Disputes.Application.Commands.OpenDispute;
using ResX.Disputes.Application.Commands.ResolveDispute;
using ResX.Disputes.Application.Queries.GetDispute;
using ResX.Disputes.Application.Repositories;
using ResX.Disputes.Domain.AggregateRoots;
using ResX.Disputes.Domain.Entities;
using ResX.Disputes.Domain.Enums;
using Xunit;

namespace ResX.Disputes.UnitTests.Handlers;

public class DisputesHandlersTests
{
    private readonly IDisputeRepository _repo = Substitute.For<IDisputeRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    [Fact]
    public async Task OpenDispute_CreatesAndSaves()
    {
        var handler = new OpenDisputeCommandHandler(_repo, _uow,
            Substitute.For<ILogger<OpenDisputeCommandHandler>>());
        var cmd = new OpenDisputeCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "reason");

        var id = await handler.Handle(cmd, CancellationToken.None);

        id.Should().NotBe(Guid.Empty);
        await _repo.Received(1).AddAsync(Arg.Any<Dispute>(), Arg.Any<CancellationToken>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CloseDispute_NotFound_Throws()
    {
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Dispute?)null);
        var handler = new CloseDisputeCommandHandler(_repo, _uow);

        var act = () => handler.Handle(new CloseDisputeCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task CloseDispute_Found_Closes()
    {
        var d = Dispute.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "x");
        _repo.GetByIdAsync(d.Id, Arg.Any<CancellationToken>()).Returns(d);
        var handler = new CloseDisputeCommandHandler(_repo, _uow);

        await handler.Handle(new CloseDisputeCommand(d.Id), CancellationToken.None);

        d.Status.Should().Be(DisputeStatus.Closed);
    }

    [Fact]
    public async Task ResolveDispute_NotFound_Throws()
    {
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Dispute?)null);
        var handler = new ResolveDisputeCommandHandler(_repo, _uow);

        var act = () => handler.Handle(new ResolveDisputeCommand(Guid.NewGuid(), Guid.NewGuid(), "res"), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task ResolveDispute_Found_Resolves()
    {
        var d = Dispute.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "x");
        _repo.GetByIdAsync(d.Id, Arg.Any<CancellationToken>()).Returns(d);
        var handler = new ResolveDisputeCommandHandler(_repo, _uow);

        await handler.Handle(new ResolveDisputeCommand(d.Id, Guid.NewGuid(), "decision"), CancellationToken.None);

        d.Status.Should().Be(DisputeStatus.Resolved);
        d.Resolution.Should().Be("decision");
    }

    [Fact]
    public async Task AddEvidence_NotFound_Throws()
    {
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Dispute?)null);
        var handler = new AddEvidenceCommandHandler(_repo, _uow);

        var act = () => handler.Handle(new AddEvidenceCommand(Guid.NewGuid(), Guid.NewGuid(), "d", null), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task AddEvidence_Found_AddsEvidence()
    {
        var d = Dispute.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "x");
        _repo.GetByIdAsync(d.Id, Arg.Any<CancellationToken>()).Returns(d);
        var handler = new AddEvidenceCommandHandler(_repo, _uow);

        var id = await handler.Handle(new AddEvidenceCommand(d.Id, Guid.NewGuid(), "desc", new[] { "u" }), CancellationToken.None);

        id.Should().NotBe(Guid.Empty);
        await _repo.Received(1).AddEvidenceAsync(Arg.Any<Evidence>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetDispute_NotFound_Throws()
    {
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Dispute?)null);
        var handler = new GetDisputeQueryHandler(_repo);

        var act = () => handler.Handle(new GetDisputeQuery(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetDispute_Found_ReturnsDto()
    {
        var d = Dispute.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "x");
        d.AddEvidence(Guid.NewGuid(), "desc");
        _repo.GetByIdAsync(d.Id, Arg.Any<CancellationToken>()).Returns(d);
        var handler = new GetDisputeQueryHandler(_repo);

        var dto = await handler.Handle(new GetDisputeQuery(d.Id), CancellationToken.None);

        dto.Id.Should().Be(d.Id);
        dto.Evidences.Should().HaveCount(1);
    }
}
