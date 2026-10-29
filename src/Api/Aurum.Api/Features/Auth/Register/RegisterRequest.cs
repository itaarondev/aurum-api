using Aurum.Domain.Enums;

namespace Aurum.Api.Features.Auth.Register
{
    public record RegisterRequest(string Email, string Password, string ConfirmPassword, UserRole Role);
}
