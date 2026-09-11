using FluentValidation;

namespace EasyPanel.Backend.Features.Nodes.UpdateNode;

public sealed class UpdateNodeRequestValidator : AbstractValidator<UpdateNodeRequest>
{
    public UpdateNodeRequestValidator()
    {
        RuleFor(request => request.DisplayName).NotEmpty().MaximumLength(200);
        RuleFor(request => request.ConnectivityMode).IsInEnum();
    }
}
