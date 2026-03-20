using MediatR;

namespace ResX.Users.Application.Commands.AddReview;

public record AddReviewCommand(
    Guid UserProfileId,
    Guid ReviewerId,
    string ReviewerName,
    int Rating,
    string Comment) : IRequest<Guid>;