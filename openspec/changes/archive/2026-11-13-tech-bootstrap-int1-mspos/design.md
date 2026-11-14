# Design: Fundación Arquitectónica Hexagonal del Microservicio MS-POS (`tech-bootstrap-int1-mspos`)

## Context

Ver `proposal.md` — Why. El estado actual relevante para el diseño:

- `Apps/services/ms-postulaciones/` contiene la solución `Ascenso.Postulaciones.sln` con 4 proyectos de código (`Domain`, `Application`, `Infrastructure`, `Api`) y un único proyecto de pruebas (`tests/Ascenso.Postulaciones.Domain.Tests`). Todas las carpetas de código están vacías (`.gitkeep`).
- No existen `.editorconfig`, `Directory.Build.props` ni `Directory.Packages.props` en la raíz del microservicio, a diferencia de `ms-prg` y `ms-becas`.
- `Ascenso.Postulaciones.Api/Program.cs` solo registra CORS, `AddControllers()` y `AddOpenApi()`; no hay `ProblemDetails`, ni inyección de dependencias de capas, ni `partial class Program` para pruebas de integración.
- Estructura de carpetas heredada mezcla español e inglés: `Domain/{Comun,Enums,Excepciones,Ports,ValueObjects,Transiciones,Entities,Exceptions,Repositories}`.
- El microservicio de referencia más reciente es `ms-prg` (`Ascenso.Programas`): ya incorpora *Shared Kernel*, `ConfiguracionPersistencia`, middleware `ManejadorExcepcionesDominio`, `Directory.Build.props` con análisis estático y bloqueo de paquetes, y 4 proyectos de prueba.
- `DEC-08` elimina la obligatoriedad del patrón *Transactional Outbox*; `MS-POS` solo publicará eventos desacoplados hacia `MS-AUD` y `MS-PLT` cuando una HU lo exija.

## Goals / Non-Goals

**Goals:**

- Dejar `MS-POS` compilando y con pruebas verdes bajo las mismas reglas de calidad que `ms-prg` (`TreatWarningsAsErrors`, `dotnet format`, versiones centralizadas y *lock files*).
- Proveer el *Shared Kernel* táctico y las excepciones base que consumirán todos los agregados de `MS-POS`.
- Proveer el contexto de persistencia base del esquema `postulaciones` y la unidad de trabajo transaccional, sin modelar todavía ningún agregado.
- Proveer el contrato único de errores `ProblemDetails` y la composición de dependencias de las cuatro capas.
- Agregar la suite de pruebas de arquitectura que haga fallar el build ante violaciones hexagonales.

**Non-Goals:**

- Entidades, objetos de valor, enums de negocio, repositorios, comandos, consultas y controladores del `RF-01` (se implementan en `rf01-int1-mspos`).
- Migraciones EF Core y cualquier tabla de negocio.
- MassTransit/RabbitMQ, tablas `inbox_state`/`outbox_state`/`outbox_message`, clientes HTTP internos y autenticación Keycloak.
- Modificar `ms-prg`, `ms-becas`, `ms-plt` o el cambio activo `tech-bootstrap-int4-msbeca`.

## Decisions

### 1. Configuración raíz copiada desde `ms-prg`

- **Decisión:** tomar como fuente `Apps/services/ms-prg/{.editorconfig,Directory.Build.props,Directory.Packages.props}` y adaptar solo lo específico del contexto (`Ascenso.Postulaciones`), manteniendo:
  - `TargetFramework=net10.0`, `Nullable=enable`, `ImplicitUsings=enable`, `LangVersion=latest`.
  - `TreatWarningsAsErrors=true` con `WarningsNotAsErrors=$(WarningsNotAsErrors);NU1900`, `AnalysisLevel=latest-recommended`, `EnforceCodeStyleInBuild=true`, `GenerateDocumentationFile=true` y `NoWarn=$(NoWarn);CS1591;NU1902;NU1903`.
  - `RestorePackagesWithLockFile=true`, `RestoreLockedMode` y `ContinuousIntegrationBuild` condicionados a `CI`.
  - `Directory.Packages.props` con `ManagePackageVersionsCentrally=true` y las mismas versiones que `ms-prg` (EF Core `10.0.15`, `Microsoft.EntityFrameworkCore.Design` `10.0.15`, Npgsql `10.0.3`, FluentValidation `12.2.0`, MassTransit `8.5.12`, xunit `2.9.3`, `Microsoft.NET.Test.Sdk` `17.14.1`, `NetArchTest.Rules` `1.3.2`, `NSubstitute` `6.2.0`, `FluentAssertions` `7.2.2`, `coverlet.collector` `6.0.4`, `Microsoft.AspNetCore.Mvc.Testing` `10.0.15`, `Testcontainers.PostgreSql` `4.16.0`, `WireMock.Net` `2.30.0`).
