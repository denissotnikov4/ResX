namespace ResX.Identity.Application.DTOs;

public record TokensDto(
    string AccessToken,
    string RefreshToken);
