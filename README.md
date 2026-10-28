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
La aplicación publica los endpoints nativos de Identity bajo `/api/auth`.
No existe una capa Routes separada. Heredar de `ApiControllerBase` permite usar
`api/[controller]` y las convenciones de validación de `[ApiController]`.

## Modelo y decisiones

- Se conserva el conjunto de diez tablas del ER en `dbo`, con fechas de auditoría
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
  Todas las tablas incorporan `CreatedAt` y `UpdatedAt` (`datetime2`, UTC), con
  defaults `SYSUTCDATETIME()`. EF asigna fechas al guardar; los triggers mantienen
  `UpdatedAt` y conservan `CreatedAt` ante actualizaciones SQL de una o varias filas.
  Dominio, usuario y rol exponen propiedades C#; las otras cinco entidades oficiales
  de Identity usan propiedades sombra de EF.
  Las filas existentes reciben la fecha de aplicación de la migración; no se
  reconstruyen fechas históricas que no estaban almacenadas.
- `TICKET.Equipment` es texto Unicode obligatorio (`nvarchar(max)`). Se exige al crear un ticket;
  en edición, omitirlo o enviar `null` conserva el equipo actual, y un valor vacío se rechaza.
  Se devuelve en creación, detalle, agrupación por estado y listado de eliminados.
  Los cambios de equipo registran el valor anterior y el nuevo en el historial.
  La migración `AddTicketEquipment` conserva los tickets existentes y les asigna
  `Sin especificar`; instala las nuevas versiones de los SP y restaura las anteriores al revertir.
- `TICKET.DeletedAt` es nullable y empieza en `NULL`. El SP de lectura excluye
  borrados; las consultas EF directas deben filtrar `DeletedAt == null` cuando corresponda.
  El historial permanece consultable después del borrado.

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

La migración `TicketAuditAndSeeds` instala las fechas, los triggers y los SP.
No necesitas ejecutar scripts manualmente. `Infrastructure/Sql/DatabaseObjects.sql`
permite reinstalar los objetos con SSMS o `sqlcmd` (usa `GO` y `CREATE OR ALTER`).
La copia SQL de la carpeta de migraciones es un recurso embebido de esa migración;
los cambios futuros deben instalarse mediante nuevas migraciones.

## Seeds

Completar en `.env` o en variables de entorno, sin guardar los valores en Git:

```dotenv
Seed__Admin__UserName=
Seed__Admin__Email=
Seed__Admin__Password=
Seed__Operator__UserName=
Seed__Operator__Email=
Seed__Operator__Password=
Seed__Technician__UserName=
Seed__Technician__Email=
Seed__Technician__Password=
```

Después de completar esos valores y aplicar las migraciones:

```powershell
dotnet run -- --seed
```

Este comando ejecuta los seeds y termina sin iniciar el servidor. Crea los roles
`Admin`, `Operator` y `Technician`; Identity normaliza los nombres como `ADMIN`,
`OPERATOR` y `TECHNICIAN`. Crea los estados `Pendiente`, `En Proceso` y `Resuelto`,
y tres cuentas de prueba, cada una con su rol y contraseña hasheada por Identity.
Los nombres de usuario y correos deben ser distintos entre las tres cuentas.
Cada contraseña debe cumplir
la política de Identity: mínimo 8 caracteres, mayúsculas, minúsculas, dígitos y símbolos.
Los seeds son transaccionales, serializan ejecuciones concurrentes y pueden repetirse
sin duplicar registros ni cambiar contraseñas existentes. Un usuario existente con
otro correo produce un error; también se rechaza un correo ya asociado a otra cuenta.
Las credenciales de las tres cuentas son obligatorias; no hay valores por defecto.
Si cambias una contraseña en `.env`, el seed no reemplaza la que ya está guardada;
para una cuenta existente utiliza el flujo de cambio o recuperación de contraseña.
El login utiliza el `UserName` configurado aunque el campo de la petición se llame `email`.

## Operaciones SQL de tickets

| Procedimiento | Parámetros | Comportamiento |
| --- | --- | --- |
| `dbo.SP_ChangeTicketState` | `@Ticket_id`, `@State_id`, `@User_id`, `@Comment = NULL` | Permite avanzar un paso (`Pendiente → En Proceso → Resuelto`) o retroceder a cualquier estado anterior, incluido `Resuelto → Pendiente`. Registra ambos estados y el comentario. Rechaza saltos hacia adelante y repetir el estado. |
| `dbo.SP_EditTicket` | `@Ticket_id`, `@User_id`, `@Title = NULL`, `@Description = NULL`, `@Comment = NULL`, `@Equipment = NULL` | `NULL` conserva el campo. Rechaza textos vacíos y operaciones sin cambios. Concatena al comentario los valores anteriores y nuevos y crea historial conservando el estado. |
| `dbo.SP_DeleteTicket` | `@Ticket_id`, `@User_id`, `@Comment = NULL` | Asigna `DeletedAt`, registra el comentario y conserva el estado en ticket e historial. |
| `dbo.SP_GetTicketsGroupedByState` | Ninguno | Devuelve `StateId`, `StateName`, `Tickets` (array JSON), un registro por estado, incluso sin tickets (`[]`). Excluye borrados y ordena cada grupo por `CreatedAt DESC`, con desempate por ID. |

