## Purpose

Define el ciclo de otorgamiento de una beca en `MS-BEC`: la emisión y respuesta a la oferta, la activación de la beca con la invariante de no doble beneficio, la compuerta del compromiso firmado y la consulta de becas activas que otros microservicios necesitan para operar.

## ADDED Requirements

### Requirement: Emisión de la oferta de beca

Al emitir una oferta, el sistema SHALL verificar que la postulación sea adjudicable y que `MS-PRG` confirme una reserva de cupo y presupuesto. En una única transacción local SHALL crear la `Beca` en estado `OTORGADA` y la `OfertaBeca` en estado `PENDIENTE_RESPUESTA`. La reserva SHALL solicitarse con la clave de idempotencia `oferta-{ofertaId}`.

#### Scenario: Emisión exitosa

- **WHEN** un coordinador emite una oferta para una postulación adjudicable con cupo y presupuesto reservados
- **THEN** el sistema responde `201` con el identificador de la oferta, su estado `PENDIENTE_RESPUESTA` y la fecha de vencimiento
- **AND** la `Beca` queda en `OTORGADA`, con `cadenaBecaId = Beca.id`, `becaAnteriorId = null` y `periodosCursados = 0`
- **AND** `fechaVencimientoAceptacion = ahora + 15 días` y se registra la referencia al job de expiración en Hangfire
- **AND** se publica `OfertaBecaEmitida` y se programa la expiración a los 15 días

#### Scenario: Postulación no adjudicable

- **WHEN** `MS-POS` indica que la postulación no está dentro de cupo ni fue promovida desde lista de espera
- **THEN** el sistema responde `422 POSTULACION_NO_ADJUDICABLE`
- **AND** no crea la beca, no reserva presupuesto ni publica eventos

#### Scenario: Reserva rechazada por cupo, presupuesto o convenio

- **WHEN** `MS-PRG` rechaza la reserva por falta de cupo, presupuesto insuficiente o convenio vencido
- **THEN** el sistema responde `409` con el código devuelto por `MS-PRG`
- **AND** no se persiste ninguna oferta parcial

#### Scenario: Reintento de emisión

- **WHEN** se repite la emisión con la misma `Idempotency-Key` y el mismo payload
- **THEN** el sistema devuelve la oferta ya creada
- **AND** no solicita una segunda reserva ni publica un segundo `OfertaBecaEmitida`

### Requirement: Fotografía inmutable de la configuración al emitir

La `OfertaBeca` y la `Beca` SHALL conservar una fotografía de la política de cobertura, las condiciones de permanencia, el costo de matrícula de referencia y los montos aprobados vigentes al momento de la emisión. Cambios posteriores en el programa, el arancel o la política NO SHALL alterar retroactivamente una oferta ni una beca ya emitida.

#### Scenario: Cambio de arancel después de emitir

- **WHEN** `MS-PRG` publica un nuevo costo de matrícula después de emitida la oferta
- **THEN** la oferta y la beca conservan el costo y los montos capturados en la emisión
- **AND** la nueva oferta de otro estudiante utiliza el arancel vigente en su propia emisión

### Requirement: Aceptación de la oferta y activación de la beca

La aceptación SHALL ejecutarse solo si la oferta sigue en `PENDIENTE_RESPUESTA` y `ahora <= fechaVencimientoAceptacion`. El sistema SHALL abrir una transacción `Serializable` y adquirir un bloqueo transaccional por estudiante antes de validar el no doble beneficio. Si las validaciones pasan, la oferta SHALL pasar a `ACEPTADA`, la beca SHALL pasar a `ACTIVA` con `fechaActivacion`, y se SHALL publicar `BecaActivada`.

#### Scenario: Aceptación dentro del plazo

- **WHEN** el postulante acepta su oferta antes del vencimiento y no incurre en doble beneficio
- **THEN** la oferta queda en `ACEPTADA` y la beca en `ACTIVA`
- **AND** se registra la fecha de activación y se publica `BecaActivada`
- **AND** la operación es idempotente ante reintentos con la misma clave

#### Scenario: Oferta vencida

- **WHEN** el postulante intenta aceptar una oferta después de `fechaVencimientoAceptacion`
- **THEN** el sistema responde `409 OFERTA_VENCIDA`
- **AND** la beca no se activa

