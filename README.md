# Tickets Core

Base de API REST por capas en .NET 10, EF Core SQL Server e Identity con claves `Guid`.

```text
src/
  Controllers/     Controllers y rutas por atributos en una misma capa
  Services/        Casos de uso e interfaces (estructura inicial)
  Repositories/    Acceso a datos e interfaces (estructura inicial)
  Contracts/       DTO de solicitudes/respuestas (estructura inicial)
  Handlers/        Respuestas, validación y excepciones
Infrastructure/
  Models/         Ticket, History, State
  Identity/       ApplicationUser y ApplicationRole
  Security/       Configuración de Identity y cookies
  Persistence/    ApplicationDbContext, configuraciones Fluent API y migraciones
  Sql/            DatabaseObjects.sql: SP, FN y plantilla TG
tests/SchemaChecks/ Verificación ejecutable del contrato ER sin base de datos
```

Flujo previsto: Controller → Service → Repository → ApplicationDbContext.
La estructura inicial no publica endpoints de negocio ni de autenticación.
No existe una capa Routes separada. Heredar de `ApiControllerBase` permite usar
`api/[controller]` y las convenciones de validación de `[ApiController]`.

## Modelo y decisiones

- Se conserva exactamente el conjunto de diez tablas y sus columnas del ER en `dbo`
  (el diagrama no especifica un esquema SQL). Los nombres C# son PascalCase;
  Fluent API conserva `ID`, `User_id`, `State_id`, `Ticket_id`, `name`, `prevState`, `newState`.
- `uuid` → `Guid` / `uniqueidentifier`; `bool` → `bit`; `int` → `int`;
  `datetimeoffset` → `DateTimeOffset?` / `datetimeoffset` para `LockoutEnd`.
- El ER no fija longitudes ni nulabilidad. Se usan cadenas Unicode `nvarchar(max)`
  para textos libres, 256 caracteres para nombres/correos/teléfonos indexados y
  128 para claves string de logins/tokens. Los campos de dominio son obligatorios;
  los opcionales de Identity conservan la nulabilidad estándar.
- Las seis UK del ER se implementan con índices únicos. Las UK opcionales de Identity
  usan filtros `IS NOT NULL`, permitiendo varios usuarios sin correo/teléfono asignado.
  La comparación entre valores depende de la collation de SQL Server.
- Identity genera `AspNetUserRoles`, `AspNetUserClaims`, `AspNetUserLogins`,
  `AspNetUserTokens` y `AspNetRoleClaims` mediante sus modelos oficiales genéricos
  `IdentityUserRole<Guid>`, `IdentityUserClaim<Guid>`, `IdentityUserLogin<Guid>`,
  `IdentityUserToken<Guid>` e `IdentityRoleClaim<Guid>`, conservando PK/FK del ER.
- `TwoFactorEnabled`, heredado de Identity pero ausente del ER, está excluido del mapeo.
  Esta base no soporta persistencia de 2FA; incorporarla requiere ampliar el esquema.
- Las relaciones del dominio no borran en cascada para conservar historial y evitar
  rutas múltiples de cascada en SQL Server. Identity conserva sus cascadas estándar.
  No se agregan fechas, estados iniciales ni relaciones nuevas al historial.

## Conexión e inicio

La conexión se configura en el archivo **`.env` de la raíz del proyecto**.
Se incluye un `.env` local con el ejemplo de Windows LocalDB, sin credenciales reales.
Para preparar otro checkout, copiar la plantilla una sola vez:

```powershell
Copy-Item .env.example .env
```

Editar `.env` y dejar una sola línea activa con los datos de tu SQL Server:

```dotenv
ConnectionStrings__DefaultConnection='Server=SERVIDOR,1433;Database=TicketsCore;User Id=USUARIO;Password=CONTRASENA;Encrypt=True;TrustServerCertificate=False'
```

`Server` contiene el host o IP y, opcionalmente, el puerto separado por coma;
no lleva `https://`. También puedes usar `Server=EQUIPO\INSTANCIA`.
Con autenticación integrada de Windows, sustituir `User Id` y `Password` por
`Integrated Security=True`. La plantilla incluye ambos ejemplos y LocalDB.
Usar comillas simples alrededor del valor evita la interpolación de `$` y conserva
barras y `#`. Si una contraseña contiene comillas simples, usar una variable del
sistema o el administrador de secretos del despliegue para evitar problemas de escape.

