using Aurum.Application.Features.Files.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Aurum.Api.Features.Files.Blob
{
    public static class UploadFileEndpoint
    {
        public static IEndpointRouteBuilder MapUploadFileEndpoint(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapPost("/api/files/upload", async ([FromForm] UploadFileRequest request, ISender mediator) =>
            {
                using var stream = request.File.OpenReadStream();

                var command = new UploadFileCommand(stream, request.File.FileName, request.ContainerName);

                var result = await mediator.Send(command);

                return Results.Ok(result);
            })
            .DisableAntiforgery() // Necesario si manejas subidas de archivos sin token de antifalsificación global
            .WithName("UploadFile")
            .WithTags("Files");

            return endpoints;
        }
    }
}
