using Aurum.Application.Common.Interfaces;
using Aurum.Application.Common.Models;
using FluentValidation;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Aurum.Application.Features.Files.DeleteContainer
{
    public record DeleteContainerCommand(string ContainerName) : IRequest<Result<string>>;

    public class DeleteContainerCommandValidator : AbstractValidator<DeleteContainerCommand>
    {
        public DeleteContainerCommandValidator()
        {
            RuleFor(v => v.ContainerName)
                .NotEmpty().WithMessage("El nombre del contenedor es obligatorio.")
                .MinimumLength(3).WithMessage("El nombre del contenedor debe tener al menos 3 caracteres.")
                .MaximumLength(30).WithMessage("El nombre del contenedor no puede superar los 30 caracteres.")
                .Matches("^[a-z0-9-]+$").WithMessage("El nombre del contenedor solo puede contener letras minúsculas, números y guiones.");
        }
    }

    public class DeleteContainerCommandHandler : IRequestHandler<DeleteContainerCommand, Result<string>>
    {
        private readonly IBlobStorageService _blobStorageService;

        public DeleteContainerCommandHandler(IBlobStorageService blobStorageService)
        {
            _blobStorageService = blobStorageService;
        }

        public async Task<Result<string>> Handle(DeleteContainerCommand request, CancellationToken cancellationToken)
        {
            // Llamamos al servicio de infraestructura para eliminar el contenedor si existe
            await _blobStorageService.DeleteContainerAsync(request.ContainerName);

            return Result<string>.Success($"Contenedor '{request.ContainerName}' eliminado exitosamente.");
        }
    }
}
