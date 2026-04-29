using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Logging;
using ResX.Common.Caching;
using ResX.Common.Persistence;
using ResX.Listings.Application.Repositories;
using ResX.Listings.Domain.AggregateRoots;
using ResX.Listings.Domain.Entities;

namespace ResX.Listings.Application.Commands.CreateCategory;

public class CreateCategoryCommandHandler : IRequestHandler<CreateCategoryCommand, Guid>
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cache;
    private readonly ILogger<CreateCategoryCommandHandler> _logger;

    public CreateCategoryCommandHandler(
        ICategoryRepository categoryRepository,
        IUnitOfWork unitOfWork,
        ICacheService cache,
        ILogger<CreateCategoryCommandHandler> logger)
    {
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
        _cache = cache;
        _logger = logger;
    }

    public async Task<Guid> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = Category.Create(
            request.Name,
            request.Description,
            request.ParentCategoryId,
            request.IconUrl,
            request.DisplayOrder);

        await _categoryRepository.AddAsync(category, cancellationToken);

        var newValues = JsonSerializer.Serialize(new
        {
            category.Name,
            category.Description,
            category.ParentCategoryId,
            category.IconUrl,
            category.IsActive,
            category.DisplayOrder
        });

        var history = CategoryHistory.Create(
            category.Id,
            request.RequestingUserId,
            CategoryChangeType.Created,
            oldValuesJson: null,
            newValuesJson: newValues);

        await _categoryRepository.AddHistoryAsync(history, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await BumpCategoriesVersionAsync(cancellationToken);

        _logger.LogInformation("Category {CategoryId} created by {UserId}.", category.Id, request.RequestingUserId);
        return category.Id;
    }

    private async Task BumpCategoriesVersionAsync(CancellationToken cancellationToken)
    {
        var version = await _cache.GetAsync<int>("categories:version", cancellationToken);
        await _cache.SetAsync("categories:version", version + 1, TimeSpan.FromDays(365), cancellationToken);
    }
}
