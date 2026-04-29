using MediatR;
using ResX.Listings.Application.DTOs;

namespace ResX.Listings.Application.Queries.GetCategoryHistory;

public record GetCategoryHistoryQuery(Guid CategoryId)
    : IRequest<IReadOnlyList<CategoryHistoryEntryDto>>;
