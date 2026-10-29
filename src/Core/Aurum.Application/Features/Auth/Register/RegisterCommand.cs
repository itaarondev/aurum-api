using Aurum.Application.Common.Interfaces;
using Aurum.Application.Common.Models;
using Aurum.Domain.Enums;
using FluentValidation;
using MediatR;

namespace Aurum.Application.Features.Auth.Register
{
    public record RegisterCommand(string Email, string Password, string ConfirmPassword, UserRole Role) : IRequest<Result<string>>;
    public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
    {
        public RegisterCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("El correo electrónico es obligatorio.")
                .EmailAddress().WithMessage("El formato del correo electrónico no es válido.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("La contraseña es obligatoria.")
                .MinimumLength(6).WithMessage("La contraseña debe tener al menos 6 caracteres.");

            RuleFor(x => x.ConfirmPassword)
                .Equal(x => x.Password).WithMessage("Las contraseñas no coinciden.");

            RuleFor(x => x.Role)
                .IsInEnum().WithMessage("El rol especificado no es válido.");
        }
    }
    public class RegisterCommandHandler : IRequestHandler<RegisterCommand, Result<string>>
    {
        private readonly IIdentityService _identityService;

        public RegisterCommandHandler(IIdentityService identityService)
        {
            _identityService = identityService;
        }

        public async Task<Result<string>> Handle(RegisterCommand request, CancellationToken cancellationToken)
        {
            return await _identityService.RegisterUserAsync(request.Email, request.Password, request.Role);
        }
    }
}