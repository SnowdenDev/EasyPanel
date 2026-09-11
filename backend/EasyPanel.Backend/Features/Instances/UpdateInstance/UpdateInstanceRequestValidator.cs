using System.Text.RegularExpressions;
using FluentValidation;

namespace EasyPanel.Backend.Features.Instances.UpdateInstance;

public sealed class UpdateInstanceRequestValidator : AbstractValidator<UpdateInstanceRequest>
{
    // Same reasoning as CreateInstanceRequestValidator's identical constant: WorkDirectory
    // describes a path on the daemon's machine (always Windows), not on whatever OS the
    // backend itself happens to run on — see that validator's comment for the full story
    // of why Path.IsPathRooted is the wrong check here.
    private static readonly Regex WindowsAbsolutePath = new(@"^([A-Za-z]:\\|\\\\)", RegexOptions.Compiled);

    public UpdateInstanceRequestValidator()
    {
        RuleFor(request => request.DisplayName).NotEmpty().MaximumLength(200);

        RuleFor(request => request.WorkDirectory)
            .NotEmpty()
            .Must(path => WindowsAbsolutePath.IsMatch(path))
            .WithMessage(@"Work directory must be an absolute Windows path (e.g. C:\Servers\myserver or \\server\share\...).");

        RuleFor(request => request.ExecutableRelativePath)
            .NotEmpty()
            .Must(path => !WindowsAbsolutePath.IsMatch(path) && !path.StartsWith('\\') && !path.StartsWith('/') && !path.Split('/', '\\').Contains(".."))
            .WithMessage("Executable path must be relative to the work directory and cannot contain '..'.");

        RuleFor(request => request.ExpectedExecutableSha256)
            .NotEmpty()
            .Matches("^[0-9A-Fa-f]{64}$")
            .WithMessage("Expected SHA256 must be exactly 64 hex characters.");

        RuleFor(request => request.CpuLimitPercent).InclusiveBetween(1, 100).When(request => request.CpuLimitPercent.HasValue);
        RuleFor(request => request.MemoryLimitMegabytes).GreaterThan(0).When(request => request.MemoryLimitMegabytes.HasValue);
    }
}
