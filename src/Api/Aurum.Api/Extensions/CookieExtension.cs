namespace Aurum.Api.Extensions
{
    public static class CookieExtension
    {
        public static IServiceCollection AddConfiguracionCookie(this IServiceCollection services) 
        {
            services.ConfigureApplicationCookie(options =>
            {
                options.Cookie.Name = "Aurum.AuthCookie";
                options.Cookie.HttpOnly = true; // Impide el acceso por JavaScript (Protección XSS)
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // Obliga el uso de HTTPS
                //options.Cookie.SameSite = SameSiteMode.Lax; // Protección contra CSRF permitiendo navegación estándar
                options.Cookie.SameSite = SameSiteMode.None; // SameSite=None para permitir que viaje entre dominios distintos (Front y Back en Azure)
                options.ExpireTimeSpan = TimeSpan.FromMinutes(1); // Tiempo de vida de la sesión
                options.SlidingExpiration = true; // Renueva el tiempo si el usuario sigue activo

                // Evitamos que responda con redirecciones automáticas de HTML (ideal para APIs REST)
                options.Events.OnRedirectToLogin = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                };
                options.Events.OnRedirectToAccessDenied = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                };
            });

            return services;
        }
    }
}
