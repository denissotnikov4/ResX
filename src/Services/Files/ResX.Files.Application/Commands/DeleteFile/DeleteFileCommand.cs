using MediatR;

namespace ResX.Files.Application.Commands.DeleteFile;

public record DeleteFileCommand(
    Guid FileId,
    Guid RequestingUserId) : IRequest<Unit>;