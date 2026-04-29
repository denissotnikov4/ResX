using MediatR;
using ResX.Listings.Application.DTOs;
using ResX.Listings.Application.Repositories;

namespace ResX.Listings.Application.Queries.GetCategories;

public class GetCategoriesQueryHandler : IRequestHandler<GetCategoriesQuery, IReadOnlyList<CategoryDetailsDto>>
{
    private readonly ICategoryRepository _categoryRepository;

    public GetCategoriesQueryHandler(ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public async Task<IReadOnlyList<CategoryDetailsDto>> Handle(
        GetCategoriesQuery request,
        CancellationToken cancellationToken)
    {
        var categories = await _categoryRepository.GetAllActiveAsync(cancellationToken);

        return categories.Select(c => new CategoryDetailsDto(
            c.Id,
            c.Name,
            c.Description,
            c.ParentCategoryId,
            c.IconUrl,
            c.IsActive,
            c.DisplayOrder,
            c.CreatedAt,
            c.UpdatedAt)).ToList();
    }
}
