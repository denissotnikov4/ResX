using MediatR;
using ResX.Identity.Application.DTOs;
using ResX.Identity.Domain.Enums;

namespace ResX.Identity.Application.Commands.RegisterUser;

public record RegisterUserCommand(
    string Email,
    string? Phone,
    string Password,
    string FirstName,
    string LastName,
    UserRole Role = UserRole.Donor) : IRequest<TokensDto>;
