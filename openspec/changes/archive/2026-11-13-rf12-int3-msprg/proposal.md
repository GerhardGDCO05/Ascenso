## Why

El microservicio **MS-PRG (`Ascenso.Programas`)** ya cuenta con la gestión de Donantes y Aportes (**RF-13**), Convenios y Costos de Universidades Aliadas (**RF-14**) y la Configuración y Activación de Programas de Becas (**RF-11**). Para completar el ciclo operativo financiero de los programas y habilitar la adjudicación segura de becas desde **MS-PST** y la liquidación de pagos desde **MS-FIN**, es indispensable implementar el **RF-12 (Control de Balance Presupuestario, Reservas Atómicas y Cierre de Períodos)** asignado al **Integrante 3 (`int3`)**.

Sin esta capacidad, el sistema carece de:
1. Un libro mayor (*ledger*) inmutable de auditoría financiera (`MovimientoPresupuestario`) que respalde cada variación de saldo y cupo por programa y período académico (`[HU-12.1]`).
2. Un mecanismo transaccional atómico en base de datos para la **Concurrencia Crítica `C2` (`RN-PRG-03`)** que reserve simultáneamente un cupo y su monto presupuestario estimado (`ExecuteUpdateAsync` condicional sobre `programas_beca`) sin riesgo de *race conditions* ni sobreasignación bajo peticiones concurrentes (`[HU-12.2]`).
3. Operaciones formales de ciclo de vida de reservas (`ConfirmarReservaPresupuestoCommand`, `LiberarReservaPresupuestoCommand`) y de cierre contable de período (`CerrarPeriodoProgramaCommand`) con trazabilidad e idempotencia por `referenciaExternaId`.

## What Changes

Se incorpora el módulo de **Control de Balance Presupuestario, Reservas Atómicas y Cierre de Períodos (RF-12)** dentro de `Apps/services/ms-prg/` extendiendo el agregado `ProgramaBeca` y su infraestructura de persistencia:

1. **[HU-12.1] Consulta de Balance Presupuestario en Tiempo Real y Auditoría de Movimientos:**
   - **Entidad inmutable `MovimientoPresupuestario` (`EntidadInmutable`):** Registro append-only con `Id` (UUIDv7 con `.ValueGeneratedNever()`), `ProgramaBecaId`, `TipoMovimiento` (`TipoMovimientoPresupuestario`: `AsignacionDonante`, `ReajusteDonante`, `ReservaCupo`, `ConfirmacionDesembolso`, `LiberacionReserva`, `CierrePeriodo`), `Monto` (`decimal(18,2)`), `Moneda` (`USD`), `ReferenciaExternaId` (`Guid?`, ej. `postulacionId` o `becaId`), `PeriodoAcademicoId` (`Guid?`), `Descripcion` y `FechaMovimiento`.
   - **Trazabilidad en asignaciones y reajustes de donantes:** Al asignar o reajustar fondos de donantes en `ProgramaBeca`, se registra automáticamente el `MovimientoPresupuestario` correspondiente (`AsignacionDonante` / `ReajusteDonante`).
   - **Cálculo de balance en tiempo real en `ProgramaBeca`:** Exposición e invariantes de `PresupuestoTotalAsignado`, `PresupuestoReservado`, `PresupuestoEjecutado`, `PresupuestoDisponible` (`Asignado - Reservado - Ejecutado`), `CupoTotal`, `CuposReservados`, `CuposOcupados` y `CuposDisponibles` (`CupoTotal - CuposReservados - CuposOcupados`).
   - **Consultas (`Queries`):**
     - `ObtenerBalanceProgramaQuery` (`GET /api/v1/programas/{id}/balance`): Devuelve el desglose financiero y de cupos en tiempo real del programa.
     - `ConsultarMovimientosPresupuestariosQuery` (`GET /api/v1/programas/{id}/movimientos`): Devuelve el historial paginado y filtrable por `tipoMovimiento`, `periodoAcademicoId` y `referenciaExternaId`.

