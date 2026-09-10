using FluentValidation;

namespace EasyPanel.Backend.Features.Nodes.RegisterNode;

public sealed class RegisterNodeRequestValidator : AbstractValidator<RegisterNodeRequest>
{
    public RegisterNodeRequestValidator()
    {
        RuleFor(request => request.DisplayName).NotEmpty().MaximumLength(200);
        RuleFor(request => request.ConnectivityMode).IsInEnum();
    }
}
