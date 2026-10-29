using Aurum.Application.Common.Models;
using Aurum.Domain.Enums;

namespace Aurum.Application.Common.Interfaces
{
    public interface IIdentityService
    {
        Task<Result<string>> RegisterUserAsync(string email, string password, UserRole role);
        Task<Result<LoginUserDto>> LoginUserAsync(string email, string password);
        Task<Result<UserInfoDto>> GetUserByIdAsync(string userId);
        Task<Result<UserInfoDto>> GetOrCreateExternalUserAsync(string email, string name, string provider);
    }
}
