## Purpose

Definir la programación y ejecución de desembolsos en `MS-BEC` para RF-19 y RF-20. Este delta se incorpora por etapas mientras las integraciones con `MS-PRG` y `MS-PLT` siguen pendientes.

## ADDED Requirements

### Requirement: Modalidad de pago sin mezclar frecuencia y destinatario

`CalendarioDesembolso` SHALL representar por separado la frecuencia (`MENSUAL`, `SEMESTRAL`) y el destinatario (`BECARIO`, `INSTITUCION`). La expresión «pago directo a la institución» del PRD SHALL interpretarse como destinatario y no como una tercera periodicidad.

#### Scenario: Calendario mensual a institución

- **WHEN** se programa un calendario mensual cuyo destinatario es una universidad
- **THEN** se conserva `frecuencia = MENSUAL` y `destinatarioPredeterminado = INSTITUCION`
- **AND** no se introduce un valor `DIRECTO_INSTITUCION` en `FrecuenciaDesembolso`

### Requirement: Códigos operativos de desembolsos

`EstadoDesembolso` SHALL usar literalmente los siete códigos de la Tabla 3.3 del DDD: `PROGRAMADO`, `EN_PROCESO`, `PAGADO_CONFIRMADO`, `FALLIDO`, `DEVUELTO`, `SUSPENDIDO` y `CANCELADO`.

Los códigos de los otros enums son decisiones locales de `MS-BEC`, porque el PRD y el DDD nombran los tipos sin enumerar sus valores. Su publicación en contratos externos SHALL requerir comprobar la compatibilidad con los consumidores.

#### Scenario: Códigos propios todavía no publicados

- **WHEN** se serializan `MetodoPago`, `EstadoPlanDesembolso`, `EstadoDatosPago`, `TipoIncidenciaDesembolso` o `EstadoIncidencia`
- **THEN** se usan `TRANSFERENCIA_BANCARIA` para el método; `PROGRAMADO`, `SUSPENDIDO`, `COMPLETADO`, `CANCELADO` para el plan; `ACTIVO`, `REEMPLAZADO` para los datos; `FALLO_BANCARIO`, `DEVOLUCION_BANCARIA` para el tipo de incidencia; y `ABIERTA`, `RESUELTA` para la incidencia
- **AND** sus contratos externos permanecen pendientes de revisión

### Requirement: Techo del calendario por periodo

El calendario SHALL exigir una beca y un periodo académico, al menos una cuota positiva con numeración única y cronológica, fechas dentro de su rango y montos con dos decimales como máximo. SHALL calcular la suma de cuotas desde sus entidades hijas y rechazar cualquier total superior al monto aprobado del periodo (`RN-BEC-03`, `422 EXCEDE_OFERTA` en la capa HTTP).

#### Scenario: Suma exacta al monto aprobado

- **WHEN** las cuotas suman exactamente el monto aprobado
- **THEN** el calendario se crea en estado `PROGRAMADO` con el número y suma de cuotas correctos

#### Scenario: Cuotas superiores al monto aprobado

- **WHEN** la suma de cuotas supera el monto aprobado
- **THEN** la programación se rechaza con `EXCEDE_OFERTA`
- **AND** no se crea un calendario parcial

### Requirement: Transiciones de una cuota

Cada cuota SHALL seguir únicamente las transiciones de la Tabla 3.3 del DDD. La confirmación SHALL exigir una autorización OTP; `FALLIDO` y `DEVUELTO` SHALL conservar observación bancaria y requerir una incidencia resuelta antes del reintento. Los estados `PAGADO_CONFIRMADO` y `CANCELADO` SHALL ser terminales.

#### Scenario: Intento de transición terminal

- **WHEN** se intenta mover una cuota `PAGADO_CONFIRMADO` o `CANCELADO`
- **THEN** el dominio rechaza la transición

### Requirement: Trazabilidad de reintentos por cuota

Un reintento SHALL conservar la fila del desembolso fallido o devuelto y crear una nueva fila de la misma cuota con `desembolsoOrigenReintentoId` apuntando a la anterior. La nueva fila SHALL ser la única vigente para esa cuota y SHALL mantener un número de intento único. Los cálculos del techo SHALL contar solo la fila vigente de cada cuota.

#### Scenario: Reintento después de incidencia resuelta

- **WHEN** una incidencia de una cuota fallida o devuelta está resuelta
- **THEN** el reintento crea una nueva fila `PROGRAMADO` enlazada con la fila previa
- **AND** la fila anterior permanece consultable para auditoría

### Requirement: Datos de pago cifrados e historial de cuentas

`DatosPagoBecario` SHALL guardar el número de cuenta únicamente como ciphertext AES-GCM versionado. La clave SHALL obtenerse de un secreto externo al repositorio. Una modificación SHALL exigir el identificador de la autorización OTP, marcar la cuenta previa como `REEMPLAZADO` y crear una nueva fila `ACTIVO` sin borrar el historial. La base SHALL permitir solo una cuenta activa por beca.

#### Scenario: Reemplazo autorizado de cuenta

- **WHEN** se registra una nueva cuenta con autorización OTP y la anterior está activa
- **THEN** la anterior queda `REEMPLAZADO` y la nueva `ACTIVO`
- **AND** ambas filas conservan solo el número cifrado y permanecen consultables

#### Scenario: Modificación sin OTP

- **WHEN** se intenta reemplazar una cuenta sin autorización OTP
- **THEN** el dominio rechaza el cambio y la cuenta anterior sigue activa

### Requirement: Casos de uso de desembolsos

`MS-BEC` SHALL ofrecer comandos para programar el calendario, actualizar datos de pago, registrar y confirmar la ejecución, registrar y resolver incidencias y reintentar un pago. SHALL ofrecer consultas paginadas de calendarios por beca y cuotas por calendario. Los comandos SHALL validar sus entradas antes de mutar el dominio y SHALL rechazar estados o montos incompatibles con `RN-BEC-02`, `RN-BEC-03` y la Tabla 3.3.

#### Scenario: Cuenta activa del becario

- **WHEN** se registra una ejecución destinada al becario
- **THEN** se comprueba que `datosPagoBecarioId` identifica su cuenta activa
- **AND** el snapshot de destino se toma del ciphertext almacenado, no del cuerpo de la solicitud

#### Scenario: Consulta de cuotas

- **WHEN** se consulta una página de cuotas de un calendario
- **THEN** la base devuelve solo el tamaño solicitado y las ordena por número de cuota e intento

### Requirement: Compuerta y conservación contable de la beca

`Beca` SHALL rechazar el registro y la confirmación de desembolsos con `COMPROMISO_NO_FIRMADO` y HTTP 412 cuando el programa exija compromiso y este no esté aprobado (`RN-BEC-02`). Al confirmar una cuota, `MS-BEC` SHALL restar su monto del saldo comprometido pendiente y sumarlo al monto desembolsado dentro de la misma transacción, sin superar el monto aprobado (`RN-BEC-03`).

#### Scenario: Compromiso sin aprobar

- **WHEN** se intenta registrar o confirmar una cuota de una beca que requiere compromiso aprobado y aún no lo tiene
- **THEN** la operación se rechaza con `412 COMPROMISO_NO_FIRMADO`
- **AND** sus saldos no cambian

#### Scenario: Confirmación de una cuota dentro del techo

- **WHEN** se confirma una cuota de 100 de una beca con 1500 comprometidos pendientes
- **THEN** quedan 1400 comprometidos pendientes y 100 desembolsados
- **AND** un monto que exceda el saldo o el aprobado se rechaza con `422 EXCEDE_OFERTA`

### Requirement: Saldo pendiente al cerrar una beca

La suspensión SHALL conservar el saldo pendiente de la beca para su transferencia entre bolsas de `MS-PRG`; la revocación o conclusión SHALL liberar únicamente ese saldo y conservar el monto ya pagado. Los movimientos en `MS-PRG` se implementan en su tarea de integración.

#### Scenario: Suspensión y revocación después de un pago

- **WHEN** se suspende una beca que ya pagó 100 y conserva 1400 pendientes
- **THEN** la beca mantiene ambos saldos y bloquea nuevas confirmaciones
- **AND** al revocarla libera los 1400 pendientes sin reducir los 100 pagados

### Requirement: Consumo OTP vinculado a la operación

La confirmación de una cuota y el cambio de datos bancarios SHALL consumir un OTP de `MS-PLT` vinculado a `usuarioId`, `operacion`, `recursoTipo`, `recursoId` y `huellaPayload`. La clave de idempotencia SHALL identificar la autorización OTP. `MS-BEC` SHALL bloquear la fila de `Beca` antes de confirmar y registrar el cambio local solo si el consumo es aceptado. Un OTP expirado o bloqueado SHALL impedir el cambio.

#### Scenario: Revocación obtiene primero el bloqueo

- **WHEN** una transacción de revocación bloquea y revoca la beca antes de la confirmación concurrente
- **THEN** la confirmación espera el bloqueo y, al recibir el estado revocado, no consume el OTP ni marca la cuota como pagada

#### Scenario: OTP rechazado

- **WHEN** `MS-PLT` devuelve `OTP_EXPIRADO` u `OTP_BLOQUEADO` para la cuota o la cuenta bancaria
- **THEN** la operación se rechaza y no cambia el estado local

### Requirement: Eventos de desembolsos sin datos sensibles

`MS-BEC` SHALL publicar por Outbox los hitos de programación, confirmación con OTP, fallo, devolución, reintento y cancelación de cuotas mediante `EventoIntegracion<T>`. Los payloads SHALL incluir solo los identificadores de beca, calendario y desembolso; la cancelación se emitirá al implementar la revocación en la sección 17.

#### Scenario: Confirmación auditada

- **WHEN** una cuota se confirma con OTP y se guarda el cambio local
- **THEN** se encola `DesembolsoConfirmadoConOTP` con el mismo commit
- **AND** el payload no contiene cuentas bancarias, documentos ni montos socioeconómicos

### Requirement: Generación automática con configuración completa

Al activarse una beca o crearse su sucesora por renovación, `MS-BEC` SHALL generar un único calendario por beca y periodo con el monto aprobado y cuotas derivadas de una frecuencia, destinatario y fechas de periodo confirmados por los servicios propietarios. Si falta cualquiera de estos datos, SHALL detectar el calendario pendiente y fallar cerrado hasta poder reconciliarlo de forma idempotente.

#### Scenario: Configuración de pagos ausente

- **WHEN** una beca se activa sin frecuencia o fechas de periodo confirmadas
- **THEN** no se inventan cuotas ni se genera un calendario parcial
- **AND** la beca queda identificable para reconciliación posterior

### Requirement: Registro de suspensión de desembolsos

`Beca` SHALL conservar cada `SuspensionDesembolso` con tipo, motivo, requisitos de reanudación, fecha inicial, fecha de revisión o marca indefinida, estado y coordinadores de alta y levantamiento. Los códigos locales son `MANUAL`, `INCUMPLIMIENTO_ACADEMICO` y `PAUSA_APLAZAMIENTO` para el tipo, y `VIGENTE` y `LEVANTADA` para el estado; el DDD no enumera sus valores literales.

#### Scenario: Suspensión indefinida

- **WHEN** coordinación registra una suspensión sin fecha de revisión y con `esIndefinida = true`
- **THEN** la fila hija de la beca se conserva en estado `VIGENTE`
- **AND** no se acepta una segunda suspensión vigente para la misma beca

#### Scenario: Reactivación manual

- **WHEN** coordinación reactiva una beca con suspensión manual vigente
- **THEN** la suspensión queda `LEVANTADA` y la beca vuelve a `ACTIVA` sin liberar ni duplicar su saldo pendiente
- **AND** las cuotas de esa suspensión pueden retornar de `SUSPENDIDO` a `PROGRAMADO` dentro de la transacción de reactivación

#### Scenario: Reactivación tras pausa sin evaluación

- **WHEN** se intenta reactivar una suspensión por pausa sin nuevo reporte académico y evaluación favorable
- **THEN** la beca y sus cuotas permanecen suspendidas

#### Scenario: Revocación definitiva

- **WHEN** se revoca con autorización OTP una beca `OTORGADA`, `ACTIVA` o `SUSPENDIDA`
- **THEN** la beca queda `REVOCADA` con `tipoCierre = REVOCACION_DEFINITIVA` y se libera únicamente el saldo pendiente
- **AND** las cuotas vigentes `PROGRAMADO` o `SUSPENDIDO` se cancelan sin cambiar los pagos `PAGADO_CONFIRMADO`
- **AND** una oferta aún pendiente se cierra para que no pueda aceptarse después

### Requirement: Coordinación local del ciclo de vida de la beca

`MS-BEC` SHALL bloquear la fila de `Beca` antes de suspender, reanudar o revocar y SHALL persistir la beca, las cuotas de su calendario y los eventos Outbox en una misma transacción local. Para revocar, SHALL consumir primero en `MS-PLT` el OTP vinculado a `REVOCAR_BECA`, recurso `BECA` e identificador de la beca; un rechazo por código expirado, bloqueado o inválido SHALL dejar beca y cuotas sin cambios. Los movimientos de saldo y cupo en `MS-PRG` se coordinan en la tarea de integración 17.6.

#### Scenario: Revocación gana la carrera a una confirmación

- **WHEN** una revocación bloquea la beca antes de que una confirmación de cuota obtenga el mismo bloqueo
- **THEN** la confirmación espera y, al ver la beca revocada, no consume su OTP ni registra el pago
