using MediatR;
using ResX.Common.Caching;
using ResX.Common.Exceptions;
using ResX.Common.Persistence;
using ResX.Users.Application.Repositories;
using ResX.Users.Domain.Aggregates;

namespace ResX.Users.Application.Commands.AddReview;

public class AddReviewCommandHandler : IRequestHandler<AddReviewCommand, Guid>
{
    private readonly IUserProfileRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cache;

    public AddReviewCommandHandler(IUserProfileRepository repository, IUnitOfWork unitOfWork, ICacheService cache)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _cache = cache;
    }

    public async Task<Guid> Handle(AddReviewCommand request, CancellationToken cancellationToken)
    {
        var profile = await _repository.GetByIdAsync(request.UserProfileId, cancellationToken)
                      ?? throw new NotFoundException(nameof(UserProfile), request.UserProfileId);

        var review = profile.AddReview(request.ReviewerId, request.ReviewerName, request.Rating, request.Comment);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _cache.RemoveAsync($"users:profile:{request.UserProfileId}", cancellationToken);

        return review.Id;
    }
}