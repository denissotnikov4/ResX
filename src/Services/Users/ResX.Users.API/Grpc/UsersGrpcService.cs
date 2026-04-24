using Grpc.Core;
using MediatR;
using ResX.Users.Application.Commands.UpdateEcoStats;
using ResX.Users.Application.Queries.GetUserProfile;
using ResX.Users.Application.Queries.GetUserProfilesBatch;

namespace ResX.Users.API.Grpc;

public class UsersGrpcService : UsersService.UsersServiceBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<UsersGrpcService> _logger;

    public UsersGrpcService(IMediator mediator, ILogger<UsersGrpcService> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public override async Task<GetUserProfileResponse> GetUserProfile(
        GetUserProfileRequest request,
        ServerCallContext context)
    {
        if (!Guid.TryParse(request.UserId, out var userId))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid user ID."));

        var profile = await _mediator.Send(new GetUserProfileQuery(userId), context.CancellationToken);

        return new GetUserProfileResponse
        {
            UserId = profile.Id.ToString(),
            FirstName = profile.FirstName,
            LastName = profile.LastName,
            AvatarUrl = profile.AvatarUrl ?? "",
            City = profile.City ?? "",
            Rating = (double)profile.Rating,
            ReviewCount = profile.ReviewCount,
            EcoStats = new EcoStatsProto
            {
                ItemsGifted = profile.EcoStats.ItemsGifted,
                ItemsReceived = profile.EcoStats.ItemsReceived,
                Co2SavedKg = (double)profile.EcoStats.Co2SavedKg,
                WasteSavedKg = (double)profile.EcoStats.WasteSavedKg
            }
        };
    }

    public override async Task<GetUserProfilesBatchResponse> GetUserProfilesBatch(
        GetUserProfilesBatchRequest request,
        ServerCallContext context)
    {
        var ids = new List<Guid>(request.UserIds.Count);
        foreach (var raw in request.UserIds)
        {
            if (Guid.TryParse(raw, out var id))
                ids.Add(id);
        }

        var profiles = await _mediator.Send(
            new GetUserProfilesBatchQuery(ids),
            context.CancellationToken);

        var response = new GetUserProfilesBatchResponse();
        foreach (var p in profiles)
        {
            response.Profiles.Add(new UserProfileBrief
            {
                UserId = p.Id.ToString(),
                FirstName = p.FirstName,
                LastName = p.LastName,
                AvatarUrl = p.AvatarUrl ?? "",
                Rating = (double)p.Rating,
                ReviewCount = p.ReviewCount
            });
        }

        return response;
    }

    public override async Task<UpdateEcoStatsResponse> UpdateEcoStats(
        UpdateEcoStatsRequest request,
        ServerCallContext context)
    {
        if (!Guid.TryParse(request.UserId, out var userId))
        {
            return new UpdateEcoStatsResponse { Success = false };
        }

        await _mediator.Send(new UpdateEcoStatsCommand(
            userId,
            request.ItemsGiftedDelta,
            request.ItemsReceivedDelta,
            (decimal)request.Co2Delta,
            (decimal)request.WasteDelta), context.CancellationToken);

        return new UpdateEcoStatsResponse { Success = true };
    }
}
