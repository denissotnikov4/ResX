using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Logging;
using ResX.Common.Caching;
using ResX.Common.Exceptions;
using ResX.Common.Persistence;
using ResX.Listings.Application.Repositories;
using ResX.Listings.Domain.AggregateRoots;
using ResX.Listings.Domain.Entities;

namespace ResX.Listings.Application.Commands.DeactivateCategory;

public class DeactivateCategoryCommandHandler : IRequestHandler<DeactivateCategoryCommand, Unit>
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cache;
    private readonly ILogger<DeactivateCategoryCommandHandler> _logger;

    public DeactivateCategoryCommandHandler(
        ICategoryRepository categoryRepository,
        IUnitOfWork unitOfWork,
        ICacheService cache,
        ILogger<DeactivateCategoryCommandHandler> logger)
    {
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
        _cache = cache;
        _logger = logger;
    }

    public async Task<Unit> Handle(DeactivateCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await _categoryRepository.GetByIdAsync(request.CategoryId, cancellationToken)
            ?? throw new NotFoundException(nameof(Category), request.CategoryId);

        var oldValues = JsonSerializer.Serialize(new { category.IsActive });

        category.Deactivate();

        var newValues = JsonSerializer.Serialize(new { category.IsActive });

        var history = CategoryHistory.Create(
            category.Id,
            request.RequestingUserId,
            CategoryChangeType.Deactivated,
            oldValues,
            newValues);

        await _categoryRepository.AddHistoryAsync(history, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var catVersion = await _cache.GetAsync<int>("categories:version", cancellationToken);
        await _cache.SetAsync("categories:version", catVersion + 1, TimeSpan.FromDays(365), cancellationToken);

        _logger.LogInformation("Category {CategoryId} deactivated by {UserId}.", category.Id, request.RequestingUserId);
        return Unit.Value;
    }
}