- **Razón:** asegurar paridad de calidad y evitar divergencia de versiones entre microservicios; `ms-becas` es el origen histórico, pero `ms-prg` ya corrigió `NU1900`, analizadores y bloqueo de paquetes.
- **Alternativas consideradas:** copiar de `ms-becas` (descartado: configuración anterior y sin los ajustes de analizadores); no usar gestión centralizada (descartado: rompe la convención del repositorio).

### 2. *Shared Kernel* portado 1:1 desde `ms-prg`

- **Decisión:** crear en `Ascenso.Postulaciones.Domain/Comun/` los mismos tipos que `Ascenso.Programas.Domain/Comun/`: `EntidadAuditable<TId>`, `EntidadInmutable<TId>`, `AggregateRoot<TId>`, `ValueObject`, `IDomainEvent`, `EventoDominio`, `DomainEvent`, `UuidV7`, `Result`, `Result<TValue>`. En `Excepciones/`: `DomainException` (con `EstadoHttp` y `Codigo`), `ExcepcionReglaNegocio`, `ExcepcionEntidadNoEncontrada` y `ExcepcionConflictoConcurrencia`. En `Enums/CodigoEnum`: conversión `PascalCase → MAYUSCULAS_CON_GUIONES` y viceversa.
- **Detalle de `EntidadAuditable<TId>`:** `Id`, `FechaCreacion`, `UsuarioCreacionId`, `FechaUltimaModificacion`, `UsuarioModificacionId` y `Version` (token de concurrencia), con métodos `MarcarComoModificado`/`MarcarComoCreado` y reloj UTC. Todos los setters son privados o `protected`; el `Id` se genera con `UuidV7.Nuevo()` en las fábricas de dominio.
- **Razón:** los agregados de admisión (`Postulante`, `BorradorPostulacion`, `Postulacion`) comparten exactamente las mismas invariantes técnicas; duplicar el kernel garantiza independencia de despliegue entre microservicios.
- **Alternativas consideradas:** crear un paquete NuGet compartido (descartado: acopla versiones entre microservicios y no existe infraestructura de paquetes privados en el repositorio).

### 3. Normalización de carpetas y consolidación de puertos

- **Decisión:** dejar `Domain/{Comun,Enums,Excepciones,Interfaces,Transiciones}`; eliminar las carpetas vacías `Entities`, `Exceptions`, `Repositories` y `Ports` (consolidada en `Interfaces`, igual que `ms-prg`) y `ValueObjects` de la raíz, ya que los objetos de valor se ubican por módulo en `Domain/{Modulo}/ObjetosValor/` (patrón `Donantes/ObjetosValor` de `ms-prg`). Mantener `Domain/Transiciones` para la máquina de estados de `Postulacion` prevista en `spec_pos.md`. En `Application` conservar `Common`, `DTOs`, `Interfaces`, `UseCases`; en `Infrastructure`, `Adapters`, `Configurations`, `Persistence`; en `Api`, crear `Controllers/v1`, `Controllers/Internal/v1` y `Middlewares`.
- **Razón:** la convención del repositorio exige nomenclatura de dominio en español y `Domain/{Modulo}/Agregados|Entidades`; las carpetas en inglés quedaron del esqueleto inicial y están vacías, por lo que eliminarlas no rompe código.
- **Alternativas consideradas:** conservar ambas estructuras (descartado: ambigüedad para futuros contribuyentes y riesgo de namespaces inconsistentes).

### 4. `PostulacionesDbContext` base sin agregados ni migraciones

- **Decisión:** implementar `Ascenso.Postulaciones.Infrastructure/Persistence/PostulacionesDbContext` con `public const string Esquema = "postulaciones"`, `HasDefaultSchema(Esquema)`, `ApplyConfigurationsFromAssembly(...)`, implementación de `IUnitOfWork.GuardarCambiosAsync(CancellationToken)` que traduce `DbUpdateConcurrencyException` a `ExcepcionConflictoConcurrencia`, y `ConfiguracionPersistencia` (auditoría `snake_case`, `ValueGeneratedNever()` en `Id` y `Version`, `ComoCodigoEnum`, `ComoDinero`). El historial se configura con `MigrationsHistoryTable("__ef_migrations_history", Esquema)` en `DependencyInjection`. No se crean entidades, `DbSet`, configuraciones ni migraciones.
- **Razón:** el primer agregado (`Postulante`) llega con `RF-01`; adelantar tablas vacías generaría migraciones sin valor. La traducción de unicidad por restricción se agregará en `RF-01`, cuando exista el índice `ux_postulante_numero_documento`.
- **Alternativas consideradas:** replicar el *Transactional Outbox* de `ms-prg` (descartado: `DEC-08` lo hace opcional y `RF-01` no emite eventos de integración); crear la migración base vacía ahora (descartado: `dotnet ef migrations has-pending-model-changes` quedaría sin efecto y ensuciaría el historial).

### 5. Middleware de excepciones con contrato idéntico a `ms-prg`

