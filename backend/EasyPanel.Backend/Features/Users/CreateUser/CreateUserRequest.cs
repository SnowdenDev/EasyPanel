using EasyPanel.Backend.Infrastructure.Entities;

namespace EasyPanel.Backend.Features.Users.CreateUser;

public sealed record CreateUserRequest(string Email, string DisplayName, UserRole Role, string Password);
