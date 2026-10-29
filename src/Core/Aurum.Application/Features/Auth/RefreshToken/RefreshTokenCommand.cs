using Aurum.Application.Common.Interfaces;
using Aurum.Application.Common.Models;
using Aurum.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Aurum.Application.Features.Auth.RefreshToken
{
    public record RefreshTokenCommand(string RefreshToken) : IRequest<Result<RefreshTokenResponseDto>>;

    public class RefreshTokenResponseDto
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
    }

    public class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
    {
        public RefreshTokenCommandValidator()
        {
            RuleFor(x => x.RefreshToken)
                .NotEmpty().WithMessage("El Refresh Token es requerido.");
        }
    }

    public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<RefreshTokenResponseDto>>
    {
        private readonly IRefreshTokenService _tokenService;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly IIdentityService _identityService;
        private readonly ILogger<RefreshTokenCommandHandler> _logger;

        public RefreshTokenCommandHandler(
            IRefreshTokenService tokenService,
            IJwtTokenService jwtTokenService,
            IIdentityService identityService,
            ILogger<RefreshTokenCommandHandler> logger)
        {
            _tokenService = tokenService;
            _jwtTokenService = jwtTokenService;
            _identityService = identityService;
            _logger = logger;
        }

        public async Task<Result<RefreshTokenResponseDto>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
        {
            // 1. Buscar el Refresh Token en la base de datos
            var existingToken = await _tokenService.GetByTokenAsync(request.RefreshToken, cancellationToken);

            // 2. Validar si existe o si ya expiró de manera normal
            if (existingToken == null || existingToken.IsExpired)
            {
                return Result<RefreshTokenResponseDto>.Failure(new[] { "El refresh token es inválido o ha expirado." });
            }

            // 3. ¡ALERTA ROJA DE SEGURIDAD! 
            // Si el token ya fue marcado como revocado pero lo vuelven a presentar, 
            // significa que alguien más interceptó el token anterior y está intentando suplantar al usuario.
            if (existingToken.IsRevoked)
            {
                _logger.LogCritical(
                    "¡ALERTA DE SEGURIDAD CRÍTICA! Intento de reutilización de un Refresh Token ya revocado. UserId: {UserId}, TokenId: {TokenId}. Posible ataque de robo de identidad (Token Reuse Detected).",
                    existingToken.UserId, existingToken.Id);

                // ACCIÓN DE DEFENSA: Revocamos TODOS los tokens activos de este usuario en la BD de inmediato
                await _tokenService.RevokeAllTokensForUserAsync(existingToken.UserId, cancellationToken);

                return Result<RefreshTokenResponseDto>.Failure(new[] {
                    "Se ha detectado una anomalía de seguridad. Su sesión ha sido invalidada por completo. Por favor, vuelva a iniciar sesión."
                });
            }

            // 4. Obtener la información del usuario
            var userResult = await _identityService.GetUserByIdAsync(existingToken.UserId);
            if (!userResult.Succeeded || userResult.Data == null)
            {
                _logger.LogWarning("Intento de refresco para un usuario inexistente. UserId: {UserId}", existingToken.UserId);
                return Result<RefreshTokenResponseDto>.Failure(userResult.Errors);
            }

            var userInfo = userResult.Data;

            // 5. Aplicar rotación normal: Revocar el token actual que se está consumiendo
            existingToken.IsRevoked = true;
            await _tokenService.UpdateAsync(existingToken, cancellationToken);
            // _logger.LogInformation("Refresh token rotado exitosamente para el usuario: {Email}", userInfo.Email);

            // 6. Generar el nuevo par de tokens
            var newAccessToken = _jwtTokenService.GenerateAccessToken(userInfo.Id, userInfo.Email, userInfo.Roles);
            var newRefreshTokenString = _jwtTokenService.GenerateRefreshToken();

            // 7. Guardar el nuevo Refresh Token
            await _tokenService.SaveTokenRefreshAsync(userInfo.Id, newRefreshTokenString, CancellationToken.None);

            // 8. Retornar la respuesta exitosa
            return Result<RefreshTokenResponseDto>.Success(new RefreshTokenResponseDto
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshTokenString
            });
        }
    }
}
