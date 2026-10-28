using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using tickets_core.Infrastructure.Persistence;

// Verifica el contrato ER sin conectarse a SQL Server ni requerir un proveedor en memoria.
using var context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
    .UseSqlServer("Server=localhost;Database=SchemaCheck;Integrated Security=true").Options);
var tables = context.Model.GetRelationalModel().Tables.ToDictionary(table => table.Name);
var expected = new Dictionary<string, (string Columns, string Key)>
{
    ["TICKET"] = ("ID:uniqueidentifier User_id:uniqueidentifier State_id:uniqueidentifier Title:nvarchar(max) Description:nvarchar(max) Equipment:nvarchar(max)", "ID"),
    ["HISTORY"] = ("ID:uniqueidentifier Ticket_id:uniqueidentifier User_id:uniqueidentifier Comment:nvarchar(max) prevState:nvarchar(max) newState:nvarchar(max)", "ID"),
    ["STATE"] = ("ID:uniqueidentifier name:nvarchar(256)", "ID"),
    ["AspNetUsers"] = ("Id:uniqueidentifier UserName:nvarchar(256) NormalizedUserName:nvarchar(256) Email:nvarchar(256) NormalizedEmail:nvarchar(256) EmailConfirmed:bit PasswordHash:nvarchar(max) SecurityStamp:nvarchar(max) ConcurrencyStamp:nvarchar(max) PhoneNumber:nvarchar(256) PhoneNumberConfirmed:bit LockoutEnd:datetimeoffset LockoutEnabled:bit AccessFailedCount:int", "Id"),
    ["AspNetRoles"] = ("Id:uniqueidentifier Name:nvarchar(256) NormalizedName:nvarchar(256) ConcurrencyStamp:nvarchar(max)", "Id"),
    ["AspNetUserRoles"] = ("UserId:uniqueidentifier RoleId:uniqueidentifier", "UserId RoleId"),
    ["AspNetUserClaims"] = ("Id:int UserId:uniqueidentifier ClaimType:nvarchar(max) ClaimValue:nvarchar(max)", "Id"),
    ["AspNetUserLogins"] = ("LoginProvider:nvarchar(128) ProviderKey:nvarchar(128) ProviderDisplayName:nvarchar(max) UserId:uniqueidentifier", "LoginProvider ProviderKey"),
    ["AspNetUserTokens"] = ("UserId:uniqueidentifier LoginProvider:nvarchar(128) Name:nvarchar(128) Value:nvarchar(max)", "UserId LoginProvider Name"),
    ["AspNetRoleClaims"] = ("Id:int RoleId:uniqueidentifier ClaimType:nvarchar(max) ClaimValue:nvarchar(max)", "Id")
};

Check(tables.Keys.ToHashSet().SetEquals(expected.Keys), "Deben existir exactamente las diez tablas del ER.");
foreach (var (name, contract) in expected)
{
    var table = tables[name];
    Check(table.Schema == "dbo", $"Esquema de {name}");
    Check(table.Columns.Select(column => $"{column.Name}:{column.StoreType}").ToHashSet()
        .SetEquals((contract.Columns + " CreatedAt:datetime2 UpdatedAt:datetime2" +
            (name == "TICKET" ? " DeletedAt:datetime2" : "")).Split(' ')), $"Columnas y tipos de {name}");
    Check(string.Join(' ', table.PrimaryKey!.Columns.Select(column => column.Name)) == contract.Key,
        $"Clave primaria de {name}");
}

var uniqueKeys = tables.Values.SelectMany(table => table.Indexes.Where(index => index.IsUnique)
    .Select(index => $"{table.Name}:{string.Join(' ', index.Columns.Select(column => column.Name))}"))
    .ToHashSet();
Check(uniqueKeys.SetEquals(new[]
{
    "STATE:name", "AspNetUsers:NormalizedUserName", "AspNetUsers:Email",
    "AspNetUsers:NormalizedEmail", "AspNetUsers:PhoneNumber", "AspNetRoles:NormalizedName"
}), "Claves únicas del ER");

var foreignKeys = tables.Values.SelectMany(table => table.ForeignKeyConstraints
    .Select(key => $"{table.Name}:{string.Join(' ', key.Columns.Select(column => column.Name))}" +
        $"->{key.PrincipalTable.Name}:{string.Join(' ', key.PrincipalColumns.Select(column => column.Name))}"))
    .ToHashSet();
Check(foreignKeys.SetEquals(new[]
{
    "TICKET:User_id->AspNetUsers:Id", "TICKET:State_id->STATE:ID",
    "HISTORY:Ticket_id->TICKET:ID", "HISTORY:User_id->AspNetUsers:Id",
    "AspNetUserRoles:UserId->AspNetUsers:Id", "AspNetUserRoles:RoleId->AspNetRoles:Id",
    "AspNetUserClaims:UserId->AspNetUsers:Id", "AspNetUserLogins:UserId->AspNetUsers:Id",
    "AspNetUserTokens:UserId->AspNetUsers:Id", "AspNetRoleClaims:RoleId->AspNetRoles:Id"
}), "Relaciones del ER, sin claves foráneas adicionales");

foreach (var name in new[] { "TICKET", "HISTORY", "STATE" })
    Check(tables[name].Columns.Where(column => column.Name != "DeletedAt").All(column => !column.IsNullable), $"Campos obligatorios de {name}");

foreach (var name in new[] { "TICKET", "HISTORY" })
    Check(tables[name].ForeignKeyConstraints.All(key => key.OnDeleteAction ==
        Microsoft.EntityFrameworkCore.Migrations.ReferentialAction.NoAction), $"Sin borrado en cascada de {name}");

foreach (var name in new[] { "AspNetUserClaims", "AspNetRoleClaims" })
    Check(tables[name].Columns.Single(column => column.Name == "Id").PropertyMappings
        .All(mapping => mapping.Property.ValueGenerated == ValueGenerated.OnAdd), $"Id autogenerado de {name}");

Check(tables["TICKET"].Columns.Single(column => column.Name == "DeletedAt").IsNullable, "DeletedAt nullable");
foreach (var entity in context.Model.GetEntityTypes())
{
    foreach (var name in new[] { "CreatedAt", "UpdatedAt" })
        Check(entity.FindProperty(name)?.GetDefaultValueSql() == "SYSUTCDATETIME()", $"Default UTC: {entity.Name}.{name}");
}

Console.WriteLine("Contrato ER verificado: 10 tablas, columnas, tipos, PK, UK y FK correctos.");

static void Check(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}
