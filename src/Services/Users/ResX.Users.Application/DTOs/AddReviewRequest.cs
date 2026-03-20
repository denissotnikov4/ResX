namespace ResX.Users.Application.DTOs;

public record AddReviewRequest(
    string ReviewerName,
    int Rating,
    string Comment);