
using Aurum.Application.Features.Files.CreateContainer;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Aurum.Api.Features.Files.Container
{
    public static class CreateContainerEndpoint
    {
        public static IEndpointRouteBuilder MapCreateContainerEndpoint(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapPost("/api/files/container", async ([FromBody] CreateContainerRequest request, ISender mediator) =>
            {
                var command = new CreateContainerCommand(request.ContainerName);
                var result = await mediator.Send(command);

                return Results.Ok(result);
            })
            .WithName("CreateContainer")
            .WithTags("Files");

            return endpoints;
        }
    }
}
