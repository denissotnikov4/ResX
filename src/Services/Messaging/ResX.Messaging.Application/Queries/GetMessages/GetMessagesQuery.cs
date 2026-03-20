using MediatR;
using ResX.Common.Models;
using ResX.Messaging.Application.DTOs;

namespace ResX.Messaging.Application.Queries.GetMessages;

public record GetMessagesQuery(
    Guid ConversationId,
    Guid UserId,
    int PageNumber = 1,
    int PageSize = 50) : IRequest<PagedList<MessageDto>>;
