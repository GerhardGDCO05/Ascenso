# Ascenso

Base del proyecto Ascenso. Incluye el entorno de desarrollo, el frontend en React y los microservicios en .NET organizados bajo `Apps/services`.

## Organización del repositorio

- `Apps/services/<microservicio>/`: código y pruebas de cada microservicio.
- `openspec/specs/<microservicio>/`: especificaciones duraderas por microservicio.
- `openspec/changes/`: cambios activos; históricos en `openspec/changes/archive/<fecha-hora>/`.
- `docs/`: contexto general (PRD, DDD, convenciones y organización).
- `openspec/roadmaps/`: planificación general por integrante y microservicio.
- `.github/workflows/ci-cd.yml`: descubre automáticamente las soluciones de `Apps/services/` y ejecuta restore bloqueado, build, formato y pruebas.

La estructura completa y las reglas para incorporar nuevos microservicios están en [`docs/organizacion_repositorio.md`](docs/organizacion_repositorio.md). El trabajo activo se desarrolla en `ms-beca`; `ms-pos` y `ms-plt` son contexto de otros equipos.

## Requisitos

| Herramienta | Versión |
| :--- | :--- |
| .NET SDK | 10.0.403 (fijado en `global.json`) |
| Entity Framework Core Tools | 10.0.14 |
| Node.js | 24.19.0 |
| npm | 11.17.0 |
| Docker | 29.7.2 |
| Docker Compose | 5.5.0 |

## Configuración de `ms-becas`

La configuración del microservicio está dentro de `Apps/services/ms-becas/` para no imponer propiedades ni versiones a los demás microservicios:

- `Directory.Build.props`: `net10.0`, nullable, warnings-as-errors, analizadores, build determinista y lock files de NuGet.
- `Directory.Packages.props`: versiones centralizadas de todos los paquetes.
- `.editorconfig`: estilo C#, nombres y severidad de analizadores.
- `packages.lock.json`: lock files por proyecto. El CI restaura con `--locked-mode`.

En la raíz del repositorio están `global.json`, que fija el SDK `10.0.403`, y `.gitignore`, que excluye artefactos de build, entornos y archivos de usuario.

## Estructura de `ms-becas`

```text
Apps/services/ms-becas/
├── Ascenso.Becas.Domain/            # C# puro, sin dependencias externas
├── Ascenso.Becas.Application/       # Casos de uso, DTOs y puertos
├── Ascenso.Becas.Infrastructure/    # Adaptadores EF Core, mensajería, seguridad
├── Ascenso.Becas.Api/               # Controllers, middlewares y DI
└── tests/
    ├── Ascenso.Becas.Domain.Tests/        # Pruebas de dominio
    ├── Ascenso.Becas.Application.Tests/   # Pruebas de casos de uso y handlers
    ├── Ascenso.Becas.ArchitectureTests/   # Reglas de dependencia entre capas
    └── Ascenso.Becas.IntegrationTests/    # Persistencia, mensajería y API
```

## Comandos de desarrollo

Desde la raíz del repositorio:

```bash
# Restaurar dependencias
dotnet restore Apps/services/ms-becas/Ascenso.Becas.sln

# Restaurar en modo CI, usando los lock files
dotnet restore Apps/services/ms-becas/Ascenso.Becas.sln --locked-mode

# Compilar con warnings como errores
dotnet build Apps/services/ms-becas/Ascenso.Becas.sln \
  --configuration Release --warnaserror

# Ejecutar todas las pruebas
dotnet test Apps/services/ms-becas/Ascenso.Becas.sln \
  --configuration Release --no-build --no-restore

# Verificar formato
dotnet format Apps/services/ms-becas/Ascenso.Becas.sln \
  --verify-no-changes --no-restore
```

## Herramientas

### `dotnet-ef`

`dotnet-ef` se instala como herramienta global y se agrega `~/.dotnet/tools` al `PATH`:

```bash
dotnet tool install --global dotnet-ef --version 10.0.14
export PATH="$PATH:$HOME/.dotnet/tools"
dotnet ef --version
```

En las terminales nuevas, `~/.bashrc` y `~/.profile` cargan el SDK y `~/.dotnet/tools` automáticamente.

### Base de datos local

Las migraciones de EF Core viven en
`Apps/services/ms-becas/Ascenso.Becas.Infrastructure/Persistence/Migraciones/`.
Para crearlas o aplicarlas se usa `BecasDbContext` con la cadena local
`Host=localhost;Port=5432;Database=ascenso_becas;Username=postgres;Password=postgres`:

```bash
# Crear la base si no existe
PGPASSWORD=postgres psql -h localhost -p 5432 -U postgres \
  -c "CREATE DATABASE ascenso_becas;"

# Aplicar todas las migraciones
dotnet ef database update \
  --project Apps/services/ms-becas/Ascenso.Becas.Infrastructure \
  --startup-project Apps/services/ms-becas/Ascenso.Becas.Infrastructure \
  --context BecasDbContext

# Generar el script SQL equivalente
dotnet ef migrations script \
  --project Apps/services/ms-becas/Ascenso.Becas.Infrastructure \
  --startup-project Apps/services/ms-becas/Ascenso.Becas.Infrastructure \
  --context BecasDbContext \
  -o /tmp/migracion-becas.sql
```

La cadena puede sustituirse con la variable `ConnectionStrings__Becas`.
La migración crea el esquema `becas`; Hangfire crea su propio esquema
`hangfire` al arrancar la API.

## Licencias de terceros

- **MassTransit 8.5.12**: Apache-2.0.
- **PdfSharpCore 1.3.67**: MIT. Arrastra `SixLabors.ImageSharp 1.0.4` (Apache-2.0), que tiene avisos de seguridad conocidos y no tiene parche en la línea 1.x. Se documenta la excepción y se revisará cuando PdfSharpCore actualice esa dependencia.

Las versiones están centralizadas en `Apps/services/ms-becas/Directory.Packages.props`.

## Documentación

- [`docs/Explicacion_del_DDD_V7.md`](docs/Explicacion_del_DDD_V7.md): especificación del dominio.
- [`docs/convenciones_c_y_ddd.md`](docs/convenciones_c_y_ddd.md): convenciones C#, DDD y arquitectura hexagonal.
- [`docs/organizacion_repositorio.md`](docs/organizacion_repositorio.md): organización general y reglas por microservicio.
- [`openspec/specs/ms-beca/spec_beca.md`](openspec/specs/ms-beca/spec_beca.md): especificación duradera de MS-BECA.
