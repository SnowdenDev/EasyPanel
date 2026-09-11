using System.Text.RegularExpressions;
using FluentValidation;

namespace EasyPanel.Backend.Features.Instances.CreateInstance;

public sealed class CreateInstanceRequestValidator : AbstractValidator<CreateInstanceRequest>
{
    // Matches an absolute Windows path (a drive letter, e.g. "C:\...") or a UNC share
    // ("\\server\share\..."). This deliberately does NOT use Path.IsPathRooted — that
    // check is relative to the OS the *backend* happens to run on, but WorkDirectory
    // describes a path on the *daemon's* machine, which is always Windows (Job Objects
    // are a Windows-only feature — see architecture.md). The backend itself is plain
    // ASP.NET Core with no OS requirement and commonly runs in a Linux container, where
    // Path.IsPathRooted("C:\\Servers\\x") returns false and would reject every legitimate
    // work directory an admin could ever actually enter.
    private static readonly Regex WindowsAbsolutePath = new(@"^([A-Za-z]:\\|\\\\)", RegexOptions.Compiled);

    public CreateInstanceRequestValidator()
    {
        RuleFor(request => request.NodeId).NotEmpty();
        RuleFor(request => request.DisplayName).NotEmpty().MaximumLength(200);

        RuleFor(request => request.WorkDirectory)
            .NotEmpty()
            .Must(path => WindowsAbsolutePath.IsMatch(path))
            .WithMessage(@"Work directory must be an absolute Windows path (e.g. C:\Servers\myserver or \\server\share\...).");

        // Same reasoning as WorkDirectory above: check against the Windows path shape
        // explicitly rather than Path.IsPathRooted, which on a Linux-hosted backend
        // returns false for "C:\Windows\System32\x.exe" — making that string look
        // "relative" and letting an absolute Windows path slip past this check entirely.
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
