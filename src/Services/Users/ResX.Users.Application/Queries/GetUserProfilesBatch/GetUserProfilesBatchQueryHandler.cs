using MediatR;
using ResX.Users.Application.DTOs;
using ResX.Users.Application.Repositories;

namespace ResX.Users.Application.Queries.GetUserProfilesBatch;

public class GetUserProfilesBatchQueryHandler
    : IRequestHandler<GetUserProfilesBatchQuery, IReadOnlyList<UserProfileBriefDto>>
{
    private readonly IUserProfileRepository _repository;

    public GetUserProfilesBatchQueryHandler(IUserProfileRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<UserProfileBriefDto>> Handle(
        GetUserProfilesBatchQuery request,
        CancellationToken cancellationToken)
    {
        if (request.UserIds.Count == 0)
            return [];

        var profiles = await _repository.GetByIdsAsync(request.UserIds, cancellationToken);

        return profiles
            .Select(p => new UserProfileBriefDto(
                p.Id,
                p.FirstName,
                p.LastName,
                p.AvatarUrl,
                p.Rating,
                p.ReviewCount))
            .ToList();
    }
}
