# Spec Delta: Registro y Configuración de Programas de Becas (`ms-prg/programas`)

## Purpose

Especificar el contrato de comportamiento del módulo de Programas de Becas (`RF-11`) en el microservicio `MS-PRG` (`Ascenso.Programas`), abarcando la creación de la estructura base y parámetros del programa (`ProgramaBeca`, `PoliticaCobertura`, `[HU-11.1]`), la asignación y control presupuestario atómico desde los saldos disponibles de los donantes (`AsignacionFondoPrograma`, `[HU-11.2]`, `RN-PRG-02`), y la configuración por composición de los criterios de elegibilidad (`CriteriosElegibilidadPrograma`) y condiciones de permanencia (`CondicionesPermanencia`) para transicionar el programa al estado `CONFIGURADO` (`[HU-11.3]`, `RN-PRG-01`).

## ADDED Requirements

### Requirement: [HU-11.1] Creación de Estructura Base y Parámetros del Programa de Becas
El sistema `MS-PRG` SHALL permitir crear un `ProgramaBeca` con `codigo` unívoco, `nombre`, `descripcion`, `PoliticaCobertura` (`modalidad`: `TOTAL`, `PARCIAL` o `MONTO_FIJO`, `porcentajeCobertura`, `montoFijoPeriodo`, `moneda`: `USD`), `cupoTotal > 0`, `duracionPeriodos > 0`, `periodoInicialId` y ventana `vigenciaHasta > vigenciaDesde`, naciendo en estado `BORRADOR` con presupuesto y cupos ocupados en `0`, y MUST emitir el evento `ProgramaBecaCreado`.

#### Scenario: [HU-11.1] Creación exitosa de programa de becas en estado borrador
- **GIVEN** un operador autorizado que envía una solicitud `POST /api/v1/programas` con un `codigo` no registrado previamente, `nombre`, `descripcion`, `politicaCobertura` válida (`TOTAL`, `PARCIAL` o `MONTO_FIJO` en `USD`), `cupoTotal > 0`, `duracionPeriodos > 0`, `periodoInicialId` y `vigenciaHasta > vigenciaDesde`
- **WHEN** el sistema procesa el comando `CrearProgramaBecaCommand`
- **THEN** crea el agregado `ProgramaBeca` en estado `BORRADOR` con `presupuestoTotalAsignado = 0.00` y `cuposOcupados = 0`
- **AND** encola el evento de integración `ProgramaBecaCreado` en el `Transactional Outbox` y responde `201 Created`.

#### Scenario: [HU-11.1] Rechazo de creación de programa por código duplicado
- **GIVEN** que ya existe un `ProgramaBeca` registrado en `MS-PRG` con el mismo `codigo` (normalizado en mayúsculas sin espacios extremos)
- **WHEN** se intenta crear un nuevo `ProgramaBeca` con dicho `codigo`
- **THEN** el sistema rechaza la operación con código HTTP `409 Conflict` (`CODIGO_PROGRAMA_DUPLICADO`)
- **AND** no persiste ningún registro duplicado ni emite eventos de integración.

#### Scenario: [HU-11.1] Rechazo de creación de programa por fechas de vigencia o parámetros de cobertura inválidos
- **GIVEN** una solicitud de creación de `ProgramaBeca` donde `vigenciaHasta <= vigenciaDesde`, `cupoTotal <= 0`, `duracionPeriodos <= 0` o los parámetros de `politicaCobertura` son inconsistentes con la modalidad seleccionada
- **WHEN** el sistema valida la solicitud en la capa de aplicación o en el dominio
- **THEN** rechaza la operación con código HTTP `400 Bad Request` (`FECHAS_VIGENCIA_PROGRAMA_INVALIDAS` o `POLITICA_COBERTURA_INVALIDA`)
- **AND** no persiste el programa ni publica eventos.

