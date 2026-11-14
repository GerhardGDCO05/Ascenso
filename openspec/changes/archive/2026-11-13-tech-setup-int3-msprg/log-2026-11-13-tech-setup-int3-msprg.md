# Registro de Auditoría — Cambio: tech-setup-int3-msprg

- **Fecha de ejecución:** 13 de noviembre de 2026 (UTC-4)
- **Cambio:** `tech-setup-int3-msprg`
- **Tipo:** `tech` (inicialización de microservicio, especificación duradera, Shared Kernel y pruebas de arquitectura)
- **Integrante:** 3 (`int3`)
- **Microservicio:** `ms-prg` (`MS-PRG` · `Ascenso.Programas`)
- **SDK .NET:** `10.0.404` (`global.json`: `10.0.403`, `rollForward: latestPatch`)

## Resumen de Actividades

1. **Especificación duradera y documentación:**
   - Se creó `openspec/specs/ms-prg/spec_prg.md` con el contexto del bounded context (`BC-02` y `BC-06` parcial), invariantes globales y reglas de negocio (`RN-PRG-01` a `RN-PRG-05`, `C2`), cubriendo `ProgramaBeca`, `BalancePresupuestarioPrograma` (fórmula de disponibilidad y reserva atómica `ExecuteUpdateAsync`), `ReservaPresupuestoCupo`, `MovimientoPresupuesto`, `Donante`, `AporteDonante`, `AsignacionFondoPrograma`, `UniversidadAliada`, `Convenio`, `CarreraElegible`, `CostoMatriculaReferencia` versionado, `Convocatoria`, `ReglaElegibilidad` y `DocumentoRequerido`.
   - Se actualizó `docs/organizacion_repositorio.md` (sección 2) reflejando el estado activo de `openspec/specs/ms-prg/spec_prg.md`.

2. **Estructura Hexagonal en `Apps/services/ms-prg/`:**
   - Se creó `Ascenso.Programas.sln` junto con `.editorconfig`, `Directory.Build.props` (`net10.0`, `TreatWarningsAsErrors=true`, `RestorePackagesWithLockFile=true`) y `Directory.Packages.props` (`ManagePackageVersionsCentrally=true`).
   - Se crearon los 4 proyectos principales (`Ascenso.Programas.Domain`, `Ascenso.Programas.Application`, `Ascenso.Programas.Infrastructure`, `Ascenso.Programas.Api`) y los 4 proyectos de prueba en `tests/` (`Domain.Tests`, `Application.Tests`, `ArchitectureTests`, `IntegrationTests`).

3. **Shared Kernel y Pruebas de Arquitectura:**
   - Se implementó `Domain/Comun/`: `EntidadAuditable` (`EntidadAuditable<TId>`), `EntidadInmutable`, `AggregateRoot` (`AggregateRoot<TId>`), `ValueObject`, `IDomainEvent`, `DomainEvent`, `EventoDominio`, `UuidV7`, `Result` (`Result<TValue>`), y `Domain/Excepciones/DomainException.cs`.
   - Se implementaron las pruebas unitarias del *Shared Kernel* en `Ascenso.Programas.Domain.Tests` (20 pruebas), las pruebas humo de ensamblado en `Application.Tests` (1 prueba) e `IntegrationTests` (1 prueba), y las pruebas de arquitectura con `NetArchTest.Rules` y reflexión en `ArchitectureTests` (9 pruebas) verificando la independencia de `Domain`, la dirección de dependencias hexagonales y la ausencia de setters públicos en entidades mutables.

## Validación Ejecutada (Salidas Reales)

### 1. Listado de proyectos de la solución (`dotnet sln list`)
```text
Proyectos
---------
Ascenso.Programas.Api\Ascenso.Programas.Api.csproj
Ascenso.Programas.Application\Ascenso.Programas.Application.csproj
Ascenso.Programas.Domain\Ascenso.Programas.Domain.csproj
Ascenso.Programas.Infrastructure\Ascenso.Programas.Infrastructure.csproj
tests\Ascenso.Programas.Application.Tests\Ascenso.Programas.Application.Tests.csproj
tests\Ascenso.Programas.ArchitectureTests\Ascenso.Programas.ArchitectureTests.csproj
tests\Ascenso.Programas.Domain.Tests\Ascenso.Programas.Domain.Tests.csproj
tests\Ascenso.Programas.IntegrationTests\Ascenso.Programas.IntegrationTests.csproj
```

### 2. Restauración y verificación de lockfiles (`dotnet restore` y `--locked-mode`)
```text
dotnet restore Apps/services/ms-prg/Ascenso.Programas.sln
  -> Los 8 proyectos restaurados correctamente (packages.lock.json generados).

dotnet restore Apps/services/ms-prg/Ascenso.Programas.sln --locked-mode
  -> Exit code 0. Los 8 proyectos restaurados en modo bloqueado.
```

### 3. Compilación en Release (`dotnet build --configuration Release --no-restore`)
```text
Compilación correcta.
    0 Advertencia(s)
    0 Errores

Tiempo transcurrido 00:00:34.79
```

### 4. Ejecución de pruebas (`dotnet test --configuration Release --no-build`)
```text
Correctas! - Con error:     0, Superado:    20, Omitido:     0, Total:    20, Duración: 253 ms - Ascenso.Programas.Domain.Tests.dll (net10.0)
Correctas! - Con error:     0, Superado:     1, Omitido:     0, Total:     1, Duración: 856 ms - Ascenso.Programas.Application.Tests.dll (net10.0)
Correctas! - Con error:     0, Superado:     9, Omitido:     0, Total:     9, Duración: 759 ms - Ascenso.Programas.ArchitectureTests.dll (net10.0)
Correctas! - Con error:     0, Superado:     1, Omitido:     0, Total:     1, Duración: 598 ms - Ascenso.Programas.IntegrationTests.dll (net10.0)
```
Total: **31/31 pruebas superadas**, 0 fallos, 0 omitidas.

## Estado

Todas las tareas (`1.1` a `4.3`) completadas y verificadas. El cambio `tech-setup-int3-msprg` está listo para revisión y posterior archivado con `/opsx:archive`.

