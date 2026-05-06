using MediatR;

namespace ResX.Listings.Application.Commands.CreateCategory;

public record CreateCategoryCommand(
    Guid RequestingUserId,
    string Name,
    string? Description,
    Guid? ParentCategoryId,
    string? IconUrl,
    int DisplayOrder,
    int Co2SavedPer100GramsG,
    int WasteSavedPer100GramsG) : IRequest<Guid>;
