using Aurum.Application.Common.Interfaces;
using Aurum.Application.Common.Models;
using Aurum.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace Aurum.Persistence.Identity
{
    public class IdentityService : IIdentityService
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly IRefreshTokenService _refreshTokenService;

        public IdentityService(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager, IJwtTokenService jwtTokenService, IRefreshTokenService refreshTokenService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _jwtTokenService = jwtTokenService;
            _refreshTokenService = refreshTokenService;
        }

        public async Task<Result<UserInfoDto>> GetUserByIdAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return Result<UserInfoDto>.Failure(new[] { "El usuario asociado al token ya no existe." });
            }

            var roles = await _userManager.GetRolesAsync(user);

            return Result<UserInfoDto>.Success(new UserInfoDto
            {
                Id = user.Id,
                Email = user.Email!,
                Roles = roles
            });
        }

        public async Task<Result<string>> RegisterUserAsync(string email, string password, UserRole role)
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

            // Convertimos el Enum numérico a texto (ej: UserRole.Admin -> "Admin")
            string roleName = role.ToString();

            // Aseguramos que el rol exista en la base de datos de Identity
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                await _roleManager.CreateAsync(new IdentityRole(roleName));
            }

            // Asignamos el rol correspondiente
            await _userManager.AddToRoleAsync(user, roleName);

            return Result<string>.Success(user.Email);
        }

        public async Task<Result<LoginUserDto>> LoginUserAsync(string email, string password)
        {
            var user = await _userManager.FindByEmailAsync(email);

            if (user == null || !await _userManager.CheckPasswordAsync(user, password))
            {
                return Result<LoginUserDto>.Failure(new[] { "Credenciales inválidas." });
            }

            // Obtenemos los roles reales del usuario desde Identity
            var roles = await _userManager.GetRolesAsync(user);

            // Generamos el Access Token con los claims, el email, id y roles
            var accessToken = _jwtTokenService.GenerateAccessToken(user.Id, user.Email!, roles);

            // Generamos el Refresh Token criptográficamente seguro
            var refreshToken = _jwtTokenService.GenerateRefreshToken();

            // Guardamos el Refresh Token en la base de datos
            await _refreshTokenService.SaveTokenRefreshAsync(user.Id, refreshToken, CancellationToken.None);

            return Result<LoginUserDto>.Success(new LoginUserDto
            {
                Id = user.Id,
                Email = user.Email!,
                AccessToken = accessToken,
                RefreshToken = refreshToken
            });
        }

        public async Task<Result<UserInfoDto>> GetOrCreateExternalUserAsync(string email, string name, string provider)
        {
            var user = await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                // Si no existe, creamos el usuario sin contraseña
                user = new IdentityUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true // Al venir de Google u otro proveedor confiable, se considera verificado
                };

                var createResult = await _userManager.CreateAsync(user);
                if (!createResult.Succeeded)
                {
                    return Result<UserInfoDto>.Failure(createResult.Errors.Select(e => e.Description));
                }

                // Asignar rol por defecto (ej. "User")
                await _userManager.AddToRoleAsync(user, "User");
            }

            var roles = await _userManager.GetRolesAsync(user);

            var userDto = new UserInfoDto
            {
                Id = user.Id,
                Email = user.Email!,
                Roles = roles.ToList()
            };

            return Result<UserInfoDto>.Success(userDto);
        }
    }
}
