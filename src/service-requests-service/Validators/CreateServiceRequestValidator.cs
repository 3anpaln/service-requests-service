using FluentValidation;
using service_requests_service.Models;

namespace service_requests_service.Validators;

public class CreateServiceRequestValidator : AbstractValidator<CreateServiceRequestRequest>
{
    public CreateServiceRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Description)
            .MaximumLength(2000);

        RuleFor(x => x.Priority)
            .IsInEnum();
    }
}