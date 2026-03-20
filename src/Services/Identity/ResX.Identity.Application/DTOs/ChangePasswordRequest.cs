namespace ResX.Identity.Application.DTOs;

public record ChangePasswordRequest(string OldPassword, string NewPassword);