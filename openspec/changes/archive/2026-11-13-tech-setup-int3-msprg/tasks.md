# Tasks

## 1. Especificación Duradera del Microservicio MS-PRG

- [x] 1.1 Crear `openspec/specs/ms-prg/spec_prg.md` con el contexto, invariantes globales y las reglas de negocio de `ProgramaBeca` (`RN-PRG-01`), `BalancePresupuestarioPrograma` (fórmula de disponibilidad en tiempo real, invariante de no doble conteo y reserva atómica `ExecuteUpdateAsync` según `RN-PRG-03` y `C2`), `ReservaPresupuestoCupo`, `MovimientoPresupuesto`, `Donante` (`AporteDonante`, `AsignacionFondoPrograma` según `RN-PRG-02` y `RN-PRG-04`), `UniversidadAliada` (`Convenio`, `CarreraElegible`, `CostoMatriculaReferencia` versionado según `RN-PRG-05`) y `Convocatoria` (`ReglaElegibilidad`, `DocumentoRequerido`), verificando que el archivo exista y respete la terminología del glosario DDD en español.
- [x] 1.2 Actualizar `docs/organizacion_repositorio.md` (sección 2) si corresponde para reflejar el estado activo de `openspec/specs/ms-prg/spec_prg.md` y `Apps/services/ms-prg/`, verificando la consistencia de las rutas documentadas.

## 2. Estructura Hexagonal y Configuración de Solución en `Apps/services/ms-prg/`

- [x] 2.1 Crear el directorio `Apps/services/ms-prg/` y copiar/adaptar `.editorconfig`, `Directory.Build.props` (`net10.0`, `TreatWarningsAsErrors=true`, `RestorePackagesWithLockFile=true`) y `Directory.Packages.props` desde `Apps/services/ms-becas/`, verificando que los tres archivos existan en la raíz del microservicio.
- [x] 2.2 Crear la solución `Apps/services/ms-prg/Ascenso.Programas.sln` y los 4 proyectos de las capas hexagonales (`Ascenso.Programas.Domain`, `Ascenso.Programas.Application`, `Ascenso.Programas.Infrastructure`, `Ascenso.Programas.Api`) junto con sus carpetas base (`Comun`, `Enums`, `Excepciones`, `Ports`, `Transiciones`, `ValueObjects` en `Domain`; `Common`, `DTOs`, `Interfaces`, `UseCases` en `Application`; `Adapters`, `Configurations`, `Persistence` en `Infrastructure`; `Controllers`, `Program.cs`, `appsettings.json`, `Properties/launchSettings.json` en `Api`), verificando las referencias estrictas hacia adentro (`Api` → `Infrastructure`/`Application` → `Domain`).
- [x] 2.3 Crear en `Apps/services/ms-prg/tests/` los 4 proyectos de pruebas (`Ascenso.Programas.Domain.Tests`, `Ascenso.Programas.Application.Tests`, `Ascenso.Programas.ArchitectureTests`, `Ascenso.Programas.IntegrationTests`), registrarlos en `Ascenso.Programas.sln` dentro de la carpeta de solución `tests` y verificar con `dotnet sln Apps/services/ms-prg/Ascenso.Programas.sln list` que los 8 proyectos estén registrados.

## 3. Shared Kernel en `Domain/Comun/` y Pruebas de Arquitectura

- [x] 3.1 Implementar el *Shared Kernel* en `Ascenso.Programas.Domain/Comun/` y `Ascenso.Programas.Domain/Excepciones/`: `EntidadAuditable` (`EntidadAuditable<TId>`), `EntidadInmutable`, `AggregateRoot` (`AggregateRoot<TId>`), `ValueObject`, `IDomainEvent`, `DomainEvent` / `EventoDominio`, `UuidV7`, `Result` (`Result<TValue>`) y `DomainException`, y verificar su comportamiento mediante pruebas unitarias en `Ascenso.Programas.Domain.Tests` (`dotnet test Apps/services/ms-prg/tests/Ascenso.Programas.Domain.Tests/Ascenso.Programas.Domain.Tests.csproj`).
- [x] 3.2 Implementar en `Ascenso.Programas.ArchitectureTests/Arquitectura/` las pruebas `DependenciasEntreCapasTests.cs` y `DomainIndependenceTests.cs` (usando `NetArchTest.Rules` y reflexión) que validen: (a) que `Ascenso.Programas.Domain` no tenga dependencias externas (`EntityFrameworkCore`, `AspNetCore`, `MassTransit`, `Npgsql`, `Firebase`, `Keycloak`), (b) que se respete la dirección de dependencias entre `Domain`, `Application`, `Infrastructure` y `Api`, y (c) que ninguna entidad mutable en `Domain` tenga propiedades con setter público; verificar que pasen con `dotnet test Apps/services/ms-prg/tests/Ascenso.Programas.ArchitectureTests/Ascenso.Programas.ArchitectureTests.csproj`.
- [x] 3.3 Agregar las pruebas humo de ensamblado en `Ascenso.Programas.Application.Tests` (`EnsambladoAplicacionTests.cs`) y `Ascenso.Programas.IntegrationTests` (`EnsambladoApiTests.cs`) y verificar que ambos proyectos ejecuten sus pruebas en verde.

## 4. Bloqueo de Paquetes, Verificación Integral y Bitácora OpenSpec

- [x] 4.1 Ejecutar `dotnet restore Apps/services/ms-prg/Ascenso.Programas.sln` para generar los archivos `packages.lock.json` de los 8 proyectos y verificar que `dotnet restore Apps/services/ms-prg/Ascenso.Programas.sln --locked-mode` finalice con código `0`.
- [x] 4.2 Ejecutar `dotnet build Apps/services/ms-prg/Ascenso.Programas.sln --configuration Release --no-restore` y `dotnet test Apps/services/ms-prg/Ascenso.Programas.sln --configuration Release --no-build`, verificando `0` advertencias (`TreatWarningsAsErrors=true`), `0` errores y el 100 % de las pruebas en verde.
- [x] 4.3 Crear la bitácora obligatoria `openspec/changes/tech-setup-int3-msprg/log-2026-11-13-tech-setup-int3-msprg.md` (`log-YYYY-MM-DD-tech-setup-int3-msprg.md`) documentando el resumen de actividades, decisiones aplicadas y salidas de validación (`dotnet restore`, `dotnet build`, `dotnet test` y `openspec validate tech-setup-int3-msprg --strict`).

## Workflow follow-up

- Archivar el cambio con `/opsx:archive` o `openspec archive tech-setup-int3-msprg` una vez completadas todas las tareas `[x]` y verificada la bitácora.

