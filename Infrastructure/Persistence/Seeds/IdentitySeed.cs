using Microsoft.AspNetCore.Identity;
using tickets_core.Infrastructure.Identity;

namespace tickets_core.Infrastructure.Persistence.Seeds;

public static class IdentitySeed
{
    public static readonly string[] RoleNames = ["Admin", "Operator", "Technician"];

    public static async Task RunAsync(IServiceProvider services, IConfiguration configuration)
    {
        // La configuración recibe estas claves desde variables de entorno o .env.
        var userName = Required(configuration, "Seed:Admin:UserName");
        var email = Required(configuration, "Seed:Admin:Email");
        var password = Required(configuration, "Seed:Admin:Password");
        var roles = services.GetRequiredService<RoleManager<ApplicationRole>>();
        var users = services.GetRequiredService<UserManager<ApplicationUser>>();
        foreach (var name in RoleNames)
            if (!await roles.RoleExistsAsync(name))
                Ensure(await roles.CreateAsync(new ApplicationRole { Name = name }));

        var admin = await users.FindByNameAsync(userName);
        if (admin is null)
        {
            admin = new ApplicationUser { UserName = userName, Email = email };
            Ensure(await users.CreateAsync(admin, password));
        }
        else if (!string.Equals(admin.Email, email, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("El usuario de seed ya existe con otro correo.");

        if (!await users.IsInRoleAsync(admin, "Admin"))
            Ensure(await users.AddToRoleAsync(admin, "Admin"));
    }

    private static string Required(IConfiguration configuration, string key) =>
        !string.IsNullOrWhiteSpace(configuration[key]) ? configuration[key]!
            : throw new InvalidOperationException($"Configura {key.Replace(":", "__")} para ejecutar los seeds.");

    private static void Ensure(IdentityResult result)
    {
        // Solo códigos: no revelar credenciales ni datos personales en errores.
        if (!result.Succeeded)
            throw new InvalidOperationException("Error de seed Identity: " +
                string.Join(", ", result.Errors.Select(error => error.Code)));
    }
}
