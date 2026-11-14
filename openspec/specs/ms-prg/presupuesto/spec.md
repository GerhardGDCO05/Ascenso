# ms-prg/presupuesto Specification

## Purpose

Especificar el contrato de comportamiento del módulo de Control de Balance Presupuestario, Reservas Atómicas y Cierre de Períodos (`RF-12`) en el microservicio `MS-PRG` (`Ascenso.Programas`), abarcando la consulta en tiempo real del balance financiero y de cupos junto con la bitácora inmutable de movimientos presupuestarios (`MovimientoPresupuestario`, `[HU-12.1]`), la reserva atómica e idempotente de cupo y saldo presupuestario bajo concurrencia crítica `C2` (`ReservaPresupuestaria`, `[HU-12.2]`, `RN-PRG-03`), y la confirmación, liberación de reservas y cierre contable de período (`[HU-12.2]`).

## Requirements

### Requirement: [HU-12.1] Consulta de Balance Presupuestario en Tiempo Real y Auditoría de Movimientos
El sistema MS-PRG SHALL mantener una bitácora inmutable de movimientos presupuestarios (`MovimientoPresupuestario`) para cada programa de becas (`AsignacionDonante`, `ReajusteDonante`, `ReservaCupo`, `ConfirmacionDesembolso`, `LiberacionReserva`, `CierrePeriodo`) y permitir consultar el balance financiero y de cupos en tiempo real (`GET /api/v1/programas/{id}/balance`) así como el historial paginado y filtrable de movimientos (`GET /api/v1/programas/{id}/movimientos`).

#### Scenario: [HU-12.1] Consulta exitosa del balance financiero y de cupos en tiempo real
- **WHEN** se consulta el balance de un programa (`GET /api/v1/programas/{id}/balance`) que tiene `$100,000.00 USD` asignados, `$15,000.00 USD` reservados, `$35,000.00 USD` ejecutados, `cupoTotal = 20`, `cuposReservados = 3` y `cuposOcupados = 7`
- **THEN** el sistema responde `200 OK` con `presupuestoTotalAsignado = 100000.00`, `presupuestoReservado = 15000.00`, `presupuestoEjecutado = 35000.00`, `presupuestoDisponible = 50000.00` (`Asignado - Reservado - Ejecutado`), `cupoTotal = 20`, `cuposReservados = 3`, `cuposOcupados = 7` y `cuposDisponibles = 10` (`CupoTotal - CuposReservados - CuposOcupados`)

#### Scenario: [HU-12.1] Registro automático de movimiento inmutable al asignar o reajustar fondos de donante
- **WHEN** se asigna o reajusta un fondo de donante en un programa de becas
- **THEN** el sistema persiste una entidad inmutable `MovimientoPresupuestario` con identificador UUIDv7, `tipoMovimiento` en `AsignacionDonante` o `ReajusteDonante`, el `monto` de la variación en `USD`, la referencia de la asignación y la fecha UTC del movimiento

#### Scenario: [HU-12.1] Consulta paginada y filtrada del historial de movimientos presupuestarios
- **WHEN** se consulta `GET /api/v1/programas/{id}/movimientos` filtrando opcionalmente por `tipoMovimiento`, `periodoAcademicoId` o `referenciaExternaId` con parámetros de paginación (`pagina`, `tamanoPagina`)
- **THEN** el sistema responde `200 OK` con la lista ordenada cronológicamente en forma descendente de registros `MovimientoPresupuestario` (`programaBecaId`, `tipoMovimiento`, `monto`, `moneda`, `referenciaExternaId`, `periodoAcademicoId`, `descripcion`, `fechaMovimiento`) y el total de elementos

### Requirement: [HU-12.2] Reserva Atómica de Cupo y Saldo Presupuestario bajo Concurrencia Crítica C2 (RN-PRG-03)
El sistema MS-PRG SHALL ejecutar reservas de cupo y saldo presupuestario (`POST /api/v1/programas/{id}/reservas`) de manera atómica e idempotente por `referenciaExternaId`, incrementando `presupuestoReservado` y `cuposReservados` únicamente si el programa está en estado operativo y dispone de al menos un cupo y saldo disponible suficiente para cubrir el monto solicitado.

#### Scenario: [HU-12.2] Reserva atómica exitosa de cupo y presupuesto con emisión de evento
- **WHEN** se solicita `POST /api/v1/programas/{id}/reservas` con `referenciaExternaId`, `periodoAcademicoId`, `monto > 0` y `descripcion` sobre un programa con al menos `1` cupo disponible y `presupuestoDisponible >= monto`
- **THEN** el sistema incrementa atómicamente `presupuestoReservado += monto` y `cuposReservados += 1`, crea la entidad `ReservaPresupuestaria` en estado `Reservada`, registra un `MovimientoPresupuestario` de tipo `ReservaCupo`, emite el evento de dominio `PresupuestoProgramaReservadoDominioEvent` y responde `201 Created` con el estado actualizado del balance y de la reserva

