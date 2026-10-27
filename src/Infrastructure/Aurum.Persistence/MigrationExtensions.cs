using Aurum.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aurum.Persistence
{
    public static class MigrationExtensions
    {
        public static async Task<IHost> MigrateDatabaseAsync(this IHost app)
        {
            using var scope = app.Services.CreateScope();
            var services = scope.ServiceProvider;

            try
            {
                var context = services.GetRequiredService<ApplicationDbContext>();
                await context.Database.MigrateAsync();
            }
            catch (Exception ex)
            {
                // Usamos MigrationExtensions en lugar de Program para evitar referencias cruzadas
                var logger = services.GetRequiredService<ILogger<ApplicationDbContext>>();
                logger.LogError(ex, "Ocurrió un error al aplicar las migraciones a la base de datos.");
                throw;
            }

            return app;
        }
    }
}