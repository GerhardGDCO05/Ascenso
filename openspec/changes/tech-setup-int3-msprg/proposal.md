# Proposal

## Why

El microservicio `MS-PRG` (`Ascenso.Programas`), responsable de los Contextos Delimitados **BC-02** (*Programas, Fondos y Convenios Aliados*) y **BC-06 parcial** (*Control de Presupuesto y Cupos*) asignados al **Integrante 3 (`int3`)** (`RF-11`, `RF-12`, `RF-13`, `RF-14`, más el soporte a `RF-02` y `RF-23`), aún no cuenta con su especificación duradera (`openspec/specs/ms-prg/spec_prg.md`) ni con su solución .NET 10 bajo `Apps/services/ms-prg/`. Inicializar esta base arquitectónica hexagonal junto con su *Shared Kernel* y pruebas de arquitectura es indispensable para habilitar el desarrollo aislado de los agregados de programas, presupuesto, donantes, convenios universitarios y convocatorias, así como para permitir la integración con `MS-POS` y `MS-BEC`.

## What Changes

- **Especificación duradera del microservicio (`openspec/specs/ms-prg/spec_prg.md`):** Documentar el contexto, invariantes y reglas de negocio (`RN-PRG-01` a `RN-PRG-05`, concurrencia `C1` y `C2`) para `ProgramaBeca`, `BalancePresupuestarioPrograma` (fórmula única de disponibilidad en tiempo real, invariante de no doble conteo y reserva atómica mediante `ExecuteUpdateAsync`), `ReservaPresupuestoCupo`, `MovimientoPresupuesto`, `Donante` (`AporteDonante`, `AsignacionFondoPrograma`), `UniversidadAliada` (`Convenio`, `CarreraElegible`, `CostoMatriculaReferencia` con versionado histórico inmutable) y `Convocatoria` (`ReglaElegibilidad`, `DocumentoRequerido`).
- **Estructura Hexagonal en `Apps/services/ms-prg/`:** Crear la solución `Ascenso.Programas.sln` copiando y adaptando `.editorconfig`, `Directory.Build.props` y `Directory.Packages.props` desde `Apps/services/ms-becas/`, e incorporando:
  - 4 proyectos principales: `Ascenso.Programas.Domain`, `Ascenso.Programas.Application`, `Ascenso.Programas.Infrastructure` y `Ascenso.Programas.Api`.
  - 4 proyectos de pruebas en `tests/`: `Ascenso.Programas.Domain.Tests`, `Ascenso.Programas.Application.Tests`, `Ascenso.Programas.ArchitectureTests` y `Ascenso.Programas.IntegrationTests`.
- **Shared Kernel en `Ascenso.Programas.Domain/Comun/` y `Excepciones/`:** Implementar las abstracciones base en C# puro (`EntidadAuditable`, `EntidadInmutable`, `AggregateRoot`, `ValueObject`, `IDomainEvent`, `DomainEvent` / `EventoDominio`, `UuidV7`, `Result` y `DomainException`) respetando el encapsulamiento estricto (sin setters públicos en propiedades mutables) y la nomenclatura DDD en español.
- **Pruebas de Arquitectura (`Ascenso.Programas.ArchitectureTests`) y Pruebas Unitarias del Shared Kernel (`Ascenso.Programas.Domain.Tests`):** Validar automáticamente con `NetArchTest.Rules` y reflexión que `Ascenso.Programas.Domain` sea 100 % independiente de frameworks externos (EF Core, ASP.NET Core, MassTransit, Npgsql, Firebase, Keycloak), que las dependencias entre capas respeten la dirección hexagonal (`Api` → `Infrastructure`/`Application` → `Domain`) y que ninguna entidad mutable en `Domain` exponga setters públicos.
- **Bloqueo de paquetes y verificación de CI:** Generar los archivos `packages.lock.json` con `dotnet restore`, verificar compilación limpia con `TreatWarningsAsErrors=true` y comprobar la ejecución en verde de `dotnet test`.

**Fuera de alcance de este cambio:** Implementación de los agregados de negocio específicos de `RF-11` a `RF-14`, casos de uso de aplicación, `ProgramasDbContext`, migraciones de EF Core o controladores REST (los cuales se abordarán en sus respectivos cambios por requerimiento funcional).

## Capabilities

### New Capabilities

- `ms-prg/fundacion-servicio`: Fundación arquitectónica hexagonal del microservicio `MS-PRG` (`Ascenso.Programas`), especificación duradera de dominio (`spec_prg.md`), *Shared Kernel* táctico (`Domain/Comun/` y `Domain/Excepciones/`) y suite de pruebas de arquitectura e independencia de dominio.

### Modified Capabilities

- Ninguna.

## Impact

- **Especificaciones OpenSpec:** Creación de `openspec/specs/ms-prg/spec_prg.md` y del delta de especificación en `openspec/changes/tech-setup-int3-msprg/specs/ms-prg/fundacion-servicio/spec.md`.
- **Código nuevo:** Nueva carpeta `Apps/services/ms-prg/` con `Ascenso.Programas.sln`, 4 proyectos de código fuente y 4 proyectos de pruebas. No modifica ningún archivo de `ms-becas` ni de `ms-postulaciones`.
- **CI/CD:** El pipeline existente `.github/workflows/ci-cd.yml` descubrirá automáticamente `Apps/services/ms-prg/Ascenso.Programas.sln` y ejecutará restore, build y test para `MS-PRG`.

