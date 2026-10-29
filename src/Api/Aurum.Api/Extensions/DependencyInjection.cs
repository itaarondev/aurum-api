namespace Aurum.Api.Extensions
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddControllers();
            services.AddOpenApi();
            // services.AddConfiguracionCookie();

            // Configuración unificada de autenticación (JWT + Google)
            services.AddJwtAuthentication(configuration);

            // Configuración de CORS para desarrollo local
            services.AddCors(options =>
            {
                options.AddPolicy("AllowLocalClient", policy =>
                {
                    policy.WithOrigins("http://127.0.0.1:5500", "http://localhost:5500", "https://localhost:5001") // Ajusta según el puerto de tu frontend/HTML
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials(); // ¡Fundamental para que el navegador acepte y envíe cookies HttpOnly!
                });
            });

            return services;
        }
    }
}
