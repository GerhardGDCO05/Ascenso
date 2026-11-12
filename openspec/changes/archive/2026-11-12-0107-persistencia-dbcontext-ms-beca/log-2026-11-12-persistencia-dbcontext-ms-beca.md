# Registro de Auditoría — Task 4.1: contexto de persistencia de MS-BECA

- **Fecha y hora de ejecución:** 12 de noviembre de 2026, 01:07 (UTC-4)
- **Rama local:** `Gerhard`
- **Alcance:** implementar la tarea `4.1` de `openspec/changes/bootstrap-ms-beca/tasks.md`.
- **Trabajo local:** sin `git push` ni modificación de ramas remotas.
- **Dependencias externas:** ninguna nueva descargada; se centralizó la versión de EF Core Relational ya presente en la caché local.

## 1. Objetivo

Crear `BecasDbContext` con los `DbSet` del agregado de otorgamiento, las proyecciones locales y la idempotencia, y verificar que el contexto se construye con la cadena de PostgreSQL y con el esquema `becas`.

## 2. Cambios realizados

### Contexto de persistencia

- `Apps/services/ms-becas/Ascenso.Becas.Infrastructure/Persistence/BecasDbContext.cs`
  - Contexto `sealed` que hereda de `DbContext`.
  - Esquema por defecto `becas` mediante `HasDefaultSchema`.
  - `DbSet<Beca>` → `Becas`.
  - `DbSet<OfertaBeca>` → `OfertasBeca`.
  - `DbSet<CompromisoBecario>` → `CompromisosBecarios`.
  - `DbSet<HistorialEstadoBeca>` → `HistorialEstadosBeca`.
  - `DbSet<ProgramaBecaRef>` → `ProgramasBecaRef`.
  - `DbSet<CostoMatriculaRef>` → `CostosMatriculaRef`.
  - `DbSet<ConvenioRef>` → `ConveniosRef`.
  - `DbSet<IdempotencyRequest>` → `SolicitudesIdempotencia`.
  - `ApplyConfigurationsFromAssembly` para las configuraciones de `4.2`–`4.7`.

### Configuración mínima del modelo

- Claves primarias de `HistorialEstadoBeca`, `ProgramaBecaRef`, `CostoMatriculaRef` y `ConvenioRef`.
- `OwnsOne` con propiedades explícitas para `Beca.CoberturaAplicada`, `OfertaBeca.CondicionesSnapshot`, `ProgramaBecaRef.PoliticaCobertura` y `ProgramaBecaRef.CondicionesPermanencia`.
- La configuración completa de nombres `snake_case`, tipos PostgreSQL, índices, `CHECK` y trigger queda para `4.2`–`4.5`.

### Modelos de proyección e idempotencia

- `Persistence/Modelos/ProgramaBecaRef.cs`: programa, política de cobertura, condiciones de permanencia, acumulación, duración, estado, `UltimoEventoId` y marcas de actualización.
- `Persistence/Modelos/CostoMatriculaRef.cs`: carrera, periodo, monto y moneda de referencia, versión, vigencia, estado, `UltimoEventoId` y marcas de actualización.
- `Persistence/Modelos/ConvenioRef.cs`: universidad, convenio, estado, fecha de fin, `UltimoEventoId` y marcas de actualización.
- `Persistence/Modelos/IdempotencyRequest.cs`: endpoint, clave, hash del payload, estado, código y cuerpo de respuesta, recurso, creación y expiración.

### Dependencias

- `Directory.Packages.props`: versión central `Microsoft.EntityFrameworkCore.Relational` `10.0.15`.
- `Ascenso.Becas.Infrastructure.csproj`: referencia directa a `Microsoft.EntityFrameworkCore.Relational`.
- Efecto: `Ascenso.Becas.Api` y `Ascenso.Becas.IntegrationTests` dejan de resolver `10.0.4` y se elimina la advertencia `MSB3277`.

### Pruebas agregadas

- `tests/Ascenso.Becas.IntegrationTests/Persistencia/BecasDbContextTests.cs`
  - El contexto se construye con `UseNpgsql` y conserva la cadena de conexión.
  - Expone los ocho `DbSet` del agregado, las proyecciones y la idempotencia.
  - El modelo contiene los ocho tipos y usa el esquema por defecto `becas`.

## 3. Decisiones de diseño

- **Persistencia en Infrastructure:** el contexto y los modelos de proyección viven en `Ascenso.Becas.Infrastructure`; `Domain` permanece como C# puro.
- **Modelo mínimo:** `4.1` solo garantiza que el modelo EF se construye; la semántica relacional completa se distribuye en `4.2`–`4.7`.
- **`OwnsOne` explícito:** los objetos de valor no tienen setters públicos y sus constructores no pueden bindearse por convención; se mapean propiedad por propiedad.
- **Claves de proyecciones:** se usa la identidad natural de cada proyección (`ProgramaBecaId`, `CostoMatriculaReferenciaId`, `UniversidadAliadaId`) y `Id` para el historial.
- **Versión alineada:** la referencia directa a EF Core Relational evita que la resolución transitiva de Npgsql baje la versión en los proyectos consumidores.

## 4. Validación ejecutada

```text
dotnet restore Ascenso.Becas.sln --ignore-failed-sources -p:NuGetAudit=false
→ 8 proyectos restaurados; lock files actualizados

dotnet build Ascenso.Becas.sln -c Release --no-restore
→ 0 advertencias, 0 errores
→ MSB3277 eliminada

dotnet test Ascenso.Becas.sln -c Release --no-build
→ Ascenso.Becas.Domain.Tests        136/136 correctas
→ Ascenso.Becas.Application.Tests     1/1   correcta
→ Ascenso.Becas.ArchitectureTests     8/8   correctas
→ Ascenso.Becas.IntegrationTests      4/4   correctas
→ Total                            149/149 correctas

dotnet format Ascenso.Becas.sln --verify-no-changes --no-restore
→ correcto (exit 0)
```

La restauración emitió `NU1900` durante una ejecución previa por falta de acceso al índice de vulnerabilidades de NuGet; la restauración final se ejecutó con `NuGetAudit=false` y no afectó la compilación ni las pruebas.

## 5. Resultado

- **Antes:** 29 de 114 tareas completadas.
- **Después:** 30 de 114 tareas completadas.
- **Tarea cerrada:** `4.1`.
- **Roadmaps:** no se marcó ningún RF de MS-BEC porque la tarea es infraestructura de persistencia y aún no existe funcionalidad de negocio completa.
- **Capas:** `Ascenso.Becas.Domain` no incorporó EF Core, ASP.NET, MassTransit, Firebase, Keycloak ni Npgsql.
- **Estado local:** sin commit y sin `git push`; `ms-pos` y `ms-plt` intactos.

## 6. Siguiente bloque

Continuar con `4.2` (nombres y tipos PostgreSQL), `4.3` (índices únicos parciales) y `4.4` (`CHECK` constraints) antes de las tablas de proyección, la idempotencia y las migraciones.
