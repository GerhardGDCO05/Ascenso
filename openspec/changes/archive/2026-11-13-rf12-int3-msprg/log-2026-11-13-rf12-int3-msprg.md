# Bitácora de Ejecución Técnica: `rf12-int3-msprg`

- **Fecha:** 2026-11-13
- **Cambio OpenSpec:** `rf12-int3-msprg`
- **Requerimiento Funcional:** RF-12 — Control de Balance Presupuestario, Reservas Atómicas y Cierre de Períodos
- **Integrante Responsable:** Integrante 3 (`int3`)
- **Microservicio:** `MS-PRG` (`Ascenso.Programas`)

---

## 1. Resumen de Implementación por Capas

### 1.1. Capa de Dominio (`Ascenso.Programas.Domain`)
- **Enumeraciones (`Domain/Enums/`):**
  - `TipoMovimientoPresupuestario`: `AsignacionDonante`, `ReajusteDonante`, `ReservaCupo`, `ConfirmacionDesembolso`, `LiberacionReserva`, `CierrePeriodo`.
  - `EstadoReservaPresupuestaria`: `Reservada`, `Confirmada`, `Liberada`.
- **Entidades (`Domain/Programas/Entidades/`):**
  - `MovimientoPresupuestario` (`EntidadInmutable`, con `Guid.CreateVersion7()`): libro mayor append-only con `ProgramaBecaId`, `TipoMovimiento`, `Monto`, `Moneda`, `ReferenciaExternaId`, `PeriodoAcademicoId`, `Descripcion`, `FechaMovimiento`.
  - `ReservaPresupuestaria` (`EntidadAuditable<Guid>`, con `Guid.CreateVersion7()`): entidad auditable con métodos expresivos `Confirmar` y `Liberar` sin setters públicos.
- **Agregado Raíz (`Domain/Programas/Agregados/ProgramaBeca.cs`):**
  - Extendido con `PresupuestoReservado`, `CuposReservados`, propiedades calculadas en tiempo real `PresupuestoDisponible` (`Asignado - Reservado - Ejecutado`) y `CuposDisponibles` (`CupoTotal - CuposReservados - CuposOcupados`), registro automático de movimientos en `AsignarFondoDonante` y `ReajustarAsignacionFondo`, y métodos expresivos `ReservarPresupuesto`, `ConfirmarReservaPresupuesto`, `LiberarReservaPresupuesto` y `CerrarPeriodo`.
- **Eventos de Dominio y Excepciones:**
  - `PresupuestoProgramaReservadoDominioEvent`, `PresupuestoProgramaConfirmadoDominioEvent`, `ReservaPresupuestoLiberadaDominioEvent`, `PeriodoProgramaCerradoDominioEvent`.
  - `SinDisponibilidadPresupuestariaException` (`409 SIN_DISPONIBILIDAD_PRESUPUESTARIA`), `ReservaPresupuestariaNoEncontradaException` (`404 RESERVA_PRESUPUESTARIA_NO_ENCONTRADA`), `TransicionEstadoReservaInvalidaException` (`409 TRANSICION_ESTADO_RESERVA_INVALIDA`), `ReservaConParametrosDistintosException` (`409 RESERVA_CON_PARAMETROS_DISTINTOS`), `ReservasPendientesEnPeriodoException` (`409 RESERVAS_PENDIENTES_EN_PERIODO`), `EstadoProgramaInvalidoParaOperacionException` (`409 ESTADO_PROGRAMA_INVALIDO_PARA_OPERACION`).

### 1.2. Capa de Aplicación (`Ascenso.Programas.Application`)
- **Consultas (`[HU-12.1]`):** `ObtenerBalanceProgramaQuery` (`ObtenerBalanceProgramaQueryHandler`) y `ConsultarMovimientosPresupuestariosQuery` (`ConsultarMovimientosPresupuestariosQueryHandler`).
- **Comandos (`[HU-12.2]`):** `ReservarPresupuestoProgramaCommand`, `ConfirmarReservaPresupuestoCommand`, `LiberarReservaPresupuestoCommand` y `CerrarPeriodoProgramaCommand` con sus validadores FluentValidation, handlers, DTOs y eventos de integración.

### 1.3. Capa de Infraestructura (`Ascenso.Programas.Infrastructure`)
- **Persistencia EF Core:**
  - `MovimientoPresupuestarioConfiguracion` (`movimientos_presupuestarios`, `.ValueGeneratedNever()` en `Id`).
  - `ReservaPresupuestariaConfiguracion` (`reservas_presupuestarias`, `ConfigurarEntidadAuditable` con `.ValueGeneratedNever()` en `Id` y `Version` e índice único `ux_reserva_presupuestaria_programa_referencia`).
  - `ProgramaBecaRepository`: implementación de `ReservarPresupuestoAtomicoAsync` mediante `ExecuteUpdateAsync` condicional en transacción (`C2` / `RN-PRG-03`) garantizando cero sobreasignación bajo ráfagas concurrentes, y `ConsultarMovimientosPaginadosAsync`.
  - Migración EF Core generada: `20261114013339_ControlPresupuestarioYReservasAtomicas`.

### 1.4. Capa de API (`Ascenso.Programas.Api`)
- Nuevos endpoints expuestos en `ProgramasController`:
  - `GET /api/v1/programas/{programaId:guid}/balance`
  - `GET /api/v1/programas/{programaId:guid}/movimientos`
  - `POST /api/v1/programas/{programaId:guid}/reservas`
  - `POST /api/v1/programas/{programaId:guid}/reservas/{referenciaExternaId:guid}/confirmar`
  - `POST /api/v1/programas/{programaId:guid}/reservas/{referenciaExternaId:guid}/liberar`
  - `POST /api/v1/programas/{programaId:guid}/cerrar-periodo`

---

## 2. Verificación Automatizada y Calidad (CI)

- `dotnet restore Apps/services/ms-prg/Ascenso.Programas.sln` y `--locked-mode` → **OK (Exit Code 0)**.
- `dotnet format "Apps/services/ms-prg/Ascenso.Programas.sln" --verify-no-changes --no-restore --verbosity minimal` → **OK (Exit Code 0, 0 violaciones IDE1006)**.
- `dotnet build Apps/services/ms-prg/Ascenso.Programas.sln --configuration Release --no-restore` → **OK (0 Advertencias, 0 Errores)**.
- `dotnet test Apps/services/ms-prg/Ascenso.Programas.sln --configuration Release --no-build` → **131/131 pruebas en verde**:
  - `Ascenso.Programas.Domain.Tests`: **61/61**
  - `Ascenso.Programas.ArchitectureTests`: **9/9**
  - `Ascenso.Programas.Application.Tests`: **25/25**
  - `Ascenso.Programas.IntegrationTests`: **36/36** (incluyendo concurrencia `C2` con 10 hilos simultáneos contra PostgreSQL Testcontainers)
- `dotnet ef migrations has-pending-model-changes` → `No changes have been made to the model since the last migration.`
- `openspec validate rf12-int3-msprg --strict` → **`valid: true` (`0 issues`)**.
