using Aurum.Application.Common.Models;
using Aurum.Application.Features.Auth.Login;

namespace Aurum.Application.Common.Interfaces
{
    public interface IIdentityService
    {
        Task<Result<string>> RegisterUserAsync(string email, string password);
        Task<Result<LoginUserDto>> LoginUserAsync(string email, string password);
    }
}
