# Tasks

## 1. Dominio (`Ascenso.Programas.Domain/Programas/`)

- [x] 1.1 `[HU-12.1]` `[HU-12.2]` Implementar las enumeraciones `TipoMovimientoPresupuestario` (`AsignacionDonante`, `ReajusteDonante`, `ReservaCupo`, `ConfirmacionDesembolso`, `LiberacionReserva`, `CierrePeriodo`) y `EstadoReservaPresupuestaria` (`Reservada`, `Confirmada`, `Liberada`) en `Domain/Programas/Enumeraciones/`.
- [x] 1.2 `[HU-12.1]` `[HU-12.2]` Implementar la entidad inmutable `MovimientoPresupuestario` (`EntidadInmutable`, con `Guid.CreateVersion7()`) y la entidad auditable `ReservaPresupuestaria` (`EntidadAuditable<Guid>`, con `Guid.CreateVersion7()`) sin setters públicos en `Domain/Programas/Entidades/`.
- [x] 1.3 `[HU-12.1]` `[HU-12.2]` Implementar los eventos de dominio (`PresupuestoProgramaReservadoDominioEvent`, `PresupuestoProgramaConfirmadoDominioEvent`, `ReservaPresupuestoLiberadaDominioEvent`, `PeriodoProgramaCerradoDominioEvent`) y excepciones de negocio (`SinDisponibilidadPresupuestariaException`, `ReservaPresupuestariaNoEncontradaException`, `TransicionEstadoReservaInvalidaException`, `ReservaConParametrosDistintosException`, `ReservasPendientesEnPeriodoException`) en `Domain/Programas/Eventos/` y `Domain/Programas/Excepciones/`.
- [x] 1.4 `[HU-12.1]` `[HU-12.2]` Extender el agregado raíz `ProgramaBeca` con `PresupuestoReservado`, `CuposReservados`, propiedades calculadas `PresupuestoDisponible` y `CuposDisponibles`, colecciones de lectura `ReservasPresupuestarias` y `MovimientosPresupuestarios`, registro automático de movimientos en `AsignarFondoDonante`/`ReajustarFondoDonante` y métodos expresivos `ReservarPresupuesto`, `ConfirmarReservaPresupuesto`, `LiberarReservaPresupuesto` y `CerrarPeriodo`.

## 2. Aplicación (`Ascenso.Programas.Application/Programas/`)

- [x] 2.1 `[HU-12.1]` `[HU-12.2]` Extender el puerto `IProgramaBecaRepository` (con `CancellationToken` obligatorio) para soportar la reserva atómica condicional `C2` (`ReservarPresupuestoAtomicoAsync`), la carga del grafo con reservas y movimientos, y la consulta paginada/filtrable de `MovimientoPresupuestario`.
- [x] 2.2 `[HU-12.1]` Implementar `ObtenerBalanceProgramaQuery` (`GET /api/v1/programas/{id}/balance`) y `ConsultarMovimientosPresupuestariosQuery` (`GET /api/v1/programas/{id}/movimientos`) con sus DTOs (`BalancePresupuestarioProgramaResponse`, `MovimientoPresupuestarioResponse`) y `QueryHandlers` usando campos privados con prefijo `_camelCase`.
- [x] 2.3 `[HU-12.2]` Implementar `ReservarPresupuestoProgramaCommand`, `ConfirmarReservaPresupuestoCommand`, `LiberarReservaPresupuestoCommand` y `CerrarPeriodoProgramaCommand` con sus validadores FluentValidation, DTOs de respuesta y `CommandHandlers` cubriendo idempotencia por `referenciaExternaId`, concurrencia `C2` (`RN-PRG-03`) y cierre de período.

## 3. Infraestructura (`Ascenso.Programas.Infrastructure/Persistence/`)

- [x] 3.1 `[HU-12.1]` `[HU-12.2]` Implementar `MovimientoPresupuestarioConfiguracion` (`movimientos_presupuestarios`, `.ValueGeneratedNever()` en `Id`) y `ReservaPresupuestariaConfiguracion` (`reservas_presupuestarias`, `ConfigurarEntidadAuditable` con `.ValueGeneratedNever()` en `Id` y `Version` e índice único `(ProgramaBecaId, ReferenciaExternaId)`), y actualizar `ProgramaBecaConfiguracion` (`presupuesto_reservado`, `cupos_reservados`) y `ProgramasDbContext`.
- [x] 3.2 `[HU-12.1]` `[HU-12.2]` Implementar en `ProgramaBecaRepository` la reserva atómica condicional `C2` (`ExecuteUpdateAsync` en transacción validando estado operativo, cupos disponibles y saldo disponible) y las consultas de balance y movimientos paginados/filtrables, y generar la migración EF Core correspondiente en `Persistence/Migrations/`.

## 4. API REST (`Ascenso.Programas.Api/`)

- [x] 4.1 `[HU-12.1]` `[HU-12.2]` Exponer en `ProgramasController` (`Api/Controllers/v1/ProgramasController.cs`) los endpoints `GET {id:guid}/balance`, `GET {id:guid}/movimientos`, `POST {id:guid}/reservas`, `POST {id:guid}/reservas/{referenciaExternaId:guid}/confirmar`, `POST {id:guid}/reservas/{referenciaExternaId:guid}/liberar` y `POST {id:guid}/cerrar-periodo`, y verificar el mapeo HTTP (`404`, `409`) en `ExceptionHandlingMiddleware`.

## 5. Pruebas Automatizadas y Verificación CI (`tests/`)

- [x] 5.1 `[HU-12.1]` `[HU-12.2]` Implementar pruebas unitarias en `Ascenso.Programas.Domain.Tests/Programas/` y `Ascenso.Programas.Application.Tests/Programas/`, y pruebas de integración en `Ascenso.Programas.IntegrationTests/Programas/` verificando el cálculo de balance en tiempo real, el historial de movimientos, el ciclo `Reservar -> Confirmar / Liberar`, el cierre de período y la prueba de concurrencia crítica `C2` (`RN-PRG-03`) con solicitudes simultáneas compitiendo por el último cupo/presupuesto disponible (`409 SIN_DISPONIBILIDAD_PRESUPUESTARIA`).
- [x] 5.2 `[HU-12.1]` `[HU-12.2]` Ejecutar y verificar en limpio `dotnet restore --locked-mode`, `dotnet format --verify-no-changes --no-restore --verbosity minimal`, `dotnet build --configuration Release --no-restore -warnaserror`, `dotnet test --configuration Release --no-build` y `openspec validate rf12-int3-msprg --strict`.
