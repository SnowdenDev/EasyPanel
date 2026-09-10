using FluentValidation;

namespace EasyPanel.Backend.Features.Users.CreateUser;

public sealed class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(request => request.Email).NotEmpty().EmailAddress();
        RuleFor(request => request.DisplayName).NotEmpty().MaximumLength(200);
        RuleFor(request => request.Role).IsInEnum();
        RuleFor(request => request.Password).MinimumLength(12);
    }
}
