using MediatR;

namespace ResX.Users.Application.Commands.UpdateEcoStats;

public record UpdateEcoStatsCommand(
    Guid UserId,
    int ItemsGiftedDelta,
    int ItemsReceivedDelta,
    decimal Co2Delta,
    decimal WasteDelta) : IRequest<Unit>;