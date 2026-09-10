namespace EasyPanel.Backend.Features.Users.CreateUser;

public sealed record CreateUserResult(Guid? UserId, string? EmailAlreadyInUseError);
