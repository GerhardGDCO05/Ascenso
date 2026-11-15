# Registro de Auditoría — Cambio: tech-bootstrap-int1-mspos

- **Fecha de ejecución:** 13 de noviembre de 2026 (UTC-4)
- **Cambio:** `tech-bootstrap-int1-mspos`
- **Tipo:** `tech` (fundación arquitectónica hexagonal del microservicio)
- **Integrante:** 1 (`int1`)
- **Microservicio:** `ms-pos` (`MS-POS` · `Ascenso.Postulaciones`)
- **SDK .NET:** `10.0.403` (`global.json`: `10.0.403`, `rollForward: latestPatch`)

## Resumen de Actividades

1. **Configuración radicular y paquetes:**
   - Se copiaron y adaptaron desde `Apps/services/ms-prg/` los archivos `.editorconfig` (`charset = utf-8`, `end_of_line = lf`), `Directory.Build.props` (`net10.0`, `Nullable`, `TreatWarningsAsErrors`, `AnalysisLevel`, `RestorePackagesWithLockFile`) y `Directory.Packages.props` (`ManagePackageVersionsCentrally`).
   - Se reescribieron los 5 `.csproj` existentes sin versiones explícitas y se generó un `packages.lock.json` por proyecto.

2. **Shared Kernel táctico (`Domain/`):**
   - `Domain/Comun/`: `EntidadAuditable`, `EntidadInmutable`, `AggregateRoot` (`AggregateRoot<TId>`), `ValueObject`, `IDomainEvent`, `EventoDominio`, `DomainEvent`, `UuidV7` (`Guid.CreateVersion7()`), `Result` (`Result<TValue>`).
   - `Domain/Excepciones/`: `DomainException`, `ExcepcionReglaNegocio` (422 por defecto), `ExcepcionEntidadNoEncontrada` (404), `ExcepcionConflictoConcurrencia` (409).
   - `Domain/Enums/CodigoEnum.cs`: conversión bidireccional PascalCase ↔ `MAYUSCULAS_CON_GUIONES`.
   - `Domain/Interfaces/IUnitOfWork.cs`: consolidación de los antiguos `Ports/` y `Repositories/`; se eliminaron las carpetas vacías `Entities`, `Exceptions`, `Repositories`, `Ports` y `ValueObjects` de la raíz. Los objetos de valor vivirán por módulo en `Domain/{Modulo}/ObjetosValor/`.

3. **Normalización y proyectos de prueba:**
   - Estructura objetivo: `Domain/{Comun,Enums,Excepciones,Interfaces,Transiciones}`, `Application/{Common,DTOs,Interfaces,UseCases}`, `Infrastructure/{Adapters,Configurations,Persistence}`, `Api/{Controllers/v1,Controllers/Internal/v1,Middlewares}`.
   - Se crearon `Ascenso.Postulaciones.Application.Tests`, `Ascenso.Postulaciones.ArchitectureTests` y `Ascenso.Postulaciones.IntegrationTests`, registrados en la carpeta de solución `tests` (8 proyectos en total).
   - Pruebas de arquitectura con `NetArchTest.Rules` y reflexión: `DependenciasEntreCapasTests`, `DomainIndependenceTests` y `EncapsulamientoTests` (sin setters públicos ni colecciones mutables expuestas).

4. **Persistencia base e inyección de dependencias:**
   - `PostulacionesDbContext` (esquema `postulaciones`, `HasDefaultSchema`, `ApplyConfigurationsFromAssembly`, `IUnitOfWork.GuardarCambiosAsync` traduciendo `DbUpdateConcurrencyException` a `ExcepcionConflictoConcurrencia`).
   - `PostulacionesDbContextFactory` para herramientas de EF Core y `ConfiguracionPersistencia` (`snake_case`, `ValueGeneratedNever()` en `Id` y `Version`, `ComoCodigoEnum`, `ComoDinero`).
   - `AgregarAplicacionPostulaciones` (`AddValidatorsFromAssembly`) y `AgregarInfraestructuraPostulaciones` (`AddDbContext`, `MigrationsHistoryTable("__ef_migrations_history", "postulaciones")`, registro de `IUnitOfWork`).

