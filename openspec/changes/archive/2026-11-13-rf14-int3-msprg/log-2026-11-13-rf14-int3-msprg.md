# Registro de Auditoría — Cambio: rf14-int3-msprg

- **Fecha de ejecución:** 13 de noviembre de 2026 (UTC-4)
- **Cambio:** `rf14-int3-msprg`
- **Requerimiento Funcional:** `RF-14` — Convenios con Universidades Aliadas, Carreras Elegibles y Versionado Histórico de Costos de Matrícula (`[HU-14.1]`, `[HU-14.2]`, `RN-PRG-05`)
- **Integrante:** 3 (`int3`)
- **Microservicio:** `ms-prg` (`MS-PRG` · `Ascenso.Programas`)

## Resumen de Actividades por Capa

1. **Capa de Dominio (`Ascenso.Programas.Domain`):**
   - `Enums/`: `EstadoUniversidad` (`Activa`, `Inactiva`), `EstadoConvenio` (`Vigente`, `Vencido`, `Suspendido`) y `EstadoVersionCosto` (`Vigente`, `Historico`).
   - `Universidades/ObjetosValor/`: `ContactoEnlace` y `CuentaBancariaInstitucional` (inmutables, validados al instanciar).
   - `Universidades/Entidades/`: `Convenio`, `CarreraElegible` y `CostoMatriculaReferencia` (heredan de `EntidadAuditable`, UUIDv7 con `UuidV7.Nuevo()`, sin setters públicos y campos privados `_camelCase`).
   - `Universidades/Agregados/UniversidadAliada.cs`: Agregado raíz con `CrearConConvenioInicial`, `RegistrarNuevoConvenio`, `MarcarConvenioVencido`, `ValidarConvenioVigenteParaNuevasPostulaciones` (`422 CONVENIO_VENCIDO` conforme a `RN-PRG-05`), `AgregarCarreraElegible` (`409 CODIGO_CARRERA_DUPLICADO`) y `ActualizarCostoMatricula` (transición `Vigente -> Historico` y versión `N+1`).
   - `Universidades/Eventos/`: `ConvenioUniversidadRegistradoDominioEvent`, `ConvenioUniversidadVencidoDominioEvent`, `CostoMatriculaActualizadoDominioEvent`.
   - `Excepciones/`: `CodigoInstitucionalDuplicadoException` (`409`), `UniversidadAliadaNoEncontradaException` (`404`), `ConvenioNoEncontradoException` (`404`), `FechasConvenioInvalidasException` (`400`), `DocumentoConvenioPdfRequeridoException` (`400`), `ConvenioUniversidadVencidoException` (`422`, `CONVENIO_VENCIDO`), `CodigoCarreraDuplicadoException` (`409`), `CarreraElegibleNoEncontradaException` (`404`), `MontoMatriculaInvalidoException` (`400`).
   - `Interfaces/IUniversidadAliadaRepository.cs`.

2. **Capa de Aplicación (`Ascenso.Programas.Application`):**
   - `Universidades/DTOs/UniversidadDtos.cs`, `Universidades/UniversidadMapeador.cs` y eventos de integración (`ConvenioUniversidadRegistrado`, `ConvenioUniversidadVencido`, `CostoMatriculaActualizado`).
   - Comandos, validadores y handlers CQRS-Lite: `RegistrarUniversidadConvenioCommand`, `RegistrarNuevoConvenioCommand`, `MarcarConvenioVencidoCommand`, `AgregarCarreraElegibleCommand`, `ActualizarCostoMatriculaCommand`.
   - Consultas CQRS-Lite: `ConsultarUniversidadesQuery`, `ObtenerUniversidadPorIdQuery`, `ConsultarHistorialCostosCarreraQuery`.
   - Registro de handlers en `DependencyInjection.cs`.

