using Aurum.Application.Common.Interfaces;
using Aurum.Application.Common.Models;
using FluentValidation;
using MediatR;

namespace Aurum.Application.Features.Auth.Login
{
    public record LoginCommand(string Email, string Password) : IRequest<Result<LoginUserDto>>;    
    public class LoginCommandValidator : AbstractValidator<LoginCommand>
    {
        public LoginCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("El correo electrónico es obligatorio.")
                .EmailAddress().WithMessage("El formato del correo electrónico no es válido.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("La contraseña es obligatoria.");
        }

        public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<LoginUserDto>>
        {
            private readonly IIdentityService _identityService;

            public LoginCommandHandler(IIdentityService identityService)
            {
                _identityService = identityService;
            }

            public async Task<Result<LoginUserDto>> Handle(LoginCommand request, CancellationToken cancellationToken)
            {
                return await _identityService.LoginUserAsync(request.Email, request.Password);
            }
        }
    }
}
