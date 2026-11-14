# Specification: MS-BECA (Microservicio de Becas y Seguimiento)

> En la especificación maestra este contexto se identifica como `MS-BEC`. La carpeta `ms-beca` agrupa las especificaciones del microservicio de becas.

## Context

Gestión del otorgamiento y del ciclo de vida de la beca: emisión y respuesta de la oferta, firma del compromiso del becario, activación con la regla de no doble beneficio, programación y confirmación de desembolsos, suspensión y revocación, seguimiento académico, evaluación de permanencia, renovación en cadena y pausas o aplazamientos.

## Invariants

* Una `Beca` solo existe en uno de los seis estados oficiales (`OTORGADA`, `ACTIVA`, `SUSPENDIDA`, `RENOVADA`, `CONCLUIDA`, `REVOCADA`) y solo transita por las duplas permitidas de la Tabla 3.2. `RENOVADA`, `CONCLUIDA` y `REVOCADA` son terminales para la fila del periodo académico.
* No se permite el doble beneficio: un estudiante no puede tener dos becas `ACTIVA` con cobertura `TOTAL`, ni dos becas `ACTIVA` en el mismo `PeriodoAcademico` salvo que ambos programas tengan `permiteAcumulacion == true` (`RN-BEC-01`, `DEC-05`).
* Una `OfertaBeca` solo puede aceptarse si permanece en `PENDIENTE_RESPUESTA` y no ha superado su `fechaVencimientoAceptacion`, fijada a 15 días desde la emisión.
* Si el programa exige compromiso firmado, la beca no habilita desembolsos hasta que exista un `CompromisoBecario` en estado `APROBADO` (`RN-BEC-02`).
* La suma de cuotas programadas de un `CalendarioDesembolso` no puede superar el monto máximo aprobado del periodo; la suma de cuotas en proceso y pagadas tampoco (`RN-BEC-03`).
* Confirmar un desembolso exige una `AutorizacionOTP` válida. Un desembolso fallido o devuelto no cuenta como desembolsado y requiere observación bancaria antes de reintentarse (`RN-BEC-04`).
* La política de cobertura, las condiciones de permanencia y el costo de matrícula se capturan como fotografía inmutable al emitir la oferta; los cambios posteriores del programa no alteran ofertas ni becas ya emitidas.
* Toda transición con justificación obligatoria exige un motivo válido y se registra en un `HistorialEstadoBeca` append-only, sin actualización ni borrado.
* Las entidades del dominio no exponen setters públicos; las mutaciones se realizan mediante métodos de comportamiento que validan las reglas.

## Scenarios

<!-- OpenCode inyectará los escenarios GIVEN/WHEN/THEN detallados al archivar cada HU -->

## Behaviors / Business Rules

### 1. Otorgamiento y respuesta a la oferta (RF-15, RF-16)

* **Emisión:** solo se emite una oferta si la postulación es adjudicable, el convenio está vigente y `MS-PRG` confirma una reserva de cupo y presupuesto. En la misma operación se crea la `Beca` en `OTORGADA` y la `OfertaBeca` en `PENDIENTE_RESPUESTA`, con `cadenaBecaId = Beca.id`, `becaAnteriorId = null`, `periodosCursados = 0` y vencimiento a 15 días.
* **Aceptación:** dentro del plazo, la oferta pasa a `ACEPTADA` y la beca a `ACTIVA` con `fechaActivacion`; se valida el no doble beneficio con bloqueo por estudiante y aislamiento serializable.
* **Rechazo:** la oferta pasa a `RECHAZADA_POR_POSTULANTE`; la beca pasa a `REVOCADA` con `tipoCierre = OFERTA_RECHAZADA` y se libera la reserva.
* **Anulación:** un coordinador puede anular una oferta pendiente con justificación de al menos 10 caracteres; la oferta pasa a `ANULADA_POR_COORDINACION`, la beca a `REVOCADA` con `tipoCierre = OFERTA_ANULADA` y se libera la reserva.
* **Expiración:** al cumplirse los 15 días, el job correspondiente pasa la oferta a `EXPIRADA`, la beca a `REVOCADA` con `tipoCierre = OFERTA_EXPIRADA` y libera la reserva. Si la oferta ya fue respondida, el job no actúa.

### 2. Compromiso del becario (RF-17)

* El compromiso recorre `PENDIENTE_CARGA → EN_REVISION → APROBADO` o `RECHAZADO`; un rechazo permite una nueva carga incrementando `numeroIntentoCarga`.
* La aprobación del compromiso habilita `Beca.habilitadaParaDesembolso`; sin compromiso aprobado, cualquier intento de desembolso se rechaza con `412 COMPROMISO_NO_FIRMADO`.
* El compromiso conserva la URL del PDF firmado, el hash SHA-256, la IP de carga, la fecha y el coordinador revisor.

### 3. Ciclo de vida y no doble beneficio (RN-BEC-01, DEC-05)

