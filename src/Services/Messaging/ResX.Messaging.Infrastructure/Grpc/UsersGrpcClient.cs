using ResX.Messaging.Application.DTOs;
using ResX.Messaging.Application.Services;
using ResX.Users.API.Grpc;

namespace ResX.Messaging.Infrastructure.Grpc;

public class UsersGrpcClient : IUsersClient
{
    private readonly UsersService.UsersServiceClient _client;

    public UsersGrpcClient(UsersService.UsersServiceClient client)
    {
        _client = client;
    }

    public async Task<IReadOnlyDictionary<Guid, ParticipantSummaryDto>> GetUserSummariesAsync(
        IReadOnlyCollection<Guid> userIds,
        CancellationToken cancellationToken = default)
    {
        if (userIds.Count == 0)
            return new Dictionary<Guid, ParticipantSummaryDto>();

        var request = new GetUserProfilesBatchRequest();
        foreach (var id in userIds.Distinct())
            request.UserIds.Add(id.ToString());

        var response = await _client.GetUserProfilesBatchAsync(request, cancellationToken: cancellationToken);

        var result = new Dictionary<Guid, ParticipantSummaryDto>(response.Profiles.Count);
        foreach (var p in response.Profiles)
        {
            if (!Guid.TryParse(p.UserId, out var id))
                continue;

            result[id] = new ParticipantSummaryDto(
                id,
                p.FirstName,
                p.LastName,
                string.IsNullOrEmpty(p.AvatarUrl) ? null : p.AvatarUrl);
        }

        return result;
    }
}
