using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Logging;
using tickets_core.src.Controllers;
using tickets_core.src.Contracts;
using tickets_core.src.Services;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using tickets_core.Infrastructure;
using tickets_core.Infrastructure.Identity;
using tickets_core.Infrastructure.Models;
using tickets_core.Infrastructure.Persistence;
using tickets_core.Infrastructure.Persistence.Seeds;

// Base aislada y efímera: no lee .env ni modifica la base de desarrollo.
var connectionString = $"Server=(localdb)\\MSSQLLocalDB;Database=TicketsCore_Check_{Guid.NewGuid():N};Integrated Security=true;TrustServerCertificate=true";
var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
{
    ["ConnectionStrings:DefaultConnection"] = connectionString,
    ["Seed:Admin:UserName"] = "seed-admin",
    ["Seed:Admin:Email"] = "admin@example.invalid",
    ["Seed:Admin:Password"] = $"Check!{Guid.NewGuid():N}aA1"
}).Build();
foreach (var role in new[] { "Operator", "Technician" })
{
    configuration[$"Seed:{role}:UserName"] = "seed-" + role.ToLowerInvariant();
    configuration[$"Seed:{role}:Email"] = role.ToLowerInvariant() + "@example.invalid";
    configuration[$"Seed:{role}:Password"] = $"Check!{Guid.NewGuid():N}aA1";
}
await using var services = new ServiceCollection().AddLogging().AddInfrastructure(configuration).BuildServiceProvider();
await using var scope = services.CreateAsyncScope();
var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
try
{
    var migrator = db.GetService<Microsoft.EntityFrameworkCore.Migrations.IMigrator>();
    await migrator.MigrateAsync("20261026234211_TicketAuditAndSeeds");
    await DatabaseSeed.RunAsync(services, configuration);
    await DatabaseSeed.RunAsync(services, configuration);
    Check(await db.Roles.CountAsync() == 3 && await db.Users.CountAsync() == 3 && await db.States.CountAsync() == 3,
        "Seeds repetibles sin duplicados");
    Check(await db.Roles.AllAsync(role => role.NormalizedName == role.Name!.ToUpper()), "Roles normalizados");
    var admin = await db.Users.SingleAsync(user => user.UserName == "seed-admin");
    var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    Check(await users.IsInRoleAsync(admin, "Admin") && await users.CheckPasswordAsync(admin, configuration["Seed:Admin:Password"]!),
        "Admin con contraseña hasheada y rol");
    var adminRole = await db.Roles.SingleAsync(role => role.Name == "Admin");
    db.AddRange(
        new IdentityUserClaim<Guid> { UserId = admin.Id, ClaimType = "test", ClaimValue = "test" },
        new IdentityRoleClaim<Guid> { RoleId = adminRole.Id, ClaimType = "test", ClaimValue = "test" },
        new IdentityUserLogin<Guid> { UserId = admin.Id, LoginProvider = "test", ProviderKey = "test" },
        new IdentityUserToken<Guid> { UserId = admin.Id, LoginProvider = "test", Name = "test", Value = "test" });
    await db.SaveChangesAsync();
    var states = await db.States.ToDictionaryAsync(state => state.Name, state => state.Id);
    // Un ticket anterior a la migración debe conservar sus datos y recibir un equipo explícito.
    var legacyId = Guid.NewGuid();
    await Execute("INSERT dbo.TICKET (ID, User_id, State_id, Title, Description) VALUES (@id, @user, @state, N'Legado', N'Conservar')",
        ("id", legacyId), ("user", admin.Id), ("state", states["Pendiente"]));
    await db.Database.MigrateAsync();
    var legacy = await db.Tickets.AsNoTracking().SingleAsync(ticket => ticket.Id == legacyId);
    Check(legacy.Equipment == "Sin especificar" && legacy.Title == "Legado" && legacy.Description == "Conservar",
        "Migración conserva tickets existentes y completa Equipment");
    await migrator.MigrateAsync("20261026234211_TicketAuditAndSeeds");
    await Execute("EXEC dbo.SP_EditTicket @Ticket_id=@ticket, @User_id=@user, @Title=N'Legado editado'",
        ("ticket", legacyId), ("user", admin.Id));
    await Execute("EXEC dbo.SP_GetTicketsGroupedByState");
    await db.Database.MigrateAsync();
    Check(await db.Tickets.AnyAsync(ticket => ticket.Id == legacyId && ticket.Equipment == "Sin especificar" && ticket.Title == "Legado editado"),
        "Reversión restaura procedimientos anteriores y permite reaplicar migración");
    await Execute("DELETE dbo.HISTORY WHERE Ticket_id=@id; DELETE dbo.TICKET WHERE ID=@id", ("id", legacyId));

    var ticketService = scope.ServiceProvider.GetRequiredService<TicketService>();
    var createdTicket = await ticketService.CreateAsync(admin.Id, new CreateTicketRequest("Equipo", "Servicio", "Laptop A"));
    Check(createdTicket.Equipment == "Laptop A", "Creación devuelve Equipment");
    await ticketService.EditAsync(createdTicket.Id, admin.Id, new EditTicketRequest(Equipment: "Laptop B"));
    var details = await ticketService.GetTicketByIdAsync(createdTicket.Id);
    Check(details?.Ticket.Equipment == "Laptop B" && details.History.Single().Comment.Contains("Laptop A")
        && details.History.Single().Comment.Contains("Laptop B"), "Edición solo de equipo persiste y audita ambos valores");
    Check((await ticketService.GetGroupedByStateAsync()).SelectMany(group => group.Tickets)
        .Single(item => item.Id == createdTicket.Id).Equipment == "Laptop B", "Consulta agrupada deserializa Equipment");
    await ticketService.EditAsync(createdTicket.Id, admin.Id, new EditTicketRequest(Title: "Equipo actualizado"));
    Check((await ticketService.GetTicketByIdAsync(createdTicket.Id))?.Ticket.Equipment == "Laptop B",
        "Omitir Equipment en edición conserva el valor");
    await ExpectError(51004, "EXEC dbo.SP_EditTicket @Ticket_id=@ticket, @User_id=@user, @Equipment=N' '",
        ("ticket", createdTicket.Id), ("user", admin.Id));
    await ExpectError(51005, "EXEC dbo.SP_EditTicket @Ticket_id=@ticket, @User_id=@user, @Equipment=N'Laptop B'",
        ("ticket", createdTicket.Id), ("user", admin.Id));
    await ticketService.DeleteAsync(createdTicket.Id, admin.Id, new DeleteTicketRequest());
    Check((await ticketService.GetDeletedTicketsAsync()).Single(item => item.Id == createdTicket.Id).Equipment == "Laptop B",
        "Consulta de eliminados conserva Equipment");
    await Execute("DELETE dbo.HISTORY WHERE Ticket_id=@id; DELETE dbo.TICKET WHERE ID=@id", ("id", createdTicket.Id));
    db.ChangeTracker.Clear();

    // Kestrel local con identidad de prueba; ejerce los contratos y controllers HTTP reales.
    var httpBuilder = WebApplication.CreateBuilder();
    httpBuilder.Logging.ClearProviders();
    httpBuilder.WebHost.UseUrls("http://127.0.0.1:0");
    httpBuilder.Services.AddInfrastructure(configuration);
    httpBuilder.Services.AddControllers().AddApplicationPart(typeof(TicketController).Assembly);
    await using (var app = httpBuilder.Build())
    {
        app.Use(async (context, next) =>
        {
            context.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, admin.Id.ToString()),
                new Claim(ClaimTypes.Role, "Admin")
            }, "DatabaseChecks"));
            await next(context);
        });
        app.UseAuthorization();
        app.MapControllers();
        await app.StartAsync();
        using var client = new HttpClient { BaseAddress = new Uri(app.Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()!.Addresses.Single()) };
        foreach (var invalid in new object[]
        {
            new { title = "HTTP", description = "Test" },
            new { title = "HTTP", description = "Test", equipment = " " }
        })
        {
            using var response = await client.PostAsJsonAsync("/api/tickets", invalid);
            Check(response.StatusCode == HttpStatusCode.BadRequest, "POST rechaza Equipment ausente o vacío");
        }
        using var creation = await client.PostAsJsonAsync("/api/tickets",
            new { title = "HTTP", description = "Test", equipment = "Impresora A" });
        creation.EnsureSuccessStatusCode();
        var body = await creation.Content.ReadFromJsonAsync<JsonElement>();
        var httpId = body.GetProperty("data").GetProperty("id").GetGuid();
        Check(body.GetProperty("data").GetProperty("equipment").GetString() == "Impresora A", "POST devuelve Equipment");
        foreach (var invalid in new object[] { new { equipment = " " }, new { comment = "Sin cambios" } })
        {
            using var response = await client.PutAsJsonAsync($"/api/tickets/{httpId}", invalid);
            Check(response.StatusCode == HttpStatusCode.BadRequest, "PUT rechaza Equipment vacío o edición sin campos");
        }
        using var update = await client.PutAsJsonAsync($"/api/tickets/{httpId}", new { equipment = "Impresora B" });
        update.EnsureSuccessStatusCode();
        var detail = await client.GetFromJsonAsync<JsonElement>($"/api/tickets/{httpId}");
        Check(detail.GetProperty("data").GetProperty("ticket").GetProperty("equipment").GetString() == "Impresora B",
            "PUT solo de Equipment y GET detalle");
        var groups = await client.GetFromJsonAsync<JsonElement>("/api/tickets");
        Check(groups.GetProperty("data").EnumerateArray().SelectMany(group => group.GetProperty("tickets").EnumerateArray())
            .Single(item => item.GetProperty("ID").GetGuid() == httpId).GetProperty("equipment").GetString() == "Impresora B",
            "GET agrupado incluye Equipment");
        using var deletionRequest = new HttpRequestMessage(HttpMethod.Delete, $"/api/tickets/{httpId}")
        {
            Content = JsonContent.Create(new { })
        };
        using var deletionResponse = await client.SendAsync(deletionRequest);
        deletionResponse.EnsureSuccessStatusCode();
        var deleted = await client.GetFromJsonAsync<JsonElement>("/api/tickets/deleted");
        Check(deleted.GetProperty("data").EnumerateArray().Single(item => item.GetProperty("id").GetGuid() == httpId)
            .GetProperty("equipment").GetString() == "Impresora B", "GET eliminados incluye Equipment");
        await Execute("DELETE dbo.HISTORY WHERE Ticket_id=@id; DELETE dbo.TICKET WHERE ID=@id", ("id", httpId));
        await app.StopAsync();
    }

    var ticket = new Ticket { UserId = admin.Id, StateId = states["Pendiente"], Title = "Original", Description = "Inicial", Equipment = "PC original" };
    var other = new Ticket { UserId = admin.Id, StateId = states["Pendiente"], Title = "Otro", Description = "Inicial", Equipment = "PC secundario" };
    db.Tickets.AddRange(ticket, other);
    await db.SaveChangesAsync();
    var created = db.Entry(ticket).Property<DateTime>("CreatedAt").CurrentValue;
    Check(created > DateTime.UtcNow.AddMinutes(-1), "CreatedAt UTC desde EF");

    await ExpectError(51003, "EXEC dbo.SP_ChangeTicketState @Ticket_id=@ticket, @State_id=@state, @User_id=@user",
        ("ticket", ticket.Id), ("state", states["Resuelto"]), ("user", admin.Id));
    Check(await db.Histories.CountAsync() == 0, "Salto de estado sin efectos parciales");
    await Execute("EXEC dbo.SP_ChangeTicketState @Ticket_id=@ticket, @State_id=@state, @User_id=@user, @Comment=N'Atendiendo'",
        ("ticket", ticket.Id), ("state", states["En Proceso"]), ("user", admin.Id));
    await ExpectError(51003, "EXEC dbo.SP_ChangeTicketState @Ticket_id=@ticket, @State_id=@state, @User_id=@user",
        ("ticket", ticket.Id), ("state", states["Pendiente"]), ("user", admin.Id));
    await ExpectError(51001, "EXEC dbo.SP_EditTicket @Ticket_id=@ticket, @User_id=@user, @Title=N'Inválido'",
        ("ticket", ticket.Id), ("user", Guid.NewGuid()));
    await ExpectError(51005, "EXEC dbo.SP_EditTicket @Ticket_id=@ticket, @User_id=@user, @Title=N'Original'",
        ("ticket", ticket.Id), ("user", admin.Id));
    await ExpectError(51004, "EXEC dbo.SP_EditTicket @Ticket_id=@ticket, @User_id=@user, @Title=N' ' ",
        ("ticket", ticket.Id), ("user", admin.Id));
    await Execute("""
        CREATE TRIGGER dbo.TG_Test_RejectHistory ON dbo.HISTORY AFTER INSERT AS
        THROW 51998, N'Error de auditoría simulado.', 1;
        """);
    try
    {
        await ExpectError(51998, "EXEC dbo.SP_EditTicket @Ticket_id=@ticket, @User_id=@user, @Title=N'No persistir', @Equipment=N'No persistir'",
            ("ticket", ticket.Id), ("user", admin.Id));
        await db.Entry(ticket).ReloadAsync();
        Check(ticket.Title == "Original" && ticket.Equipment == "PC original" && await db.Histories.CountAsync() == 1,
            "Un fallo al guardar historial revierte el cambio del ticket");
    }
    finally { await Execute("DROP TRIGGER dbo.TG_Test_RejectHistory"); }
    await Execute("EXEC dbo.SP_EditTicket @Ticket_id=@ticket, @User_id=@user, @Title=N'Nuevo', @Description=N'Actualizada', @Equipment=N'PC nuevo', @Comment=N'Corrección'",
        ("ticket", ticket.Id), ("user", admin.Id));
    var edit = await db.Histories.SingleAsync(history => history.TicketId == ticket.Id && history.PrevState == history.NewState);
    Check(edit.Comment.Contains("Corrección") && edit.Comment.Contains("Original") && edit.Comment.Contains("Nuevo")
        && edit.Comment.Contains("Inicial") && edit.Comment.Contains("Actualizada")
        && edit.Comment.Contains("PC original") && edit.Comment.Contains("PC nuevo"), "Historial con comentario y cambios completos");
    await Execute("EXEC dbo.SP_DeleteTicket @Ticket_id=@ticket, @User_id=@user, @Comment=N'Duplicado'",
        ("ticket", ticket.Id), ("user", admin.Id));
    await db.Entry(ticket).ReloadAsync();
    Check(ticket.DeletedAt is not null && ticket.Equipment == "PC nuevo" && ticket.StateId == states["En Proceso"], "Soft delete conserva estado");
    Check(await db.Histories.CountAsync(history => history.TicketId == ticket.Id) == 3, "Historial para cada operación");
    var deletion = await db.Histories.SingleAsync(history => history.TicketId == ticket.Id && history.Comment.Contains("soft delete"));
    Check(deletion.PrevState == deletion.NewState && deletion.Comment.Contains("Duplicado"), "Historial de borrado conserva estado y comentario");
    foreach (var sql in new[]
    {
        "EXEC dbo.SP_DeleteTicket @Ticket_id=@ticket, @User_id=@user",
        "EXEC dbo.SP_EditTicket @Ticket_id=@ticket, @User_id=@user, @Title=N'Otro'",
        "EXEC dbo.SP_ChangeTicketState @Ticket_id=@ticket, @User_id=@user, @State_id=@state"
    })
        await ExpectError(51000, sql, ("ticket", ticket.Id), ("user", admin.Id), ("state", states["Resuelto"]));

    // Dos transiciones simultáneas no deben crear dos historiales ni perder cambios.
    async Task<int> Transition()
    {
        try
        {
            await Execute("EXEC dbo.SP_ChangeTicketState @Ticket_id=@ticket, @User_id=@user, @State_id=@state",
                ("ticket", other.Id), ("user", admin.Id), ("state", states["En Proceso"]));
            return 1;
        }
        catch (SqlException error) when (error.Number == 51003) { return 0; }
    }
    Check((await Task.WhenAll(Transition(), Transition())).Sum() == 1, "Transiciones concurrentes serializadas");
    await Execute("EXEC dbo.SP_ChangeTicketState @Ticket_id=@ticket, @User_id=@user, @State_id=@state",
        ("ticket", other.Id), ("user", admin.Id), ("state", states["Resuelto"]));
    Check(await db.Histories.CountAsync(history => history.TicketId == other.Id) == 2, "Secuencia completa hasta Resuelto");

    // Verifica actualización directa por SQL, en varias filas, y CreatedAt inmutable.
    await Execute("UPDATE dbo.TICKET SET Title=Title + N'!', CreatedAt='2000-01-01', UpdatedAt='2000-01-01'");
    await db.Entry(ticket).ReloadAsync();
    Check(db.Entry(ticket).Property<DateTime>("CreatedAt").CurrentValue == created
        && db.Entry(ticket).Property<DateTime>("UpdatedAt").CurrentValue > created, "Trigger UTC y CreatedAt inmutable");

    foreach (var table in new[] { "TICKET", "HISTORY", "STATE", "AspNetUsers", "AspNetRoles",
                 "AspNetUserRoles", "AspNetUserClaims", "AspNetUserLogins", "AspNetUserTokens", "AspNetRoleClaims" })
        await Execute($"""
            UPDATE dbo.[{table}] SET CreatedAt='2000-01-01', UpdatedAt='2000-01-01';
            IF EXISTS (SELECT 1 FROM dbo.[{table}] WHERE CreatedAt < '2020-01-01' OR UpdatedAt < '2020-01-01')
                THROW 51999, N'Fechas no mantenidas por trigger.', 1;
            """);

    var older = Guid.NewGuid();
    var newer = Guid.NewGuid();
    await Execute("""
        INSERT dbo.TICKET (ID, User_id, State_id, Title, Description, Equipment, CreatedAt)
        VALUES (@older, @user, @state, N'Anterior', N'Test', N'Equipo anterior', '2020-01-01'),
               (@newer, @user, @state, N'Reciente', N'Test', N'Equipo reciente', '2021-01-01');
        """, ("older", older), ("newer", newer), ("user", admin.Id), ("state", states["Pendiente"]));

    await using (var connection = new SqlConnection(connectionString))
    {
        await connection.OpenAsync();
        await using var command = new SqlCommand("EXEC dbo.SP_GetTicketsGroupedByState", connection);
        await using var reader = await command.ExecuteReaderAsync();
        var names = new List<string>();
        var listed = new List<Guid>();
        while (await reader.ReadAsync())
        {
            names.Add(reader.GetString(1));
            using var json = JsonDocument.Parse(reader.GetString(2));
            var dates = new List<DateTime>();
            foreach (var item in json.RootElement.EnumerateArray())
            {
                Check(!string.IsNullOrWhiteSpace(item.GetProperty("Equipment").GetString()), "JSON agrupado incluye Equipment");
                listed.Add(item.GetProperty("ID").GetGuid());
                dates.Add(item.GetProperty("CreatedAt").GetDateTime());
            }
            Check(dates.SequenceEqual(dates.OrderDescending()), "Tickets ordenados por fecha");
        }
        Check(names.SequenceEqual(StateSeed.Names) && listed.SequenceEqual(new[] { newer, older, other.Id }),
            "Agrupación incluye estados vacíos y excluye borrados");
    }

    // Comprueba que EF también puede actualizar entidades con los triggers instalados.
    other.Title = "Desde EF";
    await db.SaveChangesAsync();
    // El rollback de la migración retira objetos antes de retirar columnas.
    await migrator.MigrateAsync("20261026223908_Init");
    await db.Database.MigrateAsync();
    Console.WriteLine("SQL Server verificado: Equipment (contratos, servicios, repositorios, SP e historial), migraciones, seeds, fechas, auditoría, transiciones, concurrencia y soft delete.");
}
finally
{
    await db.Database.EnsureDeletedAsync();
}

async Task Execute(string sql, params (string Name, object Value)[] parameters)
{
    await using var connection = new SqlConnection(connectionString);
    await connection.OpenAsync();
    await using var command = new SqlCommand(sql, connection);
    foreach (var (name, value) in parameters) command.Parameters.AddWithValue("@" + name, value);
    await command.ExecuteNonQueryAsync();
}

async Task ExpectError(int number, string sql, params (string Name, object Value)[] parameters)
{
    try { await Execute(sql, parameters); }
    catch (SqlException error) when (error.Number == number) { return; }
    throw new InvalidOperationException($"Se esperaba el error SQL {number}.");
}

static void Check(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}
