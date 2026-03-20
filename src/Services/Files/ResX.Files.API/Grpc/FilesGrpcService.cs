using Grpc.Core;
using MediatR;
using ResX.Files.Application.Commands.DeleteFile;
using ResX.Files.Application.Queries.GetFileUrl;

namespace ResX.Files.API.Grpc;

public class FilesGrpcService : FilesService.FilesServiceBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<FilesGrpcService> _logger;

    public FilesGrpcService(IMediator mediator, ILogger<FilesGrpcService> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public override async Task<GetFileUrlResponse> GetFileUrl(GetFileUrlRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.FileId, out var fileId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid file ID."));
        }

        try
        {
            var url = await _mediator.Send(new GetFileUrlQuery(fileId), context.CancellationToken);
            return new GetFileUrlResponse { Url = url };
        }
        catch (Common.Exceptions.NotFoundException)
        {
            throw new RpcException(new Status(StatusCode.NotFound, $"File {request.FileId} not found."));
        }
    }

    public override async Task<DeleteFileResponse> DeleteFile(DeleteFileRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.FileId, out var fileId))
            return new DeleteFileResponse { Success = false };

        try
        {
            await _mediator.Send(new DeleteFileCommand(fileId, Guid.Empty), context.CancellationToken);
            return new DeleteFileResponse { Success = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete file {FileId}", fileId);
            return new DeleteFileResponse { Success = false };
        }
    }
}
