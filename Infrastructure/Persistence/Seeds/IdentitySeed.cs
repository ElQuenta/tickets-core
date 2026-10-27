using Microsoft.AspNetCore.Identity;
using tickets_core.Infrastructure.Identity;

namespace tickets_core.Infrastructure.Persistence.Seeds;

public static class IdentitySeed
{
    public static readonly string[] RoleNames = ["Admin", "Operator", "Technician"];

    public static async Task RunAsync(IServiceProvider services, IConfiguration configuration)
    {
        // Validar toda la configuración antes de escribir datos.
        var accounts = RoleNames.Select(role => ReadAccount(configuration, role)).ToArray();
        var roles = services.GetRequiredService<RoleManager<ApplicationRole>>();
        var users = services.GetRequiredService<UserManager<ApplicationUser>>();
        if (accounts.Select(account => users.NormalizeName(account.UserName)).Distinct().Count() != accounts.Length)
            throw new InvalidOperationException("Los UserName de Admin, Operator y Technician deben ser distintos.");
        if (accounts.Select(account => users.NormalizeEmail(account.Email)).Distinct().Count() != accounts.Length)
            throw new InvalidOperationException("Los Email de Admin, Operator y Technician deben ser distintos.");

        foreach (var name in RoleNames)
        {
            var role = await roles.FindByNameAsync(name);
            if (role is null)
                Ensure(await roles.CreateAsync(new ApplicationRole { Name = name }), $"rol {name}");
            else if (!string.Equals(role.Name, name, StringComparison.Ordinal))
            {
                role.Name = name;
                Ensure(await roles.UpdateAsync(role), $"rol {name}");
            }
        }

        foreach (var account in accounts)
            await SeedUserAsync(users, account);
    }

    private static async Task SeedUserAsync(UserManager<ApplicationUser> users, SeedAccount account)
    {
        var user = await users.FindByNameAsync(account.UserName);
        var emailOwner = await users.FindByEmailAsync(account.Email);
        if (emailOwner is not null && emailOwner.Id != user?.Id)
            throw new InvalidOperationException($"Seed__{account.Role}__Email ya pertenece a otra cuenta. Revisa su UserName y Email.");

        if (user is null)
        {
            user = new ApplicationUser { UserName = account.UserName, Email = account.Email };
            Ensure(await users.CreateAsync(user, account.Password), $"usuario {account.Role}");
        }
        else if (users.NormalizeEmail(user.Email) != users.NormalizeEmail(account.Email))
            throw new InvalidOperationException($"La cuenta Seed__{account.Role}__UserName ya existe con otro correo. Revisa la configuración.");

        // Repetir el seed no restablece contraseñas ni elimina roles ya asignados.
        if (!await users.IsInRoleAsync(user, account.Role))
            Ensure(await users.AddToRoleAsync(user, account.Role), $"asignación {account.Role}");
    }

    private static SeedAccount ReadAccount(IConfiguration configuration, string role) => new(role,
        Required(configuration, $"Seed:{role}:UserName").Trim(),
        Required(configuration, $"Seed:{role}:Email").Trim(),
        Required(configuration, $"Seed:{role}:Password"));

    private static string Required(IConfiguration configuration, string key) =>
        !string.IsNullOrWhiteSpace(configuration[key]) ? configuration[key]!
            : throw new InvalidOperationException($"Configura {key.Replace(":", "__")} para ejecutar los seeds.");

    private static void Ensure(IdentityResult result, string operation)
    {
        // Solo códigos de Identity: no incluir contraseñas ni datos personales.
        if (!result.Succeeded)
            throw new InvalidOperationException($"Error de seed ({operation}): " +
                string.Join(", ", result.Errors.Select(error => error.Code)));
    }

    private sealed record SeedAccount(string Role, string UserName, string Email, string Password);
}