Ejecutar desde la raíz del proyecto:

```powershell
dotnet tool restore
dotnet restore
dotnet build
dotnet ef database update
dotnet run
```

La API carga `.env` con DotNetEnv **antes** de construir la configuración de .NET;
`dotnet ef` también pasa por este inicio. `ConnectionStrings__DefaultConnection`
corresponde a `ConnectionStrings:DefaultConnection` para el contexto e Identity.
Las variables de entorno del sistema/terminal tienen prioridad sobre `.env`,
siguiendo [NoClobber de DotNetEnv](https://github.com/tonerdo/dotnet-env).
Reiniciar la API después de editar el archivo.

`.env` y sus variantes locales ya están excluidos de Git mediante `.gitignore` y
del build/publish mediante el proyecto. `.env.example` sí se versiona y debe contener
solo valores de ejemplo. El archivo local contiene texto plano: no es un almacén
cifrado. En despliegue, inyectar la variable desde el servidor o su gestor de secretos;
no es necesario distribuir `.env`.

`appsettings.json` deja la conexión vacía para no mezclar credenciales con archivos
versionados. Si no hay una conexión configurada, la aplicación falla al iniciar con
un mensaje que indica la variable necesaria. No se registra la cadena de conexión.

La conexión debe apuntar a una instancia accesible con un certificado de confianza
y permisos para crear/actualizar el esquema. EF se registra por solicitud mediante
`AddDbContext<ApplicationDbContext>`. Identity usa el mismo contexto y conexión.
No se aplican migraciones ni se crean bases de datos automáticamente al iniciar.

Después de aplicar la migración, ejecutar `Infrastructure/Sql/DatabaseObjects.sql`
contra esa misma base con SSMS o `sqlcmd`. El script usa `GO` y `CREATE OR ALTER`:

```powershell
sqlcmd -S localhost -d TicketsCore -E -b -i Infrastructure/Sql/DatabaseObjects.sql
```

El SP consulta el historial por ticket y la FN cuenta tickets por estado. El TG queda
declarado como plantilla comentada porque no se especificó su regla de negocio.
No se genera historial automáticamente ni se identifica al actor con el creador del ticket.
El script SQL se administra por separado de las migraciones.

## Respuestas y seguridad

Los resultados JSON de controllers se envuelven con `success`, `message`, `data`,
`errors` y `traceId`. Los errores de validación usan HTTP 400 y errores por campo;
las excepciones usan HTTP 500 sin exponer detalles internos. Respuestas HTTP vacías
de error, como 404, 401 y 403, usan el mismo formato. Un 204 no contiene cuerpo.
OpenAPI y archivos no se envuelven. Puede devolverse un `ApiResponse<T>` explícito
para establecer un mensaje propio sin doble envoltura.

```json
{
  "success": false,
  "message": "Not Found",
  "data": null,
  "errors": null,
  "traceId": "identificador de la solicitud"
}
```

Identity registra usuarios/roles, hashing estándar, tokens de recuperación y cookies
HttpOnly/Secure. Configura correo único, longitud mínima de contraseña de 8 y bloqueo
tras 5 intentos por 5 minutos (los flujos de acceso deberán usar `lockoutOnFailure`).
Las respuestas de autenticación usan 401/403 sin redirecciones HTML.
Los futuros controllers que requieran sesión deben declarar `[Authorize]`; los flujos
de autenticación y la protección CSRF de operaciones con cookies requieren sus propios
casos de uso. Los endpoints no deben devolver entidades de Identity directamente.

## Verificación

```powershell
dotnet run --project tests/SchemaChecks
dotnet ef migrations has-pending-model-changes
dotnet ef migrations script --idempotent --output artifacts/InitialSchema.sql
```

SchemaChecks verifica el modelo relacional contra las diez tablas del diagrama,
incluidos tipos, nombres, claves compuestas, índices únicos y relaciones.
Estas verificaciones no requieren SQL Server. Aplicar las migraciones y probar
los objetos SQL requiere una instancia de base de datos disponible.

Referencia para el mapeo de Identity: [documentación oficial de Microsoft](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/customize-identity-model?view=aspnetcore-10.0).
Los filtros en índices únicos opcionales siguen la [configuración de EF Core](https://learn.microsoft.com/en-us/ef/core/modeling/indexes).
