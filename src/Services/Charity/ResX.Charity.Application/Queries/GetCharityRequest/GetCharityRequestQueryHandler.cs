using MediatR;
using ResX.Charity.Application.DTOs;
using ResX.Charity.Application.Repositories;
using ResX.Charity.Domain.AggregateRoots;
using ResX.Common.Exceptions;

namespace ResX.Charity.Application.Queries.GetCharityRequest;

public class GetCharityRequestQueryHandler : IRequestHandler<GetCharityRequestQuery, CharityRequestDto>
{
    private readonly ICharityRequestRepository _repository;

    public GetCharityRequestQueryHandler(ICharityRequestRepository repository)
    {
        _repository = repository;
    }

    public async Task<CharityRequestDto> Handle(GetCharityRequestQuery request, CancellationToken cancellationToken)
    {
        var charityRequest = await _repository.GetByIdAsync(request.RequestId, cancellationToken)
            ?? throw new NotFoundException(nameof(CharityRequest), request.RequestId);

        return MapToDto(charityRequest);
    }

    private static CharityRequestDto MapToDto(CharityRequest r) => new(
        r.Id,
        r.OrganizationId,
        r.Title,
        r.Description,
        r.Status.ToString(),
        r.RequestedItems
            .Select(i => new RequestedItemDto(i.Id, i.CategoryId, i.CategoryName, i.QuantityNeeded, i.QuantityReceived, i.Condition))
            .ToList().AsReadOnly(),
        r.DeadlineDate,
        r.CreatedAt);
}
