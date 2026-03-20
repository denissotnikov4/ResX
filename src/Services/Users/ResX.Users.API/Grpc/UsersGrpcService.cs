using Grpc.Core;
using MediatR;
using ResX.Users.Application.Commands.UpdateEcoStats;
using ResX.Users.Application.Queries.GetUserProfile;

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
