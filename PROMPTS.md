## Prompt1
### Descripción
Prompt de inicio de proyecto, creación de Modelos y Cliente de Base de datos
### Prompt
Quiero que lo siguiente que hagas sea la creación de directorios siguiente el patron de arquitectura de API Rest por capas, además de la configuración del cliente de EF core, implementando los modelos con los cuales va a trabajar la basde de datos, esta debe seguir el Diagrama ER proporcionado. Debes de respetar el esquema, los tipos de datos y los tipos de claves marcadas. de igual forma quiero que generes un archivo donde se declaren los SP (Store Procedures) FN (functions) y TG (Triggers). Finalmente quiero que des la configuración basica Inicial de la conexion entre el ciente EF core con el Context del API. 
Consideraciones:
- Al trabajar con un API Realizada con C# las capaz de Controller y de Routes Son una.
- La ruta Raiz donde se administran los elementos como se controllers, services y repositories, debe ser `./src`, en contraste para la base de datos debe ser `./Infrastructure`, desde el cliente/context, los modelos y la condifuracion de Identity y Secuerity por parte de AspNetCore Identity. 
Estructura de Modelos Esperada:
- Considera la estandarización de tipos de respuestas es decir mediante handlers. formando formatos standar de respuestas.
```mermaid
erDiagram
    AspNetUsers ||--o{ TICKET : "creates"
    AspNetUsers ||--o{ HISTORY : "registers"
    TICKET ||--o{ HISTORY : "has"
    STATE ||--o{ TICKET : "in"

    AspNetUsers ||--o{ AspNetUserRoles : "has"
    AspNetRoles ||--o{ AspNetUserRoles : "assigned"
    AspNetUsers ||--o{ AspNetUserClaims : "has"
    AspNetUsers ||--o{ AspNetUserLogins : "uses"
    AspNetUsers ||--o{ AspNetUserTokens : "owns"
    AspNetRoles ||--o{ AspNetRoleClaims : "has"

    TICKET {
        uuid ID PK
        uuid User_id FK
        uuid State_id FK
        string Title
        string Description
    }

    HISTORY {
        uuid ID PK
        uuid Ticket_id FK
        uuid User_id FK
        string Comment
        string prevState
        string newState
    }

    STATE {
        uuid ID PK
        string name UK
    }

    AspNetUsers {
        uuid Id PK
        string UserName
        string NormalizedUserName UK
        string Email UK
        string NormalizedEmail UK
        bool EmailConfirmed
        string PasswordHash
        string SecurityStamp
        string ConcurrencyStamp
        string PhoneNumber UK
        bool PhoneNumberConfirmed 
        datetimeoffset LockoutEnd
        bool LockoutEnabled
        int AccessFailedCount
    }

    AspNetRoles {
        uuid Id PK
        string Name
        string NormalizedName UK
        string ConcurrencyStamp
    }

    AspNetUserRoles {
        uuid UserId PK,FK
        uuid RoleId PK,FK
    }

    AspNetUserClaims {
        int Id PK
        uuid UserId FK
        string ClaimType
        string ClaimValue
    }

    AspNetUserLogins {
        string LoginProvider PK
        string ProviderKey PK
        string ProviderDisplayName
        uuid UserId FK
    }

    AspNetUserTokens {
        uuid UserId PK,FK
        string LoginProvider PK
        string Name PK
        string Value
    }

    AspNetRoleClaims {
        int Id PK
        uuid RoleId FK
        string ClaimType
        string ClaimValue
    }

```

---
## Prompt2
### Descripción
Prompt de ajuste de migraciones y seeds
### Prompt
Quiero que añadas created at, updated at en todas las tablas, de igual forma quiero que crees los archivos de seed, donde se indiquen los roles de Admin, Operator y technician (NORMALIZA los nombres) a la par que un usuario de prueba de tipo admin con datos extraidos del env para no revelar datos sensibles.  de igual forma Los siguientes estados: Pendiente, En Proceso y Resuelto.