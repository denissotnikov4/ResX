using MediatR;
using ResX.Common.Exceptions;
using ResX.Identity.Application.DTOs;
using ResX.Identity.Application.Repositories;
using ResX.Identity.Domain.AggregateRoots;

namespace ResX.Identity.Application.Queries.GetUserById;

public class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, UserDto>
{
    private readonly IUserRepository _userRepository;

    public GetUserByIdQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<UserDto> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(User), request.UserId);

        return new UserDto(
            user.Id,
            user.Email.Value,
            user.Phone?.Value,
            user.FirstName,
            user.LastName,
            user.Role,
            user.IsActive,
            user.CreatedAt);
    }
}
