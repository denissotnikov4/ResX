using MediatR;

namespace ResX.Disputes.Application.Commands.AddEvidence;

public record AddEvidenceCommand(
    Guid DisputeId,
    Guid SubmittedBy,
    string Description,
    IEnumerable<string>? FileUrls) : IRequest<Guid>;