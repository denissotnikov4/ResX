using Grpc.Core;
using MediatR;
using ResX.Identity.Application.Queries.GetUserById;
using ResX.Identity.Application.Services;

namespace ResX.Identity.API.Grpc;

public class IdentityGrpcService : IdentityService.IdentityServiceBase
{
    private readonly ITokenService _tokenService;
    private readonly IMediator _mediator;
    private readonly ILogger<IdentityGrpcService> _logger;

    public IdentityGrpcService(
        ITokenService tokenService,
        IMediator mediator,
        ILogger<IdentityGrpcService> logger)
    {
        _tokenService = tokenService;
        _mediator = mediator;
        _logger = logger;
    }

    public override Task<ValidateTokenResponse> ValidateToken(
        ValidateTokenRequest request,
        ServerCallContext context)
    {
        try
        {
            var principal = _tokenService.GetPrincipalFromToken(request.Token);
            var userId = principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";
            var email = principal.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? "";
            var role = principal.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "";

            return Task.FromResult(new ValidateTokenResponse
            {
                IsValid = true,
                UserId = userId,
                Email = email,
                Role = role
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Token validation failed.");
            return Task.FromResult(new ValidateTokenResponse { IsValid = false });
        }
    }

    public override async Task<GetUserClaimsResponse> GetUserClaims(
        GetUserClaimsRequest request,
        ServerCallContext context)
    {
        if (!Guid.TryParse(request.UserId, out var userId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid user ID."));
        }

        try
        {
            var userDto = await _mediator.Send(
                new GetUserByIdQuery(userId),
                context.CancellationToken);

            var response = new GetUserClaimsResponse
            {
                UserId = userDto.Id.ToString(),
                Email = userDto.Email,
                FirstName = userDto.FirstName,
                LastName = userDto.LastName,
                Role = userDto.Role.ToString(),
                IsActive = userDto.IsActive
            };

            return response;
        }
        catch (Common.Exceptions.NotFoundException)
        {
            throw new RpcException(new Status(StatusCode.NotFound, $"User {request.UserId} not found."));
        }
    }
}
