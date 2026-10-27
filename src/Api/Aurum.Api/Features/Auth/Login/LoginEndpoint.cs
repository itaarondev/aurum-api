using Aurum.Application.Features.Auth.Login;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Aurum.Api.Features.Auth.Login
{
    public static class LoginEndpoint
    {
        public static IEndpointRouteBuilder MapLoginEndpoint(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapPost("/api/auth/login", async ([FromBody] LoginRequest request, ISender mediator, SignInManager<IdentityUser> signInManager) =>
            {
                var command = new LoginCommand(request.Email, request.Password);

                var result = await mediator.Send(command);

                if (!result.Succeeded)
                {
                    return Results.BadRequest(result);
                }

                var user = new IdentityUser
                {
                    Id = result.Data!.Id,
                    Email = result.Data.Email,
                    UserName = result.Data.Email
                };
                await signInManager.SignInAsync(user, isPersistent: false);

                return Results.Ok(result);
            })
            .WithName("LoginUser")
            .WithTags("Auth");

            return endpoints;
        }
    }
}