La migración `AllowTicketStateRollback` instala la regla de retrocesos sin cambiar
tablas ni datos existentes. Aplicarla con `dotnet ef database update`; al revertirla
se restaura la regla anterior que solo permitía avances.

Las escrituras bloquean el ticket durante la transacción para evitar carreras;
el ticket y su historial se guardan juntos o se revierten juntos. Rechazan tickets
inexistentes o borrados y actores inexistentes. `@User_id` debe proceder del usuario
autenticado en la futura capa de servicio; los SP no implementan autorización por rol.
No se generan eventos de negocio por SQL directo o `SaveChanges`: las operaciones
que requieran historial deben invocar estos SP. Los errores de negocio usan los
números SQL `51000` a `51005`. La consulta agrupada ordena los estados según el flujo.

## Crear y editar tickets

`POST /api/tickets` requiere sesión y los tres campos de texto:

```json
{
  "title": "No enciende",
  "description": "El equipo no responde al botón de encendido.",
  "equipment": "Laptop Dell Latitude 5420 - INV-001"
}
```

`PUT /api/tickets/{ticketId}` permite cambiar solo el equipo, con comentario opcional:

```json
{
  "equipment": "Laptop Dell Latitude 5430 - INV-002",
  "comment": "Corrección del equipo reportado"
}
```

El detalle (`GET /api/tickets/{ticketId}`) y los eliminados (`GET /api/tickets/deleted`)
exponen `equipment`. La consulta agrupada (`GET /api/tickets`) también incluye
`equipment` en cada ticket y conserva los identificadores `ID`, `User_id` y `State_id`.
Los endpoints de estado y borrado conservan el equipo del ticket.

## Administrar usuarios

Ambos endpoints requieren el rol `Admin` y usan el formato `ApiResponse`:

- `GET /api/users`: lista usuarios con `id`, `username`, `email`, `emailConfirmed`,
  `phoneNumber`, `phoneNumberConfirmed`, `lockoutEnabled`, `lockoutEnd`,
  `accessFailedCount`, `createdAt`, `updatedAt` y `roles` (lista de strings, `[]` si no tiene roles).
- `PUT /api/users/{userId}`: reemplaza la información editable y devuelve el usuario con sus roles.
  `username` y `email` son obligatorios. Los otros campos toman los valores del ejemplo
  si se omiten. Omitir `password` o enviar `null` conserva la contraseña actual;
  cualquier string, incluido uno vacío, pasa por las validaciones de contraseña de Identity.
  `accessFailedCount` es de solo lectura y conserva su valor al editar el usuario.

```json
{
  "username": "juan",
  "email": "juan@ejemplo.com",
  "phoneNumber": null,
  "emailConfirmed": false,
  "phoneNumberConfirmed": false,
  "lockoutEnabled": true,
  "lockoutEnd": null,
  "password": "NuevaClaveSegura123!"
}
```

La actualización usa Identity para validar, normalizar y hashear; los cambios se
guardan en una sola escritura, sin modificaciones parciales ante errores de validación.
Un nombre, correo o teléfono duplicado, o una contraseña inválida, devuelve `400`
con `errors`; un usuario inexistente devuelve `404`. Las respuestas no exponen
contraseñas, hashes ni stamps. Los roles se asignan por el endpoint existente
`POST /api/users/{userId}/roles` y se conservan al editar el usuario.
No se requieren migraciones.

`DELETE /api/users/{userId}/roles/{roleId}` requiere el rol `Admin` y no recibe cuerpo.
Quita la asignación y devuelve `200` con `data: { "userId": "...", "roleId": "..." }`
en el formato `ApiResponse`. Devuelve `404` si el usuario no existe o no tiene ese rol,
y `409` si se intenta quitar su último rol. Las eliminaciones del mismo usuario
se serializan mediante una transacción para que solicitudes simultáneas no lo dejen sin roles.

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

Identity registra usuarios/roles, hashing estándar, tokens de recuperación, bearer tokens y cookies
HttpOnly/Secure. Configura correo único, longitud mínima de contraseña de 8 y bloqueo
tras 5 intentos por 5 minutos (los flujos de acceso deberán usar `lockoutOnFailure`).
Las respuestas de autenticación usan 401/403 sin redirecciones HTML.
Los controllers que requieran sesión deben declarar `[Authorize]`. Los endpoints
no deben devolver entidades de Identity directamente.

