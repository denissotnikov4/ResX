using MediatR;
using ResX.Common.Caching;
using ResX.Common.Exceptions;
using ResX.Common.Persistence;
using ResX.Users.Application.Repositories;
using ResX.Users.Domain.Aggregates;

namespace ResX.Users.Application.Commands.UpdateAvatar;

public class UpdateAvatarCommandHandler : IRequestHandler<UpdateAvatarCommand, Unit>
{
    private readonly IUserProfileRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cache;

    public UpdateAvatarCommandHandler(IUserProfileRepository repository, IUnitOfWork unitOfWork, ICacheService cache)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _cache = cache;
    }

    public async Task<Unit> Handle(UpdateAvatarCommand request, CancellationToken cancellationToken)
    {
        var profile = await _repository.GetByIdAsync(request.UserId, cancellationToken)
                      ?? throw new NotFoundException(nameof(UserProfile), request.UserId);

        profile.UpdateAvatar(request.AvatarUrl);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _cache.RemoveAsync($"users:profile:{request.UserId}", cancellationToken);

        return Unit.Value;
    }
}