* Las transiciones válidas de `Beca` son: `∅ → OTORGADA`, `∅ → ACTIVA` (sucesora de renovación), `OTORGADA → ACTIVA`, `OTORGADA → REVOCADA`, `ACTIVA → SUSPENDIDA`, `SUSPENDIDA → ACTIVA`, `ACTIVA → RENOVADA`, `ACTIVA → CONCLUIDA`, `ACTIVA → REVOCADA` y `SUSPENDIDA → REVOCADA`.
* La aceptación de la oferta valida que no exista otra beca `ACTIVA` con cobertura `TOTAL` ni otra beca `ACTIVA` en el mismo periodo cuando la acumulación no esté permitida.
* Los estados terminales no admiten nuevas transiciones; cada cambio genera una fila de `HistorialEstadoBeca` con estado anterior, estado nuevo, fecha, actor, motivo, OTP cuando aplique y marca de terminal irreversible.

### 4. Desembolsos y pagos (RF-19, RF-20, RF-21)

* El `CalendarioDesembolso` define frecuencia, destinatario, monto máximo aprobado, número de cuotas y fechas de inicio y fin; la suma de cuotas programadas no puede exceder el monto máximo.
* Cada `Desembolso` recorre los estados operativos de la Tabla 3.3; la confirmación de pago exige `AutorizacionOTP` y registra quién confirma y cuándo.
* Un pago fallido o devuelto exige observación bancaria, no suma como desembolsado y debe resolverse antes de reintentarse.
* La suspensión detiene las cuotas programadas y transfiere el saldo y el cupo a las bolsas de suspensión; la reanudación restaura las cuotas y los saldos.
* Confirmar un desembolso y revocar la beca se serializan sobre la fila de `Beca` para evitar que un pago confirmado coexista con la liberación del mismo presupuesto.

### 5. Revocación definitiva (RF-22)

* Procede por incumplimiento grave, datos falsos o retiro definitivo; exige justificación y `AutorizacionOTP` y deja `tipoCierre = REVOCACION_DEFINITIVA`.
* Cancela las cuotas futuras, libera el saldo no ejecutado y el cupo del programa, y no genera deuda ni cobro automático por los montos ya pagados.

### 6. Seguimiento académico y permanencia (RF-24, RF-25)

* El `ReporteAcademico` registra promedio, materias aprobadas, reprobadas y cursando, créditos, estado de matrícula, comprobante, fuente, universidad y fecha de entrega.
* Una entrega posterior a `fechaLimitePeriodo` marca `entregaTardia = true` y alerta al coordinador.
* La `EvaluacionPermanencia` compara cada condición con lo reportado y recomienda una consecuencia (`LLAMADO_ATENCION`, `SUSPENSION` o `REVOCACION`); la consecuencia aplicada requiere validación del coordinador y OTP cuando sea revocación.

### 7. Renovación en cadena y pausa (RF-26, RF-27)

* La `SolicitudRenovacion` solo se habilita dentro de la ventana del calendario y exige cumplimiento académico y presupuesto disponible.
* Al aprobarse, la beca saliente pasa a `RENOVADA` y se crea una beca sucesora en `ACTIVA` con el mismo `cadenaBecaId`, `becaAnteriorId = becaSaliente.id` y `periodosCursados + 1`.
* Si la ventana vence sin solicitud, la beca pasa a `CONCLUIDA`, salvo la excepción de renovación tardía contemplada por el programa.
* La `PausaAplazamiento` aprobada suspende la beca; la reactivación exige un nuevo `ReporteAcademico` con `EvaluacionPermanencia.cumpleCondiciones == true`.

### 8. Integraciones de negocio

* `MS-POS` responde vía REST interno (`GET /internal/v1/postulaciones/{id}/adjudicable`) para confirmar la adjudicabilidad antes de emitir oferta, recibe `POST /internal/v1/postulaciones/{id}/otorgar` (con `Idempotency-Key`) para transicionar la postulación a `OTORGADA`, y consulta `GET /internal/v1/becas/activas` para prevalidar el no doble beneficio.
* `MS-PRG` responde vía REST interno (`GET /internal/v1/programas/{id}/condiciones?carreraId=&periodoId=`) para validar arancel vigente, cobertura y vigencia de convenio, gestiona el ciclo de `ReservaPresupuestoCupo` (`POST /internal/v1/programas/{id}/reservas` y `/reservas/{reservaId}/{confirmar|activar|liberar}`) y registra los movimientos contables de desembolsos, suspensiones, revocaciones y conclusiones (`POST /internal/v1/programas/{id}/movimientos-presupuesto`), todos con `Idempotency-Key`.
* `MS-PLT` expone `GET /internal/v1/periodos-academicos/actual`, valida y consume las autorizaciones OTP mediante `POST /internal/v1/otp/consumir` (con `Idempotency-Key`), y recibe por RabbitMQ los eventos de hitos para notificaciones y actualización de `PlazoVigilado`.
* `MS-AUD` recibe por RabbitMQ (*fire-and-forget* con *Idempotent Inbox*) los eventos de historial y auditoría de las transiciones y decisiones.
