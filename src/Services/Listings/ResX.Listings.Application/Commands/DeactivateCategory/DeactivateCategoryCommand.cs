using MediatR;

namespace ResX.Listings.Application.Commands.DeactivateCategory;

public record DeactivateCategoryCommand(
    Guid CategoryId,
    Guid RequestingUserId) : IRequest<Unit>;
