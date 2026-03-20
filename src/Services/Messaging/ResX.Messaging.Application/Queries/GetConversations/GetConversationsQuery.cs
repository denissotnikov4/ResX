using MediatR;
using ResX.Common.Models;
using ResX.Messaging.Application.DTOs;

namespace ResX.Messaging.Application.Queries.GetConversations;

public record GetConversationsQuery(
    Guid UserId,
    int PageNumber = 1,
    int PageSize = 20) : IRequest<PagedList<ConversationDto>>;