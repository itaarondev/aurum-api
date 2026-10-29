using Aurum.Application.Features.Auth.ExternalLogin;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using System.Security.Claims;

namespace Aurum.Api.Features.Auth.Google
{
    public static class GoogleAuthEndpoint
    {
        public static IEndpointRouteBuilder MapGoogleAuthEndpoints(this IEndpointRouteBuilder endpoints)
        {
            // 1. Redirigir al usuario al formulario oficial de Google
            endpoints.MapGet("/api/auth/signin-google", () =>
            {
                var properties = new AuthenticationProperties { RedirectUri = "/api/auth/google-callback" };
                return Results.Challenge(properties, new[] { GoogleDefaults.AuthenticationScheme });
            })
            .WithTags("Auth");

            // 2. Callback a donde Google redirige con la identidad comprobada
            endpoints.MapGet("/api/auth/google-callback", async (HttpContext httpContext, ISender mediator, CancellationToken cancellationToken) =>
            {
                // Extraer el resultado de la autenticación provista por Google
                var authenticateResult = await httpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);

                if (!authenticateResult.Succeeded || authenticateResult.Principal == null)
                {
                    return Results.BadRequest(new { succeeded = false, errors = new[] { "Falló la autenticación con Google." } });
                }

                var email = authenticateResult.Principal.FindFirstValue(ClaimTypes.Email);
                var name = authenticateResult.Principal.FindFirstValue(ClaimTypes.Name) ?? email;

                if (string.IsNullOrEmpty(email))
                {
                    return Results.BadRequest(new { succeeded = false, errors = new[] { "No se pudo obtener el correo desde Google." } });
                }

                // Despachar el comando a la capa de Aplicación a través de MediatR (Clean Architecture)
                var command = new ExternalLoginCommand(email, name, "Google");
                var result = await mediator.Send(command, cancellationToken);

                if (!result.Succeeded)
                {
                    return Results.BadRequest(result);
                }

                // Inyectar las cookies HttpOnly seguras con la respuesta de MediatR
                httpContext.Response.Cookies.Append("Aurum.AccessToken", result.Data.AccessToken, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.None,
                    Expires = DateTimeOffset.UtcNow.AddMinutes(1)
                });

                httpContext.Response.Cookies.Append("Aurum.RefreshToken", result.Data.RefreshToken, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.None,
                    Expires = DateTimeOffset.UtcNow.AddDays(1)
                });

                // Redirigir de vuelta a la aplicación Web/Front-end
                return Results.Redirect("/index.html");
            })
            .WithTags("Auth");

            return endpoints;
        }
    }
}
