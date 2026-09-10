using FluentValidation;

namespace EasyPanel.Backend.Features.Instances.CreateInstance;

public sealed class CreateInstanceRequestValidator : AbstractValidator<CreateInstanceRequest>
{
    public CreateInstanceRequestValidator()
    {
        RuleFor(request => request.NodeId).NotEmpty();
        RuleFor(request => request.DisplayName).NotEmpty().MaximumLength(200);

        RuleFor(request => request.WorkDirectory)
            .NotEmpty()
            .Must(path => Path.IsPathRooted(path))
            .WithMessage("Work directory must be an absolute path.");

        RuleFor(request => request.ExecutableRelativePath)
            .NotEmpty()
            .Must(path => !Path.IsPathRooted(path) && !path.Split('/', '\\').Contains(".."))
            .WithMessage("Executable path must be relative to the work directory and cannot contain '..'.");

        RuleFor(request => request.ExpectedExecutableSha256)
            .NotEmpty()
            .Matches("^[0-9A-Fa-f]{64}$")
            .WithMessage("Expected SHA256 must be exactly 64 hex characters.");

        RuleFor(request => request.CpuLimitPercent).InclusiveBetween(1, 100).When(request => request.CpuLimitPercent.HasValue);
        RuleFor(request => request.MemoryLimitMegabytes).GreaterThan(0).When(request => request.MemoryLimitMegabytes.HasValue);
    }
}
