using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;
using ResX.Charity.Application.Commands.CancelCharityRequest;
using ResX.Charity.Application.Commands.CompleteCharityRequest;
using ResX.Charity.Application.Commands.CreateCharityRequest;
using ResX.Charity.Application.Commands.CreateOrganization;
using ResX.Charity.Application.Commands.RejectOrganization;
using ResX.Charity.Application.Commands.VerifyOrganization;
using ResX.Charity.Application.DTOs;
using ResX.Charity.Application.Queries.GetCharityRequest;
using ResX.Charity.Application.Queries.GetOrganization;
using ResX.Charity.Application.Repositories;
using ResX.Charity.Domain.AggregateRoots;
using ResX.Charity.Domain.Enums;
using ResX.Common.Exceptions;
using ResX.Common.Persistence;
using Xunit;

namespace ResX.Charity.UnitTests.Handlers;

public class CharityHandlersTests
{
    private readonly ICharityRequestRepository _charityRepo = Substitute.For<ICharityRequestRepository>();
    private readonly IOrganizationRepository _orgRepo = Substitute.For<IOrganizationRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    [Fact]
    public async Task CreateCharityRequest_OrgNotFound_Throws()
    {
        _orgRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Organization?)null);
        var handler = new CreateCharityRequestCommandHandler(
            _charityRepo, _orgRepo, _uow,
            Substitute.For<ILogger<CreateCharityRequestCommandHandler>>());
        var cmd = new CreateCharityRequestCommand(Guid.NewGuid(), "T", "D", null,
            new List<CreateRequestedItemDto>().AsReadOnly());

        var act = () => handler.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task CreateCharityRequest_UnverifiedOrg_ThrowsForbidden()
    {
        var org = Organization.Create(Guid.NewGuid(), "Org", "Desc");
        _orgRepo.GetByIdAsync(org.Id, Arg.Any<CancellationToken>()).Returns(org);
        var handler = new CreateCharityRequestCommandHandler(
            _charityRepo, _orgRepo, _uow,
            Substitute.For<ILogger<CreateCharityRequestCommandHandler>>());
        var cmd = new CreateCharityRequestCommand(org.Id, "T", "D", null,
            new List<CreateRequestedItemDto>().AsReadOnly());

        var act = () => handler.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task CreateCharityRequest_Verified_CreatesAndSaves()
    {
        var org = Organization.Create(Guid.NewGuid(), "Org", "Desc");
        org.Verify();
        _orgRepo.GetByIdAsync(org.Id, Arg.Any<CancellationToken>()).Returns(org);
        var handler = new CreateCharityRequestCommandHandler(
            _charityRepo, _orgRepo, _uow,
            Substitute.For<ILogger<CreateCharityRequestCommandHandler>>());
        var items = new List<CreateRequestedItemDto>
        {
            new(Guid.NewGuid(), "Books", 5, "Used")
        };
        var cmd = new CreateCharityRequestCommand(org.Id, "T", "D", null, items.AsReadOnly());

        var id = await handler.Handle(cmd, CancellationToken.None);

        id.Should().NotBe(Guid.Empty);
        await _charityRepo.Received(1).AddAsync(Arg.Any<CharityRequest>(), Arg.Any<CancellationToken>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CompleteCharityRequest_NotFound_Throws()
    {
        _charityRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((CharityRequest?)null);
        var handler = new CompleteCharityRequestCommandHandler(
            _charityRepo, _uow,
            Substitute.For<ILogger<CompleteCharityRequestCommandHandler>>());

        var act = () => handler.Handle(new CompleteCharityRequestCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task CompleteCharityRequest_Found_Completes()
    {
        var r = CharityRequest.Create(Guid.NewGuid(), "T", "D");
        _charityRepo.GetByIdAsync(r.Id, Arg.Any<CancellationToken>()).Returns(r);
        var handler = new CompleteCharityRequestCommandHandler(
            _charityRepo, _uow,
            Substitute.For<ILogger<CompleteCharityRequestCommandHandler>>());

        var result = await handler.Handle(new CompleteCharityRequestCommand(r.Id), CancellationToken.None);

        result.Should().Be(Unit.Value);
        r.Status.Should().Be(CharityRequestStatus.Completed);
    }

    [Fact]
    public async Task CancelCharityRequest_NotFound_Throws()
    {
        _charityRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((CharityRequest?)null);
        var handler = new CancelCharityRequestCommandHandler(
            _charityRepo, _uow,
            Substitute.For<ILogger<CancelCharityRequestCommandHandler>>());

        var act = () => handler.Handle(new CancelCharityRequestCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task CancelCharityRequest_Found_Cancels()
    {
        var r = CharityRequest.Create(Guid.NewGuid(), "T", "D");
        _charityRepo.GetByIdAsync(r.Id, Arg.Any<CancellationToken>()).Returns(r);
        var handler = new CancelCharityRequestCommandHandler(
            _charityRepo, _uow,
            Substitute.For<ILogger<CancelCharityRequestCommandHandler>>());

        await handler.Handle(new CancelCharityRequestCommand(r.Id), CancellationToken.None);

        r.Status.Should().Be(CharityRequestStatus.Cancelled);
    }

    [Fact]
    public async Task CreateOrganization_PersistsAndReturnsId()
    {
        var handler = new CreateOrganizationCommandHandler(
            _orgRepo, _uow,
            Substitute.For<ILogger<CreateOrganizationCommandHandler>>());
        var cmd = new CreateOrganizationCommand(Guid.NewGuid(), "Name", "Desc", "doc");

        var id = await handler.Handle(cmd, CancellationToken.None);

        id.Should().NotBe(Guid.Empty);
        await _orgRepo.Received(1).AddAsync(Arg.Any<Organization>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task VerifyOrganization_NotFound_Throws()
    {
        _orgRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Organization?)null);
        var handler = new VerifyOrganizationCommandHandler(
            _orgRepo, _uow,
            Substitute.For<ILogger<VerifyOrganizationCommandHandler>>());

        var act = () => handler.Handle(new VerifyOrganizationCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task VerifyOrganization_Found_Verifies()
    {
        var org = Organization.Create(Guid.NewGuid(), "O", "D");
        _orgRepo.GetByIdAsync(org.Id, Arg.Any<CancellationToken>()).Returns(org);
        var handler = new VerifyOrganizationCommandHandler(
            _orgRepo, _uow,
            Substitute.For<ILogger<VerifyOrganizationCommandHandler>>());

        await handler.Handle(new VerifyOrganizationCommand(org.Id), CancellationToken.None);

        org.VerificationStatus.Should().Be(OrganizationVerificationStatus.Verified);
        await _orgRepo.Received(1).UpdateAsync(org, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RejectOrganization_NotFound_Throws()
    {
        _orgRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Organization?)null);
        var handler = new RejectOrganizationCommandHandler(
            _orgRepo, _uow,
            Substitute.For<ILogger<RejectOrganizationCommandHandler>>());

        var act = () => handler.Handle(new RejectOrganizationCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task RejectOrganization_Found_Rejects()
    {
        var org = Organization.Create(Guid.NewGuid(), "O", "D");
        _orgRepo.GetByIdAsync(org.Id, Arg.Any<CancellationToken>()).Returns(org);
        var handler = new RejectOrganizationCommandHandler(
            _orgRepo, _uow,
            Substitute.For<ILogger<RejectOrganizationCommandHandler>>());

        await handler.Handle(new RejectOrganizationCommand(org.Id), CancellationToken.None);

        org.VerificationStatus.Should().Be(OrganizationVerificationStatus.Rejected);
    }

    [Fact]
    public async Task GetCharityRequest_NotFound_Throws()
    {
        _charityRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((CharityRequest?)null);
        var handler = new GetCharityRequestQueryHandler(_charityRepo);

        var act = () => handler.Handle(new GetCharityRequestQuery(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetCharityRequest_Found_ReturnsDto()
    {
        var r = CharityRequest.Create(Guid.NewGuid(), "T", "D");
        r.AddRequestedItem(Guid.NewGuid(), "Cat", 3, "New");
        _charityRepo.GetByIdAsync(r.Id, Arg.Any<CancellationToken>()).Returns(r);
        var handler = new GetCharityRequestQueryHandler(_charityRepo);

        var dto = await handler.Handle(new GetCharityRequestQuery(r.Id), CancellationToken.None);

        dto.Id.Should().Be(r.Id);
        dto.RequestedItems.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetOrganization_NotFound_Throws()
    {
        _orgRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Organization?)null);
        var handler = new GetOrganizationQueryHandler(_orgRepo);

        var act = () => handler.Handle(new GetOrganizationQuery(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetOrganization_Found_ReturnsDto()
    {
        var org = Organization.Create(Guid.NewGuid(), "Name", "Desc", "doc");
        _orgRepo.GetByIdAsync(org.Id, Arg.Any<CancellationToken>()).Returns(org);
        var handler = new GetOrganizationQueryHandler(_orgRepo);

        var dto = await handler.Handle(new GetOrganizationQuery(org.Id), CancellationToken.None);

        dto.Id.Should().Be(org.Id);
        dto.Name.Should().Be("Name");
        dto.LegalDocumentUrl.Should().Be("doc");
    }
}