### Requirement: [HU-11.2] Asignación Atómica de Presupuesto de Donantes al Programa (RN-PRG-02)
El sistema `MS-PRG` SHALL permitir asignar fondos de uno o más donantes a un `ProgramaBeca` mediante entidades `AsignacionFondoPrograma` (`montoAsignado > 0`), exigiendo que cada `Donante` esté en estado `ACTIVO` y posea `saldoDisponibleAsignacion >= montoAsignado`, y MUST deducir el saldo del donante e incrementar `presupuestoTotalAsignado` del programa en una única transacción atómica emitiendo `PresupuestoProgramaAsignado`.

#### Scenario: [HU-11.2] Asignación exitosa de fondos de donante activo con saldo suficiente (RN-PRG-02)
- **GIVEN** un `ProgramaBeca` existente en estado `BORRADOR` o `CONFIGURADO` y un `Donante` en estado `ACTIVO` cuyo `saldoDisponibleAsignacion` es mayor o igual al `montoAsignado` solicitado (`montoAsignado > 0`)
- **WHEN** se envía `POST /api/v1/programas/{programaId}/asignaciones-presupuesto` y se procesa `AsignarPresupuestoDonantesCommand`
- **THEN** en una misma transacción atómica se deduce `montoAsignado` de `Donante.saldoDisponibleAsignacion`, se registra `AsignacionFondoPrograma` en estado `ASIGNADA` y se incrementa `ProgramaBeca.presupuestoTotalAsignado`
- **AND** se encola el evento `PresupuestoProgramaAsignado` en el `Transactional Outbox` y el sistema responde `201 Created`.

#### Scenario: [HU-11.2] Rechazo de asignación de fondos por saldo insuficiente o donante inactivo (RN-PRG-02)
- **GIVEN** un intento de asignar fondos a un `ProgramaBeca` desde un `Donante` donde `montoAsignado > Donante.saldoDisponibleAsignacion` o donde `Donante.estado != ACTIVO`
- **WHEN** el sistema valida el comando `AsignarPresupuestoDonantesCommand`
- **THEN** rechaza la operación con código HTTP `409 Conflict` (`SALDO_DONANTE_INSUFICIENTE` o `DONANTE_INACTIVO`)
- **AND** revierte cualquier cambio sin alterar el saldo del donante ni el presupuesto del programa.

### Requirement: [HU-11.2] Reajuste y Desasignación de Presupuesto con Bloqueo en Ejecución (RN-PRG-02)
El sistema `MS-PRG` SHALL permitir reajustar o desasignar parcial o totalmente una `AsignacionFondoPrograma` devolviendo el saldo correspondiente a `Donante.saldoDisponibleAsignacion` únicamente cuando el `ProgramaBeca` esté en estado `BORRADOR` o `CONFIGURADO`, y MUST bloquear cualquier reducción o desasignación cuando el programa se encuentre en estado `ACTIVO` con convocatoria en curso (`409 PRESUPUESTO_EN_EJECUCION`).

#### Scenario: [HU-11.2] Reajuste o desasignación exitosa en programa en borrador o configurado y bloqueo cuando está activo (RN-PRG-02)
- **GIVEN** un `ProgramaBeca` con una `AsignacionFondoPrograma` en estado `ASIGNADA`
- **WHEN** se solicita reducir o desasignar fondos mediante `POST /api/v1/programas/{programaId}/asignaciones-presupuesto/{asignacionId}/reajustar` (`ReajustarPresupuestoDonanteCommand`)
- **THEN** si el programa está en `BORRADOR` o `CONFIGURADO`, el sistema reduce `ProgramaBeca.presupuestoTotalAsignado`, restituye la diferencia a `Donante.saldoDisponibleAsignacion` (marcando la asignación como `DESASIGNADA` si el monto resultante es `0`) y responde `200 OK`
- **AND** si el programa está en estado `ACTIVO` (o en ejecución), el sistema rechaza la operación con código HTTP `409 Conflict` (`PRESUPUESTO_EN_EJECUCION`) manteniendo intactos los saldos.

