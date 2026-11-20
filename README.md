# mipyme

MiPyME Cuba — Plataforma de comercio electrónico para MiPyMES cubanas.

Stack: .NET 8 · Clean Architecture · ASP.NET Core Minimal APIs · EF Core 8 · PostgreSQL · Blazor WASM.

## Configuración inicial

### 1. Base de datos

La solución usa PostgreSQL. La cadena de conexión **no está versionada** en el repositorio (está en User Secrets). Debes configurarla en cada entorno:

#### Desarrollo (User Secrets)

```bash
dotnet user-secrets set "ConnectionStrings:Database" "Host=localhost;Port=5432;Database=mipyme;Username=postgres;Password=<TU_CONTRASEÑA>;" --project src/Presentation.Api/Presentation.Api.csproj
```

#### Producción (variable de entorno)

```bash
export ConnectionStrings__Database="Host=<host>;Port=5432;Database=mipyme;Username=postgres;Password=<TU_CONTRASEÑA>;"
```

> ⚠️ **Advertencia de seguridad:** La contraseña anterior (`***REMOVED-DB-PASSWORD-OLD***`) estuvo expuesta en el historial de git público. Debe considerarse **comprometida e inválida**. Rótala inmediatamente en todos los entornos donde se haya desplegado.
>
> 🗑️ **Purga del historial de git:** El secreto anterior sigue accesible en el historial de commits. Consulta la sección "Purga del historial" más abajo.

### 2. Gestión de secretos (US-00)

El secreto JWT **no está versionado** en el repositorio. Debes configurarlo en cada entorno:

#### Desarrollo (User Secrets)

Desde la raíz del repositorio, ejecuta:

```bash
dotnet user-secrets init --project src/Presentation.Api/Presentation.Api.csproj
dotnet user-secrets set "Jwt:Secret" "<VALOR_GENERADO>" --project src/Presentation.Api/Presentation.Api.csproj
```

Genera un valor criptográficamente fuerte, por ejemplo:

```bash
# PowerShell
[Convert]::ToBase64String((1..64 | ForEach-Object { Get-Random -Maximum 256 }))

# Linux/macOS
openssl rand -base64 64
```

#### Producción (variables de entorno)

Define la variable de entorno:

```bash
export Jwt__Secret="<VALOR_DE_PRODUCCION>"
```

El separador `__` es el soporte nativo de `IConfiguration` en .NET 8 para claves jerárquicas (`Jwt:Secret` → `Jwt__Secret`).

> ⚠️ **Advertencia de seguridad:** El valor anterior del secreto JWT (`***REMOVED-JWT-SECRET***`) estuvo expuesto en el historial de git. Debe considerarse **comprometido e inválido**. Róvalo inmediatamente en todos los entornos donde se haya desplegado.

> 🗑️ **Purga del historial de git:** El secreto anterior sigue accesible en el historial de commits. Consulta la sección "Purga del historial" más abajo.

### 3. Variables de entorno de producción

| Variable | Descripción | Ejemplo |
|----------|-------------|---------|
| `ConnectionStrings__Database` | Cadena de conexión a PostgreSQL | `Host=db;Port=5432;Database=mipyme;...` |
| `Jwt__Secret` | Clave de firma de JWT | valor generado con `openssl rand -base64 64` |
| `ASPNETCORE_ENVIRONMENT` | Entorno de ejecución | `Production` |

## Ejecución local

```bash
# Restaurar dependencias
dotnet restore

# Compilar
dotnet build MIPYME.sln

# Ejecutar la API (Development)
dotnet run --project src/Presentation.Api/Presentation.Api.csproj
```

La API expone Swagger en `/swagger` y health checks en `/health`.

## Ejecutar pruebas

```bash
dotnet test
```

## Purga del historial de git (secreto comprometido)

Si el repositorio es público o el secreto anterior llegó a remoto, el secreto debe purgarse del historial:

1. Instala [git-filter-repo](https://github.com/newren/git-filter-repo) o [BFG Repo-Cleaner](https://rtyley.github.io/bfg-repo-cleaner/).
2. Crea un archivo `replacements.txt` con el valor del secreto antiguo:
   ```
   ***REMOVED-JWT-SECRET***==>***REMOVED***
   ```
3. Ejecuta:
   ```bash
   git filter-repo --replace-text replacements.txt
   git push origin --force --all
   git push origin --force --tags
   ```
4. Notifica a todo el equipo que deben **re-clonar** el repositorio (los clones locales conservan el secreto viejo).

## Estructura de la solución

```
src/
├── Domain/                    # Entidades, Value Objects, eventos de dominio
├── Application/               # CQRS (MediatR), behaviors, validators
├── Infrastructure/            # EF Core, auth, autorización por permisos
├── Presentation.Api/          # ASP.NET Core Minimal APIs, Swagger
├── Presentation.App/          # Blazor WASM (panel admin/vendedor)
└── SharedKernel/              # Result<T>, Error, Entity, domain events
tests/
└── (vacío — pendiente Sprint 0)
```

## Roadmap

| Release | Foco | Estado |
|---------|------|--------|
| R0 | Hardening inicial (secretos, auth, tests, CI) | 🚧 En curso |
| R1 | Núcleo de negocio (dominio, CRUD, inventario) | ⏳ Pendiente |
| R2 | Auth completa (permisos, refresh tokens) | ⏳ Pendiente |
| R3 | Pedidos, pagos con pasarelas cubanas, frontend Blazor | ⏳ Pendiente |
| R4 | Reportes, notificaciones, CI/CD, Docker | ⏳ Pendiente |