## Endpoints de Identity

Las rutas de autenticación se publican bajo `/api/auth` y aparecen en `/openapi/v1.json`
en Development. No hay ruta de 2FA: `/api/auth/manage/2fa` devuelve 404 y no figura
en OpenAPI. No se añade una columna `TwoFactorEnabled` ni se requieren migraciones nuevas.

| Método | Ruta |
| --- | --- |
| POST | `/api/auth/register` |
| POST | `/api/auth/login` |
| POST | `/api/auth/refresh` |
| GET | `/api/auth/confirmEmail` |
| POST | `/api/auth/resendConfirmationEmail` |
| POST | `/api/auth/forgotPassword` |
| POST | `/api/auth/resetPassword` |
| GET / POST | `/api/auth/manage/info` |

`POST /api/auth/register` recibe el nombre de usuario, correo y contraseña:

```json
{
  "username": "juan",
  "email": "juan@ejemplo.com",
  "password": "ClaveSegura123!"
}
```

El nombre de usuario y el correo deben ser únicos. Se conservan las validaciones
de contraseña de Identity y el envío del correo de confirmación mediante SMTP.
El registro exitoso devuelve `200 OK` con el mismo contrato del login:

```json
{
  "username": "juan",
  "roles": [],
  "tokenType": "Bearer",
  "accessToken": "...",
  "expiresIn": 3600,
  "refreshToken": "..."
}
```

El registro inicia sesión y no asigna roles automáticamente. Los errores de
validación devuelven `400` con `errors` en formato ValidationProblem de Identity.
Con `?useCookies=true` o `?useSessionCookies=true` devuelve únicamente `username`
y `roles`, y establece la cookie de sesión, igual que el login.

`POST /api/auth/login?useCookies=true` inicia sesión con cookies. Sin ese parámetro
devuelve tokens bearer propios de Identity (no JWT). El cuerpo de login contiene
`email` y `password`; la búsqueda de la cuenta se realiza por correo, aunque su
nombre de usuario sea diferente. Para las cuentas de seed, enviar su correo en
el campo `email`.

`manage/info` requiere autenticación. Las respuestas de los handlers nativos
conservan su formato de Identity y no pasan por el filtro MVC `ApiResponseFilter`.
El login y el registro personalizados también conservan una respuesta directa: incluyen `username`
y `roles`, además de `tokenType`, `accessToken`, `expiresIn` y `refreshToken` cuando
se usan tokens. Con cookies devuelve únicamente `username` y `roles`.

El flujo se divide en `CustomLoginEndpoints` y `CustomRegisterEndpoints` (respuesta HTTP),
`AuthService` (registro, credenciales, bloqueo, roles y sesión) e `IdentityTokenService`
(emisión de tokens de Identity). Los DTO están en `src/Contracts`.
El login y el registro nativos se excluyen en `IdentityEndpoints` para evitar rutas duplicadas.
`/refresh` conserva el contrato nativo de Identity.

Referencia: [Identity API de Microsoft](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity-api-authorization?view=aspnetcore-10.0).

Para registro, confirmación de correo y recuperación de contraseña, configurar un
servidor SMTP en `.env` o variables del entorno antes de usar esos flujos:

```dotenv
Smtp__Host=
Smtp__Port=587
Smtp__EnableSsl=true
Smtp__UserName=
Smtp__Password=
Smtp__FromAddress=
Smtp__FromName='Tickets Core'
```

`Host` y `FromAddress` son obligatorios para enviar. `UserName` y `Password`
dependen del servidor SMTP; los secretos permanecen fuera de archivos versionados.
La API puede iniciar sin SMTP, pero las operaciones que intenten enviar correo
fallarán hasta configurarlo. No se registran enlaces ni códigos de recuperación.

## Verificación

```powershell
dotnet run --project tests/SchemaChecks
dotnet run --project tests/DatabaseChecks
dotnet ef migrations has-pending-model-changes
dotnet ef migrations script --idempotent --output artifacts/InitialSchema.sql
```

SchemaChecks verifica el modelo relacional contra las diez tablas del diagrama,
incluidos tipos, nombres, claves compuestas, índices únicos y relaciones.
SchemaChecks no requiere SQL Server. DatabaseChecks utiliza Windows LocalDB con
una base temporal independiente: aplica/revierte migraciones, ejecuta seeds dos
veces y valida fechas, transiciones, concurrencia, historial y borrado lógico.
La base temporal se elimina al finalizar; no lee `.env`.

Referencia para el mapeo de Identity: [documentación oficial de Microsoft](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/customize-identity-model?view=aspnetcore-10.0).
Los filtros en índices únicos opcionales siguen la [configuración de EF Core](https://learn.microsoft.com/en-us/ef/core/modeling/indexes).