- **Decisión:** `Ascenso.Postulaciones.Api/Middlewares/ManejadorExcepcionesDominio : IExceptionHandler` traduce: `DomainException` → `ProblemDetails` con `Title = "Error de regla de negocio"`, `Status = EstadoHttp`, `Detail` y extensión `codigo`; `ValidationException` → `400` `ValidationProblemDetails` agrupado por `PropertyName` con `codigo = primer ErrorCode` o `VALIDACION_INVALIDA`; `ArgumentException` → `400` con `codigo = SOLICITUD_INVALIDA`. Se registra con `AddProblemDetails()` + `AddExceptionHandler<>()` y `app.UseExceptionHandler()`. Las excepciones no controladas se resuelven como `500` por el manejador por defecto de `ProblemDetails`.
- **Razón:** homogeneidad de contrato para el frontend y los consumidores internos; `RF-01` añadirá `DependenciaNoDisponibleException` como `DomainException` con `EstadoHttp = 503`, sin tocar el middleware.
- **Alternativas consideradas:** un middleware `try/catch` propio (descartado: duplica comportamiento y no se integra con el pipeline de `IExceptionHandler`).

### 6. Composición de dependencias condicional a base de datos

- **Decisión:** `Application.DependencyInjection.AgregarAplicacionPostulaciones()` registra `AddValidatorsFromAssembly` y los futuros handlers; `Infrastructure.DependencyInjection.AgregarInfraestructuraPostulaciones(cadenaConexion)` valida la cadena, registra `AddDbContext<PostulacionesDbContext>` con `UseNpgsql(... MigrationsHistoryTable(...))` y `IUnitOfWork`. `Program.cs` invoca la infraestructura solo si `ConnectionStrings:Postulaciones` no está vacía, y agrega `public partial class Program { }` para `WebApplicationFactory`. No se ejecuta `MigrateAsync` hasta que exista la primera migración (`RF-01`).
- **Razón:** permitir `dotnet build/test` y el arranque local sin PostgreSQL, y dejar preparado el punto exacto donde `RF-01` habilitará migraciones automáticas en `Development`.
- **Alternativas consideradas:** exigir la cadena siempre (descartado: rompe pruebas de arquitectura/unitarias y el arranque sin infraestructura).

### 7. Proyectos de prueba y su contenido

- **Decisión:** crear `tests/Ascenso.Postulaciones.Application.Tests` (prueba humo de ensamblado), `tests/Ascenso.Postulaciones.ArchitectureTests` (`DependenciasEntreCapasTests`, `DomainIndependenceTests`, `EncapsulamientoTests` con `NetArchTest` + reflexión) y `tests/Ascenso.Postulaciones.IntegrationTests` (`EnsambladoApiTests` y `ModeloPersistenciaPostulacionesTests` que valida `GetDefaultSchema() == "postulaciones"` y `ValueGenerated.Never` sin conectarse a PostgreSQL). Mover la validación de arquitectura desde `Domain.Tests` a `ArchitectureTests`. Registrar los tres proyectos en la carpeta de solución `tests`.
- **Razón:** `ms-prg` ya separa responsabilidades de prueba; las pruebas de arquitectura deben fallar el build, no convivir con pruebas unitarias del kernel.
- **Alternativas consideradas:** mantener todo en `Domain.Tests` (descartado: mezcla responsabilidades y complica el filtrado en CI).

## Risks / Trade-offs

- **[Risk] `TreatWarningsAsErrors=true` + `GenerateDocumentationFile=true` puede romper la compilación por XML docs faltantes** → **Mitigación:** conservar `NoWarn=CS1591` de `ms-prg` y documentar con `///` los tipos públicos del *Shared Kernel*.
- **[Risk] `RestorePackagesWithLockFile=true` obliga a regenerar `packages.lock.json` en cada cambio de paquete** → **Mitigación:** ejecutar `dotnet restore` como parte de las tareas y versionar los *lock files* de los 8 proyectos.
- **[Risk] El `DbContext` sin entidades ni migraciones puede quedar en un estado inconsistente al agregar `RF-01`** → **Mitigación:** la tarea de `RF-01` creará la migración inicial con el primer agregado y habilitará `MigrateAsync` en `Development`.
- **[Risk] Diferencias entre `ms-prg` y `ms-postulaciones` en el nombre del ensamblado (`Ascenso.Postulaciones` vs `Ascenso.Programas`)** → **Mitigación:** adaptar `namespace` y referencias en todos los archivos copiados; verificar con `dotnet build` y las pruebas de arquitectura.

## Migration Plan

1. Incorporar configuración raíz y *Shared Kernel* (tareas 1 y 2).
2. Normalizar carpetas y crear proyectos de prueba (tarea 3).
3. Implementar persistencia base, middleware y composición de dependencias (tareas 4 y 5).
4. Ejecutar `dotnet restore`, `dotnet format --verify-no-changes`, `dotnet build --configuration Release` y `dotnet test --configuration Release`; generar la bitácora y verificar `openspec validate --strict`.
5. **Rollback:** revertir el cambio deja `MS-POS` en su esqueleto actual; no hay migraciones ni datos que deshacer.
