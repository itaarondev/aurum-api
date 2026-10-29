
using Aurum.Application.Features.Auth.Register;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Aurum.Api.Features.Auth.Register
{
    public static class RegisterEndpoint
    {
        public static IEndpointRouteBuilder MapRegisterEndpoint(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapPost("/api/auth/register", async ([FromBody] RegisterRequest request, ISender mediator) =>
            {
                var command = new RegisterCommand(request.Email, request.Password, request.ConfirmPassword, request.Role);

                var result = await mediator.Send(command);

                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { message = "Error en el registro", errors = result.Errors });
                }

                return Results.Ok(new { message = "Usuario registrado exitosamente", email = result.Data });
            })
            .WithName("RegisterUser")
            .WithTags("Auth")
            .RequireAuthorization(policy => policy.RequireRole("Admin"));

            return endpoints;
        }
    }
}
