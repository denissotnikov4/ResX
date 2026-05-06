using FluentValidation;

namespace ResX.Listings.Application.Commands.CreateCategory;

public class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Category name is required.")
            .MaximumLength(100).WithMessage("Category name cannot exceed 100 characters.");
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.IconUrl).MaximumLength(500);
        RuleFor(x => x.DisplayOrder)
            .GreaterThanOrEqualTo(0).WithMessage("Display order cannot be negative.");
        RuleFor(x => x.Co2SavedPer100GramsG)
            .GreaterThanOrEqualTo(0).WithMessage("CO2 rate cannot be negative.");
        RuleFor(x => x.WasteSavedPer100GramsG)
            .GreaterThanOrEqualTo(0).WithMessage("Waste rate cannot be negative.");
    }
}