### Requirement: [HU-11.3] Configuración por Composición de Criterios de Elegibilidad y Condiciones de Permanencia y Transición a Configurado (RN-PRG-01)
El sistema `MS-PRG` SHALL permitir configurar en `ProgramaBeca` los objetos de valor compuestos `CriteriosElegibilidadPrograma` y `CondicionesPermanencia` (con promedios mínimos entre `0.00` y `20.00` y al menos una universidad y carrera elegible), y MUST transicionar el programa a `CONFIGURADO` emitiendo `ProgramaBecaConfigurado` solo si `presupuestoTotalAsignado > 0`, `cupoTotal > 0` y todas las universidades elegibles poseen un `Convenio` en estado `VIGENTE`.

#### Scenario: [HU-11.3] Configuración exitosa de criterios y transición del programa al estado configurado (RN-PRG-01)
- **GIVEN** un `ProgramaBeca` en estado `BORRADOR` con `presupuestoTotalAsignado > 0` y `cupoTotal > 0`, y un conjunto de `universidadesElegiblesIds` y `carrerasElegiblesIds` cuyas universidades aliadas existen, están activas y tienen al menos un `Convenio` en estado `VIGENTE`
- **WHEN** se envía `PUT /api/v1/programas/{programaId}/configuracion-elegibilidad` con `CriteriosElegibilidadPrograma` y `CondicionesPermanencia` válidos (`promedioMinimoIngreso` y `promedioMinimoRenovacion` en `[0.00, 20.00]`)
- **THEN** el sistema compone ambos objetos de valor en `ProgramaBeca`, transiciona su estado a `CONFIGURADO`
- **AND** encola el evento `ProgramaBecaConfigurado` en el `Transactional Outbox` y responde `200 OK`.

#### Scenario: [HU-11.3] Rechazo de transición a configurado por falta de presupuesto o universidad sin convenio vigente (RN-PRG-01)
- **GIVEN** un `ProgramaBeca` cuyo `presupuestoTotalAsignado == 0` o cuya lista `universidadesElegiblesIds` incluye alguna universidad inexistente, inactiva o sin `Convenio` en estado `VIGENTE` (o carreras que no pertenecen a las universidades seleccionadas)
- **WHEN** se intenta ejecutar `ConfigurarCriteriosElegibilidadCommand` para pasar el programa a `CONFIGURADO`
- **THEN** el sistema rechaza la transición con código HTTP `422 Unprocessable Entity` (`PROGRAMA_NO_CONFIGURABLE`)
- **AND** el programa permanece en estado `BORRADOR` sin publicar `ProgramaBecaConfigurado`.

#### Scenario: [HU-11.3] Rechazo de configuración por promedio académico fuera del rango 0.00 a 20.00
- **GIVEN** una solicitud de configuración donde `promedioMinimoIngreso` o `promedioMinimoRenovacion` es menor que `0.00` o mayor que `20.00`, o donde `universidadesElegiblesIds` o `carrerasElegiblesIds` están vacías
- **WHEN** el sistema valida `ConfigurarCriteriosElegibilidadCommand`
- **THEN** rechaza la operación con código HTTP `400 Bad Request` (`PROMEDIO_ACADEMICO_INVALIDO` o error de validación)
- **AND** no modifica el estado del `ProgramaBeca`.

### Requirement: [HU-11.3] Consultas Paginadas y Detalle de Programas de Becas
El sistema `MS-PRG` SHALL proveer consultas de lectura para listar `ProgramaBeca` de forma paginada (filtrable por `estado`, `periodoInicialId` y `modalidadCobertura`) y obtener el detalle completo de un programa por su identificador (incluyendo `PoliticaCobertura`, `AsignacionFondoPrograma`, `CriteriosElegibilidadPrograma` y `CondicionesPermanencia`).

#### Scenario: [HU-11.3] Consulta paginada de programas y obtención de detalle completo por ID
- **GIVEN** programas de becas registrados en `MS-PRG` con sus políticas de cobertura, asignaciones de donantes y criterios configurados
- **WHEN** se consulta `GET /api/v1/programas` (con filtros opcionales de estado, período y modalidad de cobertura) o `GET /api/v1/programas/{programaId}`
- **THEN** el sistema responde `200 OK` devolviendo el listado paginado de resúmenes o el detalle integral del `ProgramaBeca` (o `404 Not Found` si el `programaId` no existe).
