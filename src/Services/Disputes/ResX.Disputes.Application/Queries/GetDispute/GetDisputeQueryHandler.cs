using MediatR;
using ResX.Common.Exceptions;
using ResX.Disputes.Application.DTOs;
using ResX.Disputes.Application.Repositories;
using ResX.Disputes.Domain.AggregateRoots;

namespace ResX.Disputes.Application.Queries.GetDispute;

public class GetDisputeQueryHandler : IRequestHandler<GetDisputeQuery, DisputeDto>
{
    private readonly IDisputeRepository _repository;

    public GetDisputeQueryHandler(IDisputeRepository repository)
    {
        _repository = repository;
    }

    public async Task<DisputeDto> Handle(GetDisputeQuery request, CancellationToken cancellationToken)
    {
        var dispute = await _repository.GetByIdAsync(request.DisputeId, cancellationToken)
                      ?? throw new NotFoundException(nameof(Dispute), request.DisputeId);

        return MapToDto(dispute);
    }

    private static DisputeDto MapToDto(Dispute d) => new(
        d.Id,
        d.TransactionId,
        d.InitiatorId,
        d.RespondentId,
        d.Reason,
        d.Status.ToString(),
        d.Resolution,
        d.CreatedAt,
        d.ResolvedAt,
        d.Evidences.Select(e => new EvidenceDto(
            e.Id,
            e.SubmittedBy,
            e.Description,
            e.FileUrls.AsReadOnly(),
            e.SubmittedAt)).ToList().AsReadOnly());
}