2. **[HU-12.2] Reserva Atómica de Cupo y Saldo (`C2`), Confirmación/Liberación y Cierre de Período:**
   - **Entidad `ReservaPresupuestaria` (`EntidadAuditable<Guid>`):** Registra cada reserva individual con `Id` (UUIDv7 con `.ValueGeneratedNever()`), `ProgramaBecaId`, `ReferenciaExternaId` (índice único compuesto `(ProgramaBecaId, ReferenciaExternaId)` para garantizar idempotencia), `PeriodoAcademicoId`, `Monto` (`> 0`), `Moneda` (`USD`), `Estado` (`EstadoReservaPresupuestaria`: `Reservada`, `Confirmada`, `Liberada`), `MotivoLiberacion` y marcas de tiempo de confirmación/liberación.
   - **Concurrencia Crítica `C2` (`RN-PRG-03` - `ReservarPresupuestoProgramaCommand` / `POST /api/v1/programas/{id}/reservas`):**
     - Verifica idempotencia por `(ProgramaBecaId, ReferenciaExternaId)`. Si la reserva ya existe con el mismo monto y estado `Reservada`, devuelve el resultado existente sin duplicar cargos; si existe con parámetros incompatibles o ya procesada, o si se solicita duplicar ilegalmente, controla el conflicto.
     - Ejecuta una actualización condicional atómica en PostgreSQL mediante EF Core (`ExecuteUpdateAsync`) con la condición: `estado IN ('ACTIVO', 'CONFIGURADO') AND (cupos_ocupados + cupos_reservados < cupo_total) AND (presupuesto_total_asignado - presupuesto_reservado - presupuesto_ejecutado >= monto)`, incrementando `presupuesto_reservado += monto`, `cupos_reservados += 1` y `version += 1` en una única sentencia SQL dentro de una transacción junto con la inserción de `ReservaPresupuestaria` y `MovimientoPresupuestario` (`ReservaCupo`).
     - Si la actualización atómica afecta `0` filas y el programa existe en estado válido, rechaza con `409 Conflict` (`SIN_DISPONIBILIDAD_PRESUPUESTARIA`), garantizando cero sobreasignación bajo ráfagas concurrentes, y emite `PresupuestoProgramaReservadoDominioEvent`.
   - **Ciclo de vida de la reserva:**
     - `ConfirmarReservaPresupuestoCommand` (`POST /api/v1/programas/{id}/reservas/{referenciaExternaId}/confirmar`): Transiciona la reserva de `Reservada` a `Confirmada`, convierte `PresupuestoReservado -> PresupuestoEjecutado` (`PresupuestoReservado -= monto`, `PresupuestoEjecutado += monto`) y `CuposReservados -> CuposOcupados` (`CuposReservados -= 1`, `CuposOcupados += 1`), registra `MovimientoPresupuestario` (`ConfirmacionDesembolso`) y emite `PresupuestoProgramaConfirmadoDominioEvent`.
     - `LiberarReservaPresupuestoCommand` (`POST /api/v1/programas/{id}/reservas/{referenciaExternaId}/liberar`): Transiciona la reserva de `Reservada` a `Liberada`, revierte `PresupuestoReservado -= monto` y `CuposReservados -= 1` devolviendo el saldo y el cupo a disponible, registra `MovimientoPresupuestario` (`LiberacionReserva`) y emite `ReservaPresupuestoLiberadaDominioEvent`.
   - **Cierre de período / programa (`CerrarPeriodoProgramaCommand` / `POST /api/v1/programas/{id}/cerrar-periodo`):**
     - Verifica que no existan reservas pendientes en estado `Reservada` para el período a cerrar (o para el programa si se solicita cierre definitivo -> `409 RESERVAS_PENDIENTES_EN_PERIODO`), consolida los saldos remanentes no ejecutados registrando un `MovimientoPresupuestario` de auditoría con tipo `CierrePeriodo`, permite opcionalmente transicionar el programa a `Cerrado` cuando finaliza su vigencia/período final, y emite `PeriodoProgramaCerradoDominioEvent`.

## Capabilities

### New Capabilities
- `ms-prg/presupuesto`: Control de balance presupuestario y cupos en tiempo real, auditoría inmutable de movimientos presupuestarios (`[HU-12.1]`), reserva atómica concurrente `C2` (`RN-PRG-03`), confirmación/liberación de reservas por `referenciaExternaId` y cierre contable de período (`[HU-12.2]`).

### Modified Capabilities
- *(Ninguna modificación de requisitos sobre capacidades existentes; extiende el agregado `ProgramaBeca` con las operaciones financieras del RF-12).*

## Impact

- **Dominio (`Ascenso.Programas.Domain`):**
  - Nuevas entidades en `Domain/Programas/Entidades/`: `MovimientoPresupuestario` (`EntidadInmutable`) y `ReservaPresupuestaria` (`EntidadAuditable<Guid>`).
  - Nuevas enumeraciones en `Domain/Programas/Enumeraciones/`: `TipoMovimientoPresupuestario`, `EstadoReservaPresupuestaria`.
  - Nuevos eventos de dominio en `Domain/Programas/Eventos/`: `PresupuestoProgramaReservadoDominioEvent`, `PresupuestoProgramaConfirmadoDominioEvent`, `ReservaPresupuestoLiberadaDominioEvent`, `PeriodoProgramaCerradoDominioEvent`.
  - Nuevas excepciones/códigos de error en `Domain/Programas/Excepciones/` y métodos de dominio en `ProgramaBeca`.
- **Aplicación (`Ascenso.Programas.Application`):**
  - Comandos, validadores y handlers para `ReservarPresupuestoProgramaCommand`, `ConfirmarReservaPresupuestoCommand`, `LiberarReservaPresupuestoCommand` y `CerrarPeriodoProgramaCommand`.
  - Queries y handlers para `ObtenerBalanceProgramaQuery` y `ConsultarMovimientosPresupuestariosQuery`.
- **Infraestructura (`Ascenso.Programas.Infrastructure`):**
  - Configuraciones EF Core `MovimientoPresupuestarioConfiguracion` (`movimientos_presupuestarios`, `.ValueGeneratedNever()` en `Id`) y `ReservaPresupuestariaConfiguracion` (`reservas_presupuestarias`, `ConfigurarEntidadAuditable` con `.ValueGeneratedNever()` en `Id` y `Version`).
  - Nuevas columnas en `programas_beca` (`presupuesto_reservado`, `cupos_reservados`), método atómico `IntentarReservarPresupuestoAtomicoAsync` en `ProgramaBecaRepository` y nueva migración EF Core.
- **API (`Ascenso.Programas.Api`):**
  - Nuevos endpoints REST en `ProgramasController`: `GET {id:guid}/balance`, `GET {id:guid}/movimientos`, `POST {id:guid}/reservas`, `POST {id:guid}/reservas/{referenciaExternaId:guid}/confirmar`, `POST {id:guid}/reservas/{referenciaExternaId:guid}/liberar` y `POST {id:guid}/cerrar-periodo`.
- **Pruebas (`tests/`):**
  - Cobertura unitaria en `Domain.Tests` y `Application.Tests`, validación en `ArchitectureTests` y pruebas de integración en `IntegrationTests` incluyendo prueba de concurrencia `C2` con múltiples solicitudes simultáneas contra el último cupo/saldo disponible.
