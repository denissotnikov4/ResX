using MediatR;
using ResX.Common.Exceptions;
using ResX.Common.Persistence;
using ResX.Disputes.Application.Repositories;
using ResX.Disputes.Domain.AggregateRoots;

namespace ResX.Disputes.Application.Commands.AddEvidence;

public class AddEvidenceCommandHandler : IRequestHandler<AddEvidenceCommand, Guid>
{
    private readonly IDisputeRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public AddEvidenceCommandHandler(IDisputeRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(AddEvidenceCommand request, CancellationToken cancellationToken)
    {
        var dispute = await _repository.GetByIdAsync(request.DisputeId, cancellationToken)
                      ?? throw new NotFoundException(nameof(Dispute), request.DisputeId);

        var evidence = dispute.AddEvidence(request.SubmittedBy, request.Description, request.FileUrls);

        await _repository.AddEvidenceAsync(evidence, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return evidence.Id;
    }
}