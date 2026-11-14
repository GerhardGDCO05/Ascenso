# Bitácora de Ejecución: `rf11-int3-msprg` (2026-11-13)

- **Cambio OpenSpec:** `openspec/changes/rf11-int3-msprg`
- **Requerimiento:** `RF-11` — Registro y Configuración de Programas de Becas (`[HU-11.1]`, `[HU-11.2]`, `[HU-11.3]`, `RN-PRG-01`, `RN-PRG-02`)
- **Microservicio:** `MS-PRG` (`Apps/services/ms-prg/`)
- **Integrante:** Integrante 3 (`int3`)

## Resumen de Implementación

1. **Capa de Dominio (`Ascenso.Programas.Domain`):**
   - Enumeraciones: `EstadoProgramaBeca`, `ModalidadCobertura`, `EstadoAsignacionFondo`, `FrecuenciaReporteAcademico`, `ConsecuenciaIncumplimiento`.
   - Objetos de Valor inmutables por composición: `PoliticaCobertura`, `CriteriosElegibilidadPrograma`, `CondicionesPermanencia`.
   - Agregado `ProgramaBeca` (`UuidV7.Nuevo()`, sin setters públicos) y entidad interna `AsignacionFondoPrograma`.
   - Extensión en `Donante`: `ComprometerSaldoParaPrograma` y `ReintegrarSaldoDePrograma` (`RN-PRG-02`).
   - Eventos de dominio: `ProgramaBecaCreadoDominioEvent`, `PresupuestoProgramaAsignadoDominioEvent`, `ProgramaBecaConfiguradoDominioEvent`.
   - Excepciones de negocio (`400`, `404`, `409`, `422`) y puerto `IProgramaBecaRepository`.
2. **Capa de Aplicación (`Ascenso.Programas.Application`):**
   - Comandos, handlers y validadores FluentValidation: `CrearProgramaBecaCommand`, `AsignarPresupuestoDonantesCommand`, `ReajustarPresupuestoDonanteCommand`, `ConfigurarCriteriosElegibilidadCommand`.
   - Consultas: `ConsultarProgramasQuery`, `ObtenerProgramaPorIdQuery`.
   - DTOs, `ProgramaBecaMapeador` y eventos de integración (`ProgramaBecaCreado`, `PresupuestoProgramaAsignado`, `ProgramaBecaConfigurado`).
3. **Capa de Infraestructura (`Ascenso.Programas.Infrastructure`):**
   - Configuraciones EF Core `ProgramaBecaConfiguracion` y `AsignacionFondoProgramaConfiguracion` con `.ValueGeneratedNever()` en `Id` y `Version`.
   - `ProgramaBecaRepository`, actualización de `DonanteRepository.TieneFondosComprometidosEnProgramaVigenteAsync` y migración `RegistroYConfiguracionProgramasBeca`.
4. **Capa de Presentación (`Ascenso.Programas.Api`):**
   - `ProgramasController` en `/api/v1/programas`.

## Evidencia Real de Verificación

- **Restore con modo bloqueado:**
  - Comando: `dotnet restore Apps/services/ms-prg/Ascenso.Programas.sln --locked-mode`
  - Resultado: Exitoso (`exit code 0`).
- **Compilación Release:**
  - Comando: `dotnet build Apps/services/ms-prg/Ascenso.Programas.sln --configuration Release --no-restore --disable-build-servers -m:1`
  - Resultado: `Compilación correcta. 0 Advertencia(s), 0 Errores`.
- **Migración EF Core:**
  - Comando: `dotnet ef migrations has-pending-model-changes --project Apps/services/ms-prg/Ascenso.Programas.Infrastructure --startup-project Apps/services/ms-prg/Ascenso.Programas.Api --no-build --configuration Release`
  - Resultado: `No changes have been made to the model since the last migration.` (`exit code 0`).
- **Formato CI:**
  - Comando: `dotnet format "Apps/services/ms-prg/Ascenso.Programas.sln" --verify-no-changes --no-restore --verbosity minimal`
  - Resultado: `exit code 0` (0 violaciones).
- **Suite de Pruebas (`Release`):**
  - Comando: `dotnet test Apps/services/ms-prg/Ascenso.Programas.sln --configuration Release --no-build`
  - Resultados:
    - `Ascenso.Programas.Domain.Tests`: **59/59** superadas.
    - `Ascenso.Programas.ArchitectureTests`: **9/9** superadas.
    - `Ascenso.Programas.Application.Tests`: **24/24** superadas.
    - `Ascenso.Programas.IntegrationTests`: **34/34** superadas.
    - **Total:** **126/126** superadas (`0` fallidas, `0` omitidas).