5. **Contrato de errores y composición de la API:**
   - `ManejadorExcepcionesDominio` (`IExceptionHandler`): `DomainException` → `ProblemDetails` con extensión `codigo`; `ValidationException` → `400 ValidationProblemDetails` por campo; `ArgumentException` → `400` con `codigo = SOLICITUD_INVALIDA`.
   - `Program.cs` con `AddProblemDetails()`, `AddExceptionHandler<ManejadorExcepcionesDominio>()`, `UseExceptionHandler()`, DI condicionada a `ConnectionStrings:Postulaciones` y `public partial class Program { }`; `appsettings.json` con la cadena de conexión vacía.
   - Pruebas sobre `DefaultHttpContext`, arranque con `WebApplicationFactory<Program>` sin base de datos y pruebas humo de ensamblado.

## Decisiones Aplicadas

- Se eliminó `ValueObjects/` de la raíz del dominio (los objetos de valor van por módulo en `Domain/{Modulo}/ObjetosValor/`).
- Se consolidaron `Ports/` y `Repositories/` en `Domain/Interfaces/`.
- El contexto de persistencia no traduce aún violaciones de unicidad de PostgreSQL: no existen agregados; se incorporará con RF-01.
- No se configura mensajería (MassTransit) en la fundación: se añadirá cuando un caso de uso lo requiera.
- No se ejecutan migraciones en el arranque: el `Program` no llama a `Database.MigrateAsync()` para permitir el arranque sin PostgreSQL.

## Validación Ejecutada (Salidas Reales)

### 1. Proyectos de la solución (`dotnet sln list`)
```text
Proyectos
---------
Ascenso.Postulaciones.Api\Ascenso.Postulaciones.Api.csproj
Ascenso.Postulaciones.Application\Ascenso.Postulaciones.Application.csproj
Ascenso.Postulaciones.Domain\Ascenso.Postulaciones.Domain.csproj
Ascenso.Postulaciones.Infrastructure\Ascenso.Postulaciones.Infrastructure.csproj
tests\Ascenso.Postulaciones.Application.Tests\Ascenso.Postulaciones.Application.Tests.csproj
tests\Ascenso.Postulaciones.ArchitectureTests\Ascenso.Postulaciones.ArchitectureTests.csproj
tests\Ascenso.Postulaciones.Domain.Tests\Ascenso.Postulaciones.Domain.Tests.csproj
tests\Ascenso.Postulaciones.IntegrationTests\Ascenso.Postulaciones.IntegrationTests.csproj
```

### 2. Restauración bloqueada (`dotnet restore --locked-mode`)
```text
  Se ha restaurado ... (8 proyectos)
RESTORE_EXIT=0
```

### 3. Formato (`dotnet format --verify-no-changes --no-restore`)
```text
FORMAT_VERIFY_EXIT=0
```

### 4. Compilación en Release (`dotnet build --configuration Release`)
```text
Compilación correcta.
    0 Advertencia(s)
    0 Errores

Tiempo transcurrido 00:00:07.00
BUILD_RELEASE_EXIT=0
```

### 5. Pruebas en Release (`dotnet test --configuration Release --no-build`)
```text
Correctas! - Con error:     0, Superado:     1, Omitido:     0, Total:     1, Duración: 117 ms - Ascenso.Postulaciones.Application.Tests.dll (net10.0)
Correctas! - Con error:     0, Superado:    39, Omitido:     0, Total:    39, Duración: 158 ms - Ascenso.Postulaciones.Domain.Tests.dll (net10.0)
Correctas! - Con error:     0, Superado:    10, Omitido:     0, Total:    10, Duración: 459 ms - Ascenso.Postulaciones.ArchitectureTests.dll (net10.0)
Correctas! - Con error:     0, Superado:     7, Omitido:     0, Total:     7, Duración: 1 s - Ascenso.Postulaciones.IntegrationTests.dll (net10.0)
TEST_RELEASE_EXIT=0
```
Total: **57/57 pruebas superadas**, 0 fallos, 0 omitidas.

### 6. Validación OpenSpec
```text
openspec validate tech-bootstrap-int1-mspos --strict
Change 'tech-bootstrap-int1-mspos' is valid

openspec status --change tech-bootstrap-int1-mspos
Progress: 4/4 artifacts complete
[x] proposal
[x] specs
[x] design
[x] tasks
All planning artifacts complete!
```

## Estado

Todas las tareas (`1.1` a `6.2`) completadas y verificadas. El cambio `tech-bootstrap-int1-mspos` queda listo para revisión y posterior archivado; habilita el cambio de negocio `rf01-int1-mspos` (HU 1.1 · RF-01).
