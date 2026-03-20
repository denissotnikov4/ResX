using MediatR;
using ResX.Files.Application.DTOs;

namespace ResX.Files.Application.Commands.UploadFile;

public record UploadFileCommand(
    Stream FileStream,
    string FileName,
    string ContentType,
    long SizeBytes,
    Guid UploadedBy) : IRequest<FileRecordDto>;