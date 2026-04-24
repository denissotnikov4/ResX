using ResX.Listings.Application.DTOs;
using ResX.Listings.Application.Services;
using ResX.Users.API.Grpc;

namespace ResX.Listings.Infrastructure.Grpc;

public class UsersGrpcClient : IUsersClient
{
    private readonly UsersService.UsersServiceClient _client;

    public UsersGrpcClient(UsersService.UsersServiceClient client)
    {
        _client = client;
    }

    public async Task<IReadOnlyDictionary<Guid, DonorDto>> GetDonorsAsync(
        IReadOnlyCollection<Guid> userIds,
        CancellationToken cancellationToken = default)
    {
        if (userIds.Count == 0)
            return new Dictionary<Guid, DonorDto>();

        var request = new GetUserProfilesBatchRequest();
        foreach (var id in userIds.Distinct())
            request.UserIds.Add(id.ToString());

        var response = await _client.GetUserProfilesBatchAsync(request, cancellationToken: cancellationToken);

        var result = new Dictionary<Guid, DonorDto>(response.Profiles.Count);
        foreach (var p in response.Profiles)
        {
            if (!Guid.TryParse(p.UserId, out var id))
                continue;

            result[id] = new DonorDto(
                id,
                p.FirstName,
                p.LastName,
                string.IsNullOrEmpty(p.AvatarUrl) ? null : p.AvatarUrl,
                (decimal)p.Rating,
                p.ReviewCount);
        }

        return result;
    }
}