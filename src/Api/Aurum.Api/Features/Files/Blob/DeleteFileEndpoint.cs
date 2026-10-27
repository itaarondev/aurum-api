using Aurum.Application.Features.Files.Commands;
using MediatR;

namespace Aurum.Api.Features.Files.Blob
{
    public static class DeleteFileEndpoint
    {
        public static IEndpointRouteBuilder MapDeleteFileEndpoint(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapDelete("/api/files/{containerName}/{fileName}", async (string containerName, string fileName, ISender mediator) =>
            {
                var command = new DeleteFileCommand(containerName, fileName);

                var result = await mediator.Send(command);

                if (!result.Succeeded)
                    return Results.NotFound("El archivo no fue encontrado en el contenedor especificado.");

                return Results.Ok(result);
            })
            .WithName("DeleteFile")
            .WithTags("Files");

            return endpoints;
        }
    }
}
