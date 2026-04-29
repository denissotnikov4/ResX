using MediatR;
using ResX.Common.Exceptions;
using ResX.Listings.Application.DTOs;
using ResX.Listings.Application.Repositories;
using ResX.Listings.Domain.AggregateRoots;

namespace ResX.Listings.Application.Queries.GetCategoryHistory;

public class GetCategoryHistoryQueryHandler
    : IRequestHandler<GetCategoryHistoryQuery, IReadOnlyList<CategoryHistoryEntryDto>>
{
    private readonly ICategoryRepository _categoryRepository;

    public GetCategoryHistoryQueryHandler(ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public async Task<IReadOnlyList<CategoryHistoryEntryDto>> Handle(
        GetCategoryHistoryQuery request,
        CancellationToken cancellationToken)
    {
        var category = await _categoryRepository.GetByIdAsync(request.CategoryId, cancellationToken)
            ?? throw new NotFoundException(nameof(Category), request.CategoryId);

        var entries = await _categoryRepository.GetHistoryAsync(category.Id, cancellationToken);

        return entries.Select(h => new CategoryHistoryEntryDto(
            h.Id,
            h.CategoryId,
            h.ChangedByUserId,
            h.ChangeType.ToString(),
            h.OldValuesJson,
            h.NewValuesJson,
            h.ChangedAt)).ToList();
    }
}
