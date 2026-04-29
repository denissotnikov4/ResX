using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Logging;
using ResX.Common.Caching;
using ResX.Common.Exceptions;
using ResX.Common.Persistence;
using ResX.Listings.Application.Repositories;
using ResX.Listings.Domain.AggregateRoots;
using ResX.Listings.Domain.Entities;

namespace ResX.Listings.Application.Commands.UpdateCategory;

public class UpdateCategoryCommandHandler : IRequestHandler<UpdateCategoryCommand, Unit>
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cache;
    private readonly ILogger<UpdateCategoryCommandHandler> _logger;

    public UpdateCategoryCommandHandler(
        ICategoryRepository categoryRepository,
        IUnitOfWork unitOfWork,
        ICacheService cache,
        ILogger<UpdateCategoryCommandHandler> logger)
    {
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
        _cache = cache;
        _logger = logger;
    }

    public async Task<Unit> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await _categoryRepository.GetByIdAsync(request.CategoryId, cancellationToken)
            ?? throw new NotFoundException(nameof(Category), request.CategoryId);

        if (request.ParentCategoryId.HasValue)
        {
            var parent = await _categoryRepository.GetByIdAsync(request.ParentCategoryId.Value, cancellationToken)
                ?? throw new NotFoundException("ParentCategory", request.ParentCategoryId.Value);

            if (!parent.IsActive)
                throw new DomainException("Parent category is not active.");
        }

        var oldValues = SerializeSnapshot(category);

        category.Update(
            request.Name,
            request.Description,
            request.ParentCategoryId,
            request.IconUrl,
            request.DisplayOrder);

        var newValues = SerializeSnapshot(category);

        var history = CategoryHistory.Create(
            category.Id,
            request.RequestingUserId,
            CategoryChangeType.Updated,
            oldValues,
            newValues);

        await _categoryRepository.AddHistoryAsync(history, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await BumpVersionsAsync(cancellationToken);

        _logger.LogInformation("Category {CategoryId} updated by {UserId}.", category.Id, request.RequestingUserId);
        return Unit.Value;
    }

    private async Task BumpVersionsAsync(CancellationToken cancellationToken)
    {
        var catVersion = await _cache.GetAsync<int>("categories:version", cancellationToken);
        await _cache.SetAsync("categories:version", catVersion + 1, TimeSpan.FromDays(365), cancellationToken);

        var listVersion = await _cache.GetAsync<int>("listings:version", cancellationToken);
        await _cache.SetAsync("listings:version", listVersion + 1, TimeSpan.FromDays(365), cancellationToken);
    }

    private static string SerializeSnapshot(Category category) => JsonSerializer.Serialize(new
    {
        category.Name,
        category.Description,
        category.ParentCategoryId,
        category.IconUrl,
        category.IsActive,
        category.DisplayOrder
    });
}
