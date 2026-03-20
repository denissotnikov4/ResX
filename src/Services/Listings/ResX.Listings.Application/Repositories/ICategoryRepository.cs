using ResX.Listings.Application.DTOs;

namespace ResX.Listings.Application.Repositories;

public interface ICategoryRepository
{
    Task<IReadOnlyList<CategoryResultDto>> GetAllActiveAsync(CancellationToken cancellationToken = default);
}
