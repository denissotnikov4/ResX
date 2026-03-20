using MediatR;
using ResX.Listings.Application.DTOs;
using ResX.Listings.Application.Repositories;

namespace ResX.Listings.Application.Queries.GetCategories;

public class GetCategoriesQueryHandler : IRequestHandler<GetCategoriesQuery, IReadOnlyList<CategoryResultDto>>
{
    private readonly ICategoryRepository _categoryRepository;

    public GetCategoriesQueryHandler(ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public Task<IReadOnlyList<CategoryResultDto>> Handle(GetCategoriesQuery request, CancellationToken cancellationToken)
        => _categoryRepository.GetAllActiveAsync(cancellationToken);
}
