using MediatR;

namespace ResX.Listings.Application.Commands.UpdateCategory;

public record UpdateCategoryCommand(
    Guid CategoryId,
    Guid RequestingUserId,
    string Name,
    string? Description,
    Guid? ParentCategoryId,
    string? IconUrl,
    int DisplayOrder) : IRequest<Unit>;
