using MediatR;
using ResX.Common.Caching;
using ResX.Common.Exceptions;
using ResX.Users.Application.DTOs;
using ResX.Users.Application.Repositories;
using ResX.Users.Domain.Aggregates;

namespace ResX.Users.Application.Queries.GetUserProfile;

public class GetUserProfileQueryHandler : IRequestHandler<GetUserProfileQuery, UserProfileDto>
{
    private readonly IUserProfileRepository _repository;
    private readonly ICacheService _cache;

    public GetUserProfileQueryHandler(IUserProfileRepository repository, ICacheService cache)
    {
        _repository = repository;
        _cache = cache;
    }

    public async Task<UserProfileDto> Handle(GetUserProfileQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = $"users:profile:{request.UserId}";

        return await _cache.GetOrSetAsync(cacheKey, async () =>
        {
            var profile = await _repository.GetByIdAsync(request.UserId, cancellationToken)
                          ?? throw new NotFoundException(nameof(UserProfile), request.UserId);

            return MapToDto(profile);
        }, expiry: TimeSpan.FromMinutes(10), cancellationToken);
    }

    private static UserProfileDto MapToDto(UserProfile profile)
    {
        return new UserProfileDto(
            profile.Id,
            profile.FirstName,
            profile.LastName,
            profile.AvatarUrl,
            profile.Bio,
            profile.City,
            profile.Rating,
            profile.ReviewCount,
            new EcoStatsDto(
                profile.EcoStats.ItemsGifted,
                profile.EcoStats.ItemsReceived,
                profile.EcoStats.Co2SavedKg,
                profile.EcoStats.WasteSavedKg),
            profile.CreatedAt);
    }
}