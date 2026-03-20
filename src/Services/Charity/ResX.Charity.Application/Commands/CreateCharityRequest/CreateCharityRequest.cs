using MediatR;
using ResX.Charity.Application.DTOs;

namespace ResX.Charity.Application.Commands.CreateCharityRequest;

public record CreateCharityRequestCommand(
    Guid OrganizationId,
    string Title,
    string Description,
    DateTime? DeadlineDate,
    IReadOnlyList<CreateRequestedItemDto> Items) : IRequest<Guid>;
