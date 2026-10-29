using Aurum.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aurum.Persistence.Context
{
    public static class ApplicationDbContextSeed
    {
        public static async Task SeedDefaultUserAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var services = scope.ServiceProvider;

            try
            {
                var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
                var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
                var configuration = services.GetRequiredService<IConfiguration>();
                var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("DefaultSeeder");

                // 1. Asegurar que los roles existan
                string adminRole = UserRole.Admin.ToString();
                string userRole = UserRole.User.ToString();

                if (!await roleManager.RoleExistsAsync(adminRole))
                {
                    await roleManager.CreateAsync(new IdentityRole(adminRole));
                }

                if (!await roleManager.RoleExistsAsync(userRole))
                {
                    await roleManager.CreateAsync(new IdentityRole(userRole));
                }

                // 2. Leer las credenciales de forma segura desde la configuración / Key Vault
                string adminEmail = configuration["AdminSettings:Email"]
                    ?? throw new InvalidOperationException("La propiedad 'AdminSettings:Email' no está configurada.");

                string adminPassword = configuration["AdminSettings:Password"]
                    ?? throw new InvalidOperationException("La propiedad 'AdminSettings:Password' no está configurada.");

                // 3. Crear el usuario Administrador por defecto si no existe
                var existingAdmin = await userManager.FindByEmailAsync(adminEmail);

                if (existingAdmin == null)
                {
                    var defaultAdmin = new IdentityUser
                    {
                        UserName = adminEmail,
                        Email = adminEmail,
                        EmailConfirmed = true
                    };

                    var result = await userManager.CreateAsync(defaultAdmin, adminPassword);

                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(defaultAdmin, adminRole);
                        logger.LogInformation("Usuario Administrador creado exitosamente: {Email}", adminEmail);
                    }
                    else
                    {
                        logger.LogError("No se pudo crear el usuario Administrador: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
                    }
                }
            }
            catch (Exception ex)
            {
                var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DefaultSeeder");
                logger.LogError(ex, "Ocurrió un error al sembrar los datos iniciales en la base de datos.");
            }
        }
    }
}
