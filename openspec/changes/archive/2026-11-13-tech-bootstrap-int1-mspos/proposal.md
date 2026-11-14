# Proposal: Fundación Arquitectónica Hexagonal del Microservicio MS-POS (`tech-bootstrap-int1-mspos`)

## Why

El microservicio `MS-POS` (`Apps/services/ms-postulaciones/`) solo cuenta con la estructura de proyectos creada en `tech-setup-int1-mspos`: todas sus carpetas de código están vacías (`.gitkeep`), no existe *Shared Kernel*, no hay manejo estandarizado de errores, no existe base de persistencia, no hay pruebas de arquitectura y no hay bloqueo de dependencias ni de versiones de paquetes.

Sin esta fundación, el **RF-01** (`[HU-1.1]` Registro de Datos Personales y Académicos Base) no puede implementarse respetando `docs/convenciones_c_y_ddd.md` ni validarse de forma automática en el pipeline de CI. Se aborda ahora, antes de iniciar las historias de negocio asignadas al **Integrante 1 (`int1`)** en `MS-POS`.

## What Changes

- **Configuración de solución y paquetes (`Apps/services/ms-postulaciones/`):** incorporar `.editorconfig`, `Directory.Build.props` (`net10.0`, `Nullable`, `TreatWarningsAsErrors`, `RestorePackagesWithLockFile`) y `Directory.Packages.props` con versiones centralizadas, copiando y adaptando los de `Apps/services/ms-prg/` (EF Core `10.0.15`, Npgsql `10.0.3`, FluentValidation `12.2.0`, MassTransit `8.5.12`, xunit, NSubstitute, FluentAssertions, NetArchTest, Testcontainers, WireMock).
- ***Shared Kernel* de dominio (`Ascenso.Postulaciones.Domain`):** implementar en `Comun/` las abstracciones base (`EntidadAuditable<TId>`, `EntidadInmutable<TId>`, `AggregateRoot<TId>`, `ValueObject`, `IDomainEvent`, `EventoDominio`, `UuidV7`, `Result`, `Result<TValue>`), en `Excepciones/` la jerarquía `DomainException` / `ExcepcionReglaNegocio` / `ExcepcionEntidadNoEncontrada` / `ExcepcionConflictoConcurrencia`, y en `Enums/` el helper `CodigoEnum` (`MAYUSCULAS_CON_GUIONES`).
- **Normalización de la estructura de carpetas:** alinear `Domain/{Comun,Enums,Excepciones,Interfaces,ValueObjects,Transiciones}`, `Application/{Common,DTOs,Interfaces,UseCases}`, `Infrastructure/{Adapters,Configurations,Persistence}`, `Api/{Controllers/v1,Controllers/Internal/v1,Middlewares}`, eliminando las carpetas vacías duplicadas en inglés (`Entities`, `Exceptions`, `Repositories`) y consolidando `Ports` en `Interfaces`.
- **Base de persistencia (`Ascenso.Postulaciones.Infrastructure`):** crear `PostulacionesDbContext` sobre el esquema PostgreSQL `postulaciones` con `IUnitOfWork` (`GuardarCambiosAsync` con `CancellationToken`, traducción de `DbUpdateConcurrencyException` a `ExcepcionConflictoConcurrencia`), `HasDefaultSchema`, `ApplyConfigurationsFromAssembly` e historial `__ef_migrations_history`, más `ConfiguracionPersistencia` (auditoría, `ValueGeneratedNever`, token `Version`, conversión de enums a `VARCHAR`). **No** se crean entidades ni migraciones: llegan con el primer agregado (`RF-01`).
- **Manejo estandarizado de errores en la API:** implementar `ManejadorExcepcionesDominio` (`DomainException` → `ProblemDetails` con `codigo`; `ValidationException` → `400` `ValidationProblemDetails`; `ArgumentException` → `400`) y registrarlo junto con `AddProblemDetails()` y `UseExceptionHandler()`.
- **Composición de dependencias:** agregar `DependencyInjection` en `Application` e `Infrastructure` y cablear `Program.cs` con `AgregarInfraestructuraPostulaciones(connectionString)` condicionado a `ConnectionStrings:Postulaciones`.
- **Proyectos de prueba faltantes:** crear `Ascenso.Postulaciones.Application.Tests`, `Ascenso.Postulaciones.ArchitectureTests` (reglas `NetArchTest`: independencia de `Domain`, dirección de dependencias y ausencia de setters públicos) y `Ascenso.Postulaciones.IntegrationTests` (pruebas humo de ensamblado y del modelo EF), y registrarlos en `Ascenso.Postulaciones.sln`.
- **Bloqueo y verificación:** generar `packages.lock.json` con `dotnet restore`, y validar `dotnet format --verify-no-changes`, `dotnet build --configuration Release` (`0` advertencias) y `dotnet test`.

**Fuera de alcance:** agregados, objetos de valor, casos de uso y controladores de negocio del `RF-01`; clientes HTTP internos; MassTransit/RabbitMQ y tablas de *outbox/inbox* (conforme a `DEC-08`); migraciones EF Core; cualquier modificación a otros microservicios.

## Capabilities

### New Capabilities

- `ms-pos/fundacion-servicio`: Fundación arquitectónica hexagonal del microservicio `MS-POS`, *Shared Kernel* táctico de dominio, contrato estandarizado de errores `ProblemDetails`, base de persistencia y suite de pruebas de arquitectura e independencia del dominio.

### Modified Capabilities

- Ninguna.

## Impact

- **Especificaciones OpenSpec:** se agrega el delta `openspec/changes/tech-bootstrap-int1-mspos/specs/ms-pos/fundacion-servicio/spec.md`, que al archivar se sincronizará a `openspec/specs/ms-pos/fundacion-servicio/spec.md`. La especificación duradera existente `openspec/specs/ms-pos/spec_pos.md` no cambia.
- **Código:** únicamente `Apps/services/ms-postulaciones/` (4 proyectos de código + 4 de pruebas). No se modifica `ms-becas`, `ms-prg` ni `ms-plt`.
- **Base de datos:** no hay migración ni cambio de esquema en este cambio.
- **CI/CD:** el pipeline `.github/workflows/ci-cd.yml` ya descubre `Ascenso.Postulaciones.sln`; el cambio solo agrega proyectos y pruebas que ejecutar.
