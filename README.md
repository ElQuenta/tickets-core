# Tickets Core

Backend de gestión de tickets desarrollado con ASP.NET Core, .NET 10, Entity Framework Core, SQL Server y ASP.NET Core Identity.

## Local setup

### 1. Preparar los requisitos

Instala las siguientes herramientas:

- **Git** para clonar el repositorio.
- **SDK de .NET 10.x** para restaurar dependencias, compilar y ejecutar la API. Instala el SDK, no solo el runtime.
- **SQL Server** disponible localmente o en un servidor accesible. En Windows puedes usar SQL Server Express LocalDB, que es la opción configurada en `.env.example`. En Linux o macOS utiliza una instancia de SQL Server accesible y ajusta la conexión.

Comprueba las herramientas desde tu terminal:

```sh
git --version
dotnet --version
```

Si usas LocalDB en Windows, verifica que esté instalado e inicia la instancia:

```powershell
sqllocaldb info
sqllocaldb start MSSQLLocalDB
```

### 2. Clonar el repositorio

```sh
git clone https://github.com/ElQuenta/tickets-core.git
cd tickets-core
```

Ejecuta los siguientes comandos desde esta carpeta, donde están `tickets-core.csproj`, `dotnet-tools.json` y `.env.example`. La aplicación busca el archivo `.env` en el directorio desde el que se ejecuta.

### 3. Restaurar las dependencias y herramientas

```sh
dotnet restore
dotnet tool restore
```

El primer comando restaura los paquetes NuGet del proyecto. El segundo instala la versión de `dotnet-ef` definida en `dotnet-tools.json`, necesaria para aplicar las migraciones de Entity Framework Core.

### 4. Configurar las variables de entorno

Copia `.env.example` a `.env`.

En Windows con PowerShell:

```powershell
Copy-Item .env.example .env
```

En Linux o macOS:

```sh
cp .env.example .env
```

Abre `.env` y configura los siguientes grupos:

| Variables | Sección que las utiliza | Cuándo se necesitan |
| --- | --- | --- |
| `ConnectionStrings__DefaultConnection` | Entity Framework Core y SQL Server | Para iniciar la API, aplicar migraciones y ejecutar seeds. |
| `Seed__Admin__UserName`, `Seed__Admin__Email`, `Seed__Admin__Password` | Seed de Identity: cuenta con rol `Admin` | Al ejecutar `--seed`. |
| `Seed__Operator__UserName`, `Seed__Operator__Email`, `Seed__Operator__Password` | Seed de Identity: cuenta con rol `Operator` | Al ejecutar `--seed`. |
| `Seed__Technician__UserName`, `Seed__Technician__Email`, `Seed__Technician__Password` | Seed de Identity: cuenta con rol `Technician` | Al ejecutar `--seed`. |
| `Smtp__Host`, `Smtp__Port`, `Smtp__EnableSsl` | Servidor de correo de Identity | Para enviar correos durante el registro, la confirmación de correo y la recuperación de contraseña. |
| `Smtp__UserName`, `Smtp__Password` | Autenticación del servidor SMTP | Si el servidor de correo requiere credenciales. |
| `Smtp__FromAddress`, `Smtp__FromName` | Remitente de los correos de Identity | Al enviar correos; `FromAddress` es obligatorio y `FromName` es el nombre visible. |

Los dobles guiones bajos (`__`) representan secciones de configuración de .NET. Por ejemplo, `Seed__Admin__Email` corresponde a `Seed:Admin:Email`.

#### Conexión a SQL Server

Para Windows con LocalDB puedes conservar la conexión incluida en `.env.example`:

```dotenv
ConnectionStrings__DefaultConnection='Server=(localdb)\MSSQLLocalDB;Database=TicketsCore;Trusted_Connection=True;MultipleActiveResultSets=true'
```

Si utilizas SQL Server con usuario y contraseña, reemplaza la línea anterior y ajusta servidor, base de datos y credenciales:

```dotenv
ConnectionStrings__DefaultConnection='Server=SERVIDOR,1433;Database=TicketsCore;User Id=USUARIO;Password=CONTRASENA;Encrypt=True;TrustServerCertificate=False'
```

