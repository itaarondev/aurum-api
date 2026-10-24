using Aurum.Application.Common.Interfaces;
using FluentValidation;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Aurum.Application.Features.Files.Commands
{
    public record DeleteFileCommand(string ContainerName, string FileName) : IRequest<bool>;

    public class DeleteFileCommandValidator : AbstractValidator<DeleteFileCommand>
    {
        public DeleteFileCommandValidator()
        {
            RuleFor(v => v.ContainerName)
                .NotEmpty().WithMessage("El nombre del contenedor es obligatorio.")
                .MinimumLength(3).WithMessage("El nombre del contenedor debe tener al menos 3 caracteres.")
                .MaximumLength(30).WithMessage("El nombre del contenedor no puede superar los 63 caracteres.")
                .Matches("^[a-z0-9-]+$").WithMessage("El nombre del contenedor solo puede contener letras minúsculas, números y guiones.");

            RuleFor(v => v.FileName)
                .NotEmpty().WithMessage("El nombre del archivo es obligatorio.")
                .MaximumLength(50).WithMessage("El nombre del archivo no puede superar los 50 caracteres.");
        }
    }

    public class DeleteFileCommandHandler : IRequestHandler<DeleteFileCommand, bool>
    {
        private readonly IBlobStorageService _blobStorageService;

        public DeleteFileCommandHandler(IBlobStorageService blobStorageService)
        {
            _blobStorageService = blobStorageService;
        }

        public async Task<bool> Handle(DeleteFileCommand request, CancellationToken cancellationToken)
        {
            return await _blobStorageService.DeleteFileAsync(request.ContainerName, request.FileName);
        }
    }
}
