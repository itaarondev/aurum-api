using Aurum.Application.Common.Interfaces;
using Aurum.Application.Common.Models;
using Aurum.Application.Features.Auth.Login;
using Microsoft.AspNetCore.Identity;

namespace Aurum.Persistence.Identity
{
    public class IdentityService : IIdentityService
    {
        private readonly UserManager<IdentityUser> _userManager;

        public IdentityService(UserManager<IdentityUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<Result<string>> RegisterUserAsync(string email, string password)
        {
            var user = new IdentityUser
            {
                UserName = email,
                Email = email
            };

            var result = await _userManager.CreateAsync(user, password);

            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description);
                return Result<string>.Failure(errors);
            }

            return Result<string>.Success(user.Email);
        }

        public async Task<Result<LoginUserDto>> LoginUserAsync(string email, string password)
        {
            var user = await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                return Result<LoginUserDto>.Failure(new[] { "Credenciales inválidas." });
            }

            var isPasswordValid = await _userManager.CheckPasswordAsync(user, password);

            if (!isPasswordValid)
            {
                return Result<LoginUserDto>.Failure(new[] { "Credenciales inválidas." });
            }

            return Result<LoginUserDto>.Success(new LoginUserDto
            {
                Id = user.Id,
                Email = user.Email!
            });
        }
    }
}
