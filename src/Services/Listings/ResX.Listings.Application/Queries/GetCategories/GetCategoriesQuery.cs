using MediatR;
using ResX.Listings.Application.DTOs;

namespace ResX.Listings.Application.Queries.GetCategories;

public record GetCategoriesQuery : IRequest<IReadOnlyList<CategoryDetailsDto>>;