3. **Capa de Infraestructura (`Ascenso.Programas.Infrastructure`):**
   - `Configuraciones/`: `UniversidadAliadaConfiguracion`, `ConvenioConfiguracion`, `CarreraElegibleConfiguracion`, `CostoMatriculaReferenciaConfiguracion` utilizando `ConfiguracionPersistencia.ConfigurarEntidadAuditable` (`.ValueGeneratedNever()` en todos los `Id` UUIDv7 y en `Version`).
   - `ProgramasDbContext.cs`: Registro de los 4 `DbSet` y traducción de índices únicos PostgreSQL `23505`.
   - `Repositorios/UniversidadAliadaRepository.cs`: Carga con tracking en `ObtenerPorIdAsync` y `AsNoTracking()` en consultas de lectura.
   - `Persistence/Migrations/`: Generación de la migración `ConveniosUniversidadesYCostos`.

4. **Capa API (`Ascenso.Programas.Api`):**
   - `Controllers/v1/UniversidadesController.cs` bajo `/api/v1/universidades` (8 endpoints para `[HU-14.1]` y `[HU-14.2]`).

5. **Pruebas y Gobernanza:**
   - `Domain.Tests/Universidades/UniversidadAliadaTests.cs`
   - `Application.Tests/Universidades/UniversidadesCommandAndQueryHandlerTests.cs`
   - `IntegrationTests/Persistencia/ModeloPersistenciaProgramasTests.cs` ampliado para validar `ValueGenerated.Never` y el Change Tracker en versionado de costos `1 -> 2`.
   - Roadmaps actualizados con `[x] RF-14` en `openspec/roadmaps/por_integrante.md` y `openspec/roadmaps/por_microservicio.md`.

## Validación Ejecutada (Salidas Reales)

### 1. Restauración (`dotnet restore` y `--locked-mode`)
```text
> dotnet restore Apps/services/ms-prg/Ascenso.Programas.sln
  Todos los proyectos están actualizados para la restauración.

> dotnet restore Apps/services/ms-prg/Ascenso.Programas.sln --locked-mode
  Exit code 0 (los 8 proyectos restaurados en modo bloqueado).
```

### 2. Formato CI (`dotnet format --verify-no-changes`)
```text
> dotnet format "Apps/services/ms-prg/Ascenso.Programas.sln" --verify-no-changes --no-restore --verbosity minimal
  Exit code 0 (sin violaciones de formato ni IDE1006).
```

### 3. Compilación en Release (`dotnet build --configuration Release --no-restore`)
```text
Compilación correcta.
    0 Advertencia(s)
    0 Errores

Tiempo transcurrido 00:00:23.66
```

### 4. Ejecución integral de pruebas (`dotnet test --configuration Release --no-build`)
```text
Correctas! - Con error:     0, Superado:    50, Omitido:     0, Total:    50, Duración: 113 ms - Ascenso.Programas.Domain.Tests.dll (net10.0)
Correctas! - Con error:     0, Superado:     9, Omitido:     0, Total:     9, Duración: 924 ms - Ascenso.Programas.ArchitectureTests.dll (net10.0)
Correctas! - Con error:     0, Superado:    18, Omitido:     0, Total:    18, Duración: 431 ms - Ascenso.Programas.Application.Tests.dll (net10.0)
Correctas! - Con error:     0, Superado:    33, Omitido:     0, Total:    33, Duración: 2 s - Ascenso.Programas.IntegrationTests.dll (net10.0)
```
Total: **110/110 pruebas superadas**, 0 errores, 0 omitidas.

### 5. Verificación de migración EF Core (`has-pending-model-changes`)
```text
> dotnet ef migrations has-pending-model-changes --configuration Release --no-build
No changes have been made to the model since the last migration.
```

## Estado

Todas las tareas (`1.1` a `5.2`) han sido completadas y verificadas en verde. El cambio `rf14-int3-msprg` está listo para revisión y posterior archivado con `/opsx:archive rf14-int3-msprg`.
