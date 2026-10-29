using Aurum.Application.Common.Interfaces;
using Aurum.Application.Common.Models;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Aurum.Application.Features.Auth.ExternalLogin
{
    public record ExternalLoginCommand(string Email, string Name, string Provider) : IRequest<Result<LoginUserDto>>;

    public class ExternalLoginCommandValidator : AbstractValidator<ExternalLoginCommand>
    {
        public ExternalLoginCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("El correo es requerido.")
                .EmailAddress().WithMessage("El correo electrónico no tiene un formato válido.");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("El nombre es requerido.");

            RuleFor(x => x.Provider)
                .NotEmpty().WithMessage("El proveedor externo es requerido.");
        }
    }

    public class ExternalLoginCommandHandler : IRequestHandler<ExternalLoginCommand, Result<LoginUserDto>>
    {
        private readonly IIdentityService _identityService;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly IRefreshTokenService _refreshTokenService;
        private readonly ILogger<ExternalLoginCommandHandler> _logger;

        public ExternalLoginCommandHandler(
            IIdentityService identityService,
            IJwtTokenService jwtTokenService,
            IRefreshTokenService refreshTokenService,
            ILogger<ExternalLoginCommandHandler> logger)
        {
            _identityService = identityService;
            _jwtTokenService = jwtTokenService;
            _refreshTokenService = refreshTokenService;
            _logger = logger;
        }

        public async Task<Result<LoginUserDto>> Handle(ExternalLoginCommand request, CancellationToken cancellationToken)
        {
            // 1. Obtener o crear el usuario externo en Identity (si es su primera vez, se registra automáticamente)
            var userResult = await _identityService.GetOrCreateExternalUserAsync(request.Email, request.Name, request.Provider);

            if (!userResult.Succeeded || userResult.Data == null)
            {
                _logger.LogWarning("Falló el registro/obtención de usuario externo para el correo: {Email}", request.Email);
                return Result<LoginUserDto>.Failure(userResult.Errors);
            }

            var user = userResult.Data;

            // 2. Generar Access Token y Refresh Token con tu lógica centralizada
            var accessToken = _jwtTokenService.GenerateAccessToken(user.Id, user.Email, user.Roles);
            var refreshTokenString = _jwtTokenService.GenerateRefreshToken();

            // 3. Guardar el Refresh Token en la Base de Datos
            await _refreshTokenService.SaveTokenRefreshAsync(user.Id, refreshTokenString, cancellationToken);

            _logger.LogInformation("Usuario autenticado exitosamente mediante proveedor externo ({Provider}): {Email}", request.Provider, user.Email);

            return Result<LoginUserDto>.Success(new LoginUserDto
            {
                Id = user.Id,
                Email = user.Email,
                AccessToken = accessToken,
                RefreshToken = refreshTokenString
            });
        }
    }
}
