using MediatR;
using Microsoft.Extensions.Logging;
using ResX.Common.Persistence;
using ResX.Users.Application.Repositories;
using ResX.Users.Domain.Aggregates;

namespace ResX.Users.Application.Commands.CreateUserProfile;

public class CreateUserProfileCommandHandler : IRequestHandler<CreateUserProfileCommand, Unit>
{
    private readonly ILogger<CreateUserProfileCommandHandler> _logger;
    private readonly IUserProfileRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateUserProfileCommandHandler(
        IUserProfileRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<CreateUserProfileCommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Unit> Handle(CreateUserProfileCommand request, CancellationToken cancellationToken)
    {
        var profile = UserProfile.Create(request.UserId, request.FirstName, request.LastName, request.City);

        await _repository.AddAsync(profile, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("UserProfile created for user {UserId}.", request.UserId);

        return Unit.Value;
    }
}