#### Scenario: [HU-12.2] Idempotencia al reintentar una reserva con la misma referenciaExternaId
- **WHEN** se recibe una solicitud repetida `POST /api/v1/programas/{id}/reservas` con el mismo `referenciaExternaId` y el mismo `monto` para una reserva ya existente en el programa
- **THEN** el sistema no vuelve a descontar cupo ni saldo presupuestario ni duplica el `MovimientoPresupuestario`, respondiendo `200 OK` con los datos de la reserva previamente registrada

#### Scenario: [HU-12.2] Rechazo por concurrencia C2 o falta de cupo/presupuesto disponible (RN-PRG-03)
- **WHEN** una o varias solicitudes concurrentes intentan reservar cupo y presupuesto en `POST /api/v1/programas/{id}/reservas` cuando `cuposDisponibles == 0` o `presupuestoDisponible < monto` (por ejemplo, dos solicitudes simultáneas por el último cupo disponible)
- **THEN** únicamente la solicitud que gana la actualización condicional atómica tiene éxito y toda solicitud que exceda el cupo o saldo disponible es rechazada con `409 Conflict` y código `SIN_DISPONIBILIDAD_PRESUPUESTARIA`, garantizando cero sobreasignación

### Requirement: [HU-12.2] Confirmación y Liberación de Reservas Presupuestarias y Cierre de Período
El sistema MS-PRG SHALL permitir confirmar una reserva activa (`POST /api/v1/programas/{id}/reservas/{referenciaExternaId}/confirmar`) convirtiéndola en presupuesto ejecutado y cupo ocupado, liberar una reserva activa (`POST /api/v1/programas/{id}/reservas/{referenciaExternaId}/liberar`) restituyendo el saldo y cupo a disponible, y ejecutar el cierre contable de período (`POST /api/v1/programas/{id}/cerrar-periodo`) consolidando los saldos remanentes no ejecutados.

#### Scenario: [HU-12.2] Confirmación exitosa de reserva convirtiendo saldo reservado en ejecutado y cupo en ocupado
- **WHEN** se solicita `POST /api/v1/programas/{id}/reservas/{referenciaExternaId}/confirmar` sobre una reserva en estado `Reservada`
- **THEN** el sistema transiciona la reserva a `Confirmada`, descuenta `presupuestoReservado -= monto` e incrementa `presupuestoEjecutado += monto`, descuenta `cuposReservados -= 1` e incrementa `cuposOcupados += 1`, registra un `MovimientoPresupuestario` de tipo `ConfirmacionDesembolso`, emite `PresupuestoProgramaConfirmadoDominioEvent` y responde `200 OK`

#### Scenario: [HU-12.2] Liberación exitosa de reserva restituyendo cupo y presupuesto disponible
- **WHEN** se solicita `POST /api/v1/programas/{id}/reservas/{referenciaExternaId}/liberar` sobre una reserva en estado `Reservada` indicando el motivo de liberación
- **THEN** el sistema transiciona la reserva a `Liberada`, descuenta `presupuestoReservado -= monto` y `cuposReservados -= 1` devolviendo el monto y el cupo al disponible del programa, registra un `MovimientoPresupuestario` de tipo `LiberacionReserva`, emite `ReservaPresupuestoLiberadaDominioEvent` y responde `200 OK`

#### Scenario: [HU-12.2] Rechazo al intentar confirmar o liberar una reserva ya procesada o inexistente
- **WHEN** se intenta confirmar una reserva que ya está en estado `Liberada` (o liberar una reserva que ya está en estado `Confirmada`)
- **THEN** el sistema rechaza la operación con `409 Conflict` y código `TRANSICION_ESTADO_RESERVA_INVALIDA` sin alterar los saldos ni los cupos del programa

#### Scenario: [HU-12.2] Cierre contable de período consolidando saldos no ejecutados
- **WHEN** se solicita `POST /api/v1/programas/{id}/cerrar-periodo` para un `periodoAcademicoId` sin reservas pendientes en estado `Reservada` (con opción de cerrar definitivamente el programa si concluye su vigencia)
- **THEN** el sistema consolida el saldo disponible no ejecutado del período registrando un `MovimientoPresupuestario` de tipo `CierrePeriodo`, actualiza el estado del programa a `Cerrado` cuando se solicita el cierre definitivo del programa, emite `PeriodoProgramaCerradoDominioEvent` y responde `200 OK`

#### Scenario: [HU-12.2] Rechazo de cierre de período cuando existen reservas pendientes sin resolver
- **WHEN** se solicita `POST /api/v1/programas/{id}/cerrar-periodo` y aún existen reservas en estado `Reservada` para el período indicado
- **THEN** el sistema rechaza la operación con `409 Conflict` y código `RESERVAS_PENDIENTES_EN_PERIODO` exigiendo confirmar o liberar las reservas antes del cierre contable
