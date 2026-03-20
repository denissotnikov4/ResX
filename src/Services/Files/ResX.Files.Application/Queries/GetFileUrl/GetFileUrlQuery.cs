using MediatR;

namespace ResX.Files.Application.Queries.GetFileUrl;

public record GetFileUrlQuery(Guid FileId) : IRequest<string>;