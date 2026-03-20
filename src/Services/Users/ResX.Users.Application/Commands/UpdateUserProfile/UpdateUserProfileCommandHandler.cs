using MediatR;
using ResX.Common.Caching;
using ResX.Common.Exceptions;
using ResX.Common.Persistence;
using ResX.Users.Application.Repositories;
using ResX.Users.Domain.Aggregates;

namespace ResX.Users.Application.Commands.UpdateUserProfile;

public class UpdateUserProfileCommandHandler : IRequestHandler<UpdateUserProfileCommand, Unit>
{
    private readonly IUserProfileRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cache;

    public UpdateUserProfileCommandHandler(IUserProfileRepository repository, IUnitOfWork unitOfWork, ICacheService cache)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _cache = cache;
    }

    public async Task<Unit> Handle(UpdateUserProfileCommand request, CancellationToken cancellationToken)
    {
        var profile = await _repository.GetByIdAsync(request.UserId, cancellationToken)
                      ?? throw new NotFoundException(nameof(UserProfile), request.UserId);

        profile.Update(request.FirstName, request.LastName, request.Bio, request.City);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _cache.RemoveAsync($"users:profile:{request.UserId}", cancellationToken);

        return Unit.Value;
    }
}