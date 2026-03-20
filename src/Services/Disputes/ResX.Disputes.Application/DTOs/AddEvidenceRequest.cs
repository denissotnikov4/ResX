namespace ResX.Disputes.Application.DTOs;

public record AddEvidenceRequest(
    string Description,
    IEnumerable<string>? FileUrls);