`.env.example` también incluye una alternativa con autenticación integrada de Windows. Mantén **una sola conexión activa** y asegúrate de que la instancia esté disponible. La cuenta utilizada debe tener permisos para crear la base de datos si no existe y aplicar las migraciones.

#### Usuarios de prueba para los seeds

Completa los nueve valores de `Seed__Admin__*`, `Seed__Operator__*` y `Seed__Technician__*` en tu `.env`. Cada grupo define el nombre de usuario, correo y contraseña de una cuenta de prueba con su rol correspondiente.

Utiliza **nombres de usuario y correos diferentes para las tres cuentas**. Las contraseñas deben cumplir la política de Identity: al menos ocho caracteres, con mayúsculas, minúsculas, números y un carácter especial.

Estos valores solo se leen al ejecutar `--seed`; no son necesarios para aplicar migraciones ni para iniciar la API si las cuentas ya existen. Para iniciar sesión con una cuenta creada por el seed, utiliza su **correo y contraseña**.

#### Correo SMTP

Configura este grupo con los datos de tu servidor de correo si vas a probar los flujos que envían mensajes:

```dotenv
Smtp__Host=
Smtp__Port=587
Smtp__EnableSsl=true
Smtp__UserName=
Smtp__Password=
Smtp__FromAddress=
Smtp__FromName='Tickets Core'
```

`Host` y `FromAddress` deben tener valores para enviar correos. Ajusta el puerto, el uso de TLS mediante `EnableSsl` y las credenciales según tu servidor SMTP.

Puedes dejar SMTP sin configurar para aplicar migraciones, ejecutar seeds, iniciar la API e iniciar sesión con las cuentas del seed. Las operaciones que intenten enviar correo fallarán hasta configurar un servidor válido.

> [!NOTE]
> El proyecto carga `.env` automáticamente y conserva los valores de las variables ya definidas en el entorno del proceso. Si un cambio en `.env` no se refleja, revisa si la misma variable está definida en tu terminal o sistema. Usa comillas simples para conservar caracteres como `$`, `#`, espacios y barras. `.env` está excluido de Git; guarda las credenciales únicamente allí o en variables del entorno.

### 5. Crear o actualizar la base de datos

Con SQL Server disponible y la conexión configurada, ejecuta:

```sh
dotnet ef database update
```

Este comando aplica las migraciones pendientes y crea la base de datos si aún no existe y la cuenta tiene permisos. Incluye las tablas de la aplicación, Identity y los objetos SQL definidos por las migraciones.

**La API no aplica migraciones automáticamente al iniciar.** Ejecuta este paso también cuando incorpores cambios que incluyan nuevas migraciones.

### 6. Cargar los datos iniciales y usuarios de prueba

Después de aplicar las migraciones y completar las variables `Seed__*`, ejecuta:

```sh
dotnet run -- --seed
```

El comando crea los roles `Admin`, `Operator` y `Technician`, las tres cuentas configuradas y los estados de ticket `Pendiente`, `En Proceso` y `Resuelto`. Al terminar, el proceso finaliza; debes iniciar la API por separado en el siguiente paso.

Puedes repetirlo sin duplicar los datos existentes. Repetir el seed **no restablece las contraseñas** de cuentas ya creadas ni elimina sus roles. Cambiar una contraseña en `.env` después de crear la cuenta no cambia su contraseña en la base de datos.

### 7. Preparar HTTPS e iniciar la API

Prepara el certificado de desarrollo:

```sh
dotnet dev-certs https --trust
```

Si tu sistema requiere pasos adicionales para confiar en el certificado, sigue las indicaciones del comando. Después inicia la API con el perfil HTTPS:

```sh
dotnet run --launch-profile https
```

El perfil está definido en `Properties/launchSettings.json` y utiliza estas direcciones:

- HTTPS: `https://localhost:7272`.
- HTTP: `http://localhost:5261`.

Utiliza **HTTPS**, ya que la API redirige las peticiones HTTP y las cookies de autenticación requieren una conexión segura. Mantén esta terminal abierta mientras usas el backend.

### 8. Comprobar la ejecución y conectar el frontend

Comprueba que la terminal muestre la API escuchando en las direcciones anteriores. Con el perfil de desarrollo puedes abrir el documento OpenAPI en:

```text
https://localhost:7272/openapi/v1.json
```

Este recurso devuelve el contrato JSON de la API.