#### Scenario: Aceptación simultánea de dos ofertas incompatibles

- **WHEN** el mismo estudiante intenta aceptar dos ofertas incompatibles en peticiones paralelas
- **THEN** una petición activa la beca y la otra responde `409 DOBLE_BENEFICIO`
- **AND** el estudiante no queda con dos becas activas incompatibles

### Requirement: Invariante de no doble beneficio

Al activar una beca, el sistema SHALL verificar que el estudiante no tenga otra beca `ACTIVA` con cobertura `TOTAL` y que no tenga otra beca `ACTIVA` en el mismo periodo académico, salvo que ambos programas permitan acumulación. La regla SHALL estar respaldada por bloqueo por estudiante, aislamiento `Serializable` e índices únicos parciales en PostgreSQL.

#### Scenario: Beca total activa previa

- **WHEN** el estudiante ya posee una beca `ACTIVA` con modalidad `TOTAL` en cualquier programa o periodo
- **THEN** la aceptación se rechaza con `409 DOBLE_BENEFICIO`
- **AND** la nueva beca permanece en `OTORGADA`

#### Scenario: Acumulación permitida

- **WHEN** el estudiante ya tiene una beca `ACTIVA` en el mismo periodo y ambos programas tienen `permiteAcumulacion = true`
- **THEN** la aceptación se permite
- **AND** se conservan ambas becas activas

### Requirement: Rechazo y anulación de la oferta

El postulante SHALL poder rechazar una oferta `PENDIENTE_RESPUESTA`, y un coordinador SHALL poder anularla con una justificación no vacía de al menos 10 caracteres. En ambos casos la oferta SHALL pasar a su estado terminal correspondiente (`RECHAZADA_POR_POSTULANTE` o `ANULADA_POR_COORDINACION`), la beca SHALL pasar de `OTORGADA` a `REVOCADA` con `tipoCierre` `OFERTA_RECHAZADA` u `OFERTA_ANULADA`, y se SHALL liberar la reserva de cupo y presupuesto en `MS-PRG`.

#### Scenario: Rechazo del postulante

- **WHEN** el postulante rechaza una oferta pendiente
- **THEN** la oferta queda en `RECHAZADA_POR_POSTULANTE` y la beca en `REVOCADA` con `tipoCierre = OFERTA_RECHAZADA`
- **AND** se libera la reserva y se publica `OfertaBecaRechazada`

#### Scenario: Anulación sin justificación suficiente

- **WHEN** un coordinador intenta anular una oferta con una justificación de menos de 10 caracteres
- **THEN** el sistema responde `400 JUSTIFICACION_REQUERIDA`
- **AND** la oferta y la beca conservan su estado

#### Scenario: Anulación válida

- **WHEN** un coordinador anula una oferta pendiente con una justificación válida
- **THEN** la oferta queda en `ANULADA_POR_COORDINACION` y la beca en `REVOCADA` con `tipoCierre = OFERTA_ANULADA`
- **AND** se libera la reserva y se publica `OfertaBecaAnulada`

### Requirement: Expiración automática a los 15 días

El sistema SHALL programar la expiración de cada oferta a los 15 días exactos de su emisión en la cola `critical`. Al ejecutarse, el job SHALL releer la oferta y solo SHALL actuar si continúa en `PENDIENTE_RESPUESTA` y la fecha de vencimiento ya se cumplió. En ese caso la oferta SHALL pasar a `EXPIRADA`, la beca a `REVOCADA` con `tipoCierre = OFERTA_EXPIRADA` y se SHALL liberar la reserva en `MS-PRG`.

#### Scenario: Expiración de una oferta pendiente

- **WHEN** el job se ejecuta al cumplirse los 15 días y la oferta sigue pendiente
- **THEN** la oferta queda en `EXPIRADA` y la beca en `REVOCADA` con `tipoCierre = OFERTA_EXPIRADA`
- **AND** se libera la reserva y se publica `OfertaBecaExpirada`

#### Scenario: Oferta ya respondida antes del job

- **WHEN** el job se ejecuta y la oferta ya fue aceptada, rechazada o anulada
- **THEN** el job no modifica ningún estado ni libera recursos
- **AND** la ejecución finaliza correctamente

### Requirement: Compuerta de compromiso firmado

Si el programa exige compromiso firmado, la beca SHALL activarse con `habilitadaParaDesembolso = false` y SHALL requerir un `CompromisoBecario` en estado `APROBADO` para habilitar desembolsos. El compromiso SHALL pasar por `PENDIENTE_CARGA`, `EN_REVISION` y `APROBADO` o `RECHAZADO`, con número de intento, URL de Firebase, hash SHA-256 del PDF y registro del coordinador que revisa. Un rechazo SHALL permitir una nueva carga.

#### Scenario: Carga del compromiso firmado

- **WHEN** el becario carga un PDF de compromiso válido para una beca que lo requiere
- **THEN** el compromiso queda en `EN_REVISION` con su URL, hash y número de intento
- **AND** se publica `CompromisoBecarioCargado`

#### Scenario: Aprobación del compromiso

- **WHEN** el coordinador aprueba el compromiso en revisión
- **THEN** el compromiso queda en `APROBADO` y la beca actualiza `habilitadaParaDesembolso = true`
- **AND** se publica `CompromisoBecarioAprobado`

#### Scenario: Rechazo y nueva carga

- **WHEN** el coordinador rechaza el compromiso con observaciones
- **THEN** el compromiso queda en `RECHAZADO`, la beca mantiene `habilitadaParaDesembolso = false` y se publica `CompromisoBecarioRechazado`
- **AND** el becario puede cargar un nuevo intento que incremente `numeroIntentoCarga`

### Requirement: Historial de estados append-only

Cada transición de `Beca` u `OfertaBeca` SHALL registrar una fila inmutable en `HistorialEstadoBeca` con estado anterior, estado nuevo, fecha, actor, motivo o justificación, identificador de OTP cuando aplique y marca de estado terminal irreversible. El historial NO SHALL permitir actualizaciones ni eliminaciones.

#### Scenario: Transición auditada

- **WHEN** una beca pasa de `OTORGADA` a `ACTIVA`
- **THEN** se inserta una fila de historial con ambos estados, el actor y la fecha
- **AND** no existe operación de API que modifique o elimine esa fila

### Requirement: Consulta interna de becas activas

El servicio SHALL exponer `GET /internal/v1/becas/activas?postulanteId={uuid}&periodoId={uuid}` para que `MS-POS` prevalide el no doble beneficio. La respuesta SHALL requerir un *Service Token* válido y SHALL devolver únicamente las becas en estado `ACTIVA` del estudiante en el periodo consultado, con identificadores, programa, modalidad de cobertura y `permiteAcumulacion`, sin datos personales ni bancarios.

#### Scenario: Estudiante con beca activa

- **WHEN** `MS-POS` consulta con un token de servicio válido y el estudiante tiene una beca activa en el periodo
- **THEN** el servicio responde `200` con la lista de becas activas y sus datos de cobertura
- **AND** no incluye documentos, cuentas bancarias ni datos socioeconómicos

#### Scenario: Estudiante sin becas activas

- **WHEN** `MS-POS` consulta con un token de servicio válido y el estudiante no tiene becas activas en el periodo
- **THEN** el servicio responde `200` con una lista vacía

### Requirement: Eventos de integración de otorgamiento

El servicio SHALL publicar los eventos `OfertaBecaEmitida`, `BecaActivada`, `OfertaBecaRechazada`, `OfertaBecaAnulada`, `OfertaBecaExpirada`, `CompromisoBecarioCargado`, `CompromisoBecarioAprobado` y `CompromisoBecarioRechazado`, junto con los identificadores de la beca, la oferta, la postulación, el programa y el periodo, y sin exponer datos personales sensibles.

#### Scenario: Evento publicado tras una transición

- **WHEN** una oferta se acepta correctamente
- **THEN** se publica `BecaActivada` con `becaId`, `ofertaId`, `postulacionId`, `programaBecaId`, `periodoAcademicoId` y `occurredAt`
- **AND** el evento no contiene nombre, documento, correo ni datos bancarios del estudiante

#### Scenario: Fallo de publicación

- **WHEN** RabbitMQ no está disponible al confirmar la transición
- **THEN** el evento queda en Outbox y se publica cuando el broker se recupera
- **AND** el estado de la beca no se revierte
