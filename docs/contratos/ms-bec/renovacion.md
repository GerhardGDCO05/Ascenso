# Modelo de renovación de MS-BEC

`SolicitudRenovacion` es el agregado de la decisión de continuidad entre dos
periodos distintos de una beca. Nace en `SOLICITADA_EN_CURSO` y puede terminar
en `APROBADA`, `RECHAZADA` o `EXPIRADA_CONCLUIDA`. Los estados finales no se
reabren. La solicitud guarda la fecha límite oficial de la ventana, los
resultados de cumplimiento académico y disponibilidad presupuestaria, el monto
y la referencia de la reserva confirmada, la decisión, su autor y la referencia
del job de vencimiento.

La solicitud solo puede crearse para una beca `ACTIVA`, dentro de la ventana
oficial de `CalendarioAcademicoPrograma`, con un `ReporteAcademico` aprobado
del periodo de origen cuya `EvaluacionPermanencia` esté confirmada y cumpla
todas las condiciones, y con disponibilidad presupuestaria confirmada por
MS-PRG. Una consulta preliminar de presupuesto no reemplaza la reserva atómica
de 19.5. Si faltan la ventana o la respuesta presupuestaria, MS-BEC falla
cerrado; no crea una solicitud ni una beca sucesora. El contrato de consulta
por periodo con MS-PRG sigue pendiente de acuerdo entre servicios.

La aprobación exige cumplimiento académico, presupuesto disponible y reserva
confirmada de monto positivo, además de ocurrir antes del vencimiento. La
aprobación de la solicitud y la transición de la beca anterior a `RENOVADA`,
con creación de su sucesora `ACTIVA`, deberán confirmarse juntas en la misma
transacción local. El rechazo conserva la referencia de la reserva para que el
caso de uso ordene su liberación en MS-PRG.

Para la reserva de renovación, MS-BEC envía `referenciaTipo = RENOVACION`,
`referenciaId = solicitudRenovacionId` e `Idempotency-Key:
renovacion-{solicitudRenovacionId}` al contrato interno de reservas. Las
transiciones de la reserva reutilizan el identificador de la solicitud para
sus claves de idempotencia. Este cliente tiene pruebas de contrato locales;
no se habilita la aprobación de renovaciones con la API actual de MS-PRG.
Hoy esa API expone `api/v1/programas`, recibe `referenciaExternaId`, un
periodo como texto y `descripcion`, no expone `/activar`, y `/confirmar`
registra ejecución presupuestaria como desembolso. Es necesario que MS-PRG
publique el ciclo de reserva definido por el DDD antes de conectarlo al caso
de uso de renovación. MS-BEC no debe interpretar esa confirmación actual como
reserva confirmada para una beca sucesora.

La sucesora conserva `cadenaBecaId`, apunta a `becaAnteriorId`, incrementa
`periodosCursados` y guarda sus propios snapshots de cobertura, costo y condiciones
del nuevo periodo. Nace directamente `ACTIVA`, sin una nueva `OfertaBeca`.
Puede heredar la aprobación del compromiso firmado anterior, salvo que el
programa exija una firma nueva; en ese caso queda sin habilitación de
desembolso hasta aprobar el nuevo compromiso. La evaluación académica usa las
condiciones propias de la sucesora. El calendario de desembolsos requiere
datos oficiales del nuevo periodo y el reporte académico se registrará cuando
la universidad lo entregue; la renovación no genera un reporte ficticio.
Al programar el calendario de una sucesora sin `OfertaBeca`, MS-BEC usa como
límite el monto comprometido de esa propia beca. Generar ese calendario durante
la aprobación sigue pendiente de los datos oficiales del nuevo periodo.

`EXPIRADA_CONCLUIDA` solo puede registrarse en una solicitud existente que
continúa en curso después de su fecha límite. Si vence la ventana sin que el
becario haya presentado una solicitud, el job de conclusión deberá cerrar la
beca conforme al DDD sin crear una solicitud ficticia. La programación y
ejecución de jobs corresponde a los casos de uso de 19.3 y 19.6; el agregado
solo almacena su referencia.

La conclusión por vencimiento relee la beca con bloqueo, consulta la ventana
oficial del periodo siguiente y exige el valor explícito de
`permiteRenovacionTardiaExcepcional`. Si la excepción está activa, el plazo no
venció o existe una solicitud, conserva la beca. Si corresponde concluir,
MS-PRG debe confirmar primero la liberación idempotente del cupo **ocupado**
por esa beca y periodo; ante respuesta ausente o negativa, MS-BEC conserva
`ACTIVA` para reintentar. El endpoint `/liberar` existente de MS-PRG libera
reservas pendientes y no sirve para un cupo ocupado. El job está preparado,
pero su programación requiere la ventana oficial y el adaptador financiero
que MS-PRG aún no publica.

El comando local de solicitud persiste la solicitud y marca
`Beca.TieneRenovacionEnCurso` en una transacción con bloqueo de la beca. El
rechazo sin reserva desmarca esa condición. Mientras no esté implementada la
reserva de renovación en MS-PRG, una aprobación o un rechazo que deba liberar
una reserva fallan cerrado: ninguna de esas rutas crea una sucesora ni altera
el presupuesto sin confirmación.

La API ofrece `POST /api/v1/becas/{id}/renovaciones` al becario dueño,
`POST /api/v1/renovaciones/{id}/evaluacion` al coordinador con alcance y 2FA,
`GET /api/v1/renovaciones/{id}` y
`GET /api/v1/becas/{id}/renovaciones?pagina=&tamano=` al dueño o a coordinación
con alcance. El listado limita `tamano` a 50 y no expone datos personales.
`RenovacionBecaSolicitada` y `BecaConcluidaPorVencimientoRenovacion` se
encolan en el Outbox junto con la transacción local. Los eventos de aprobación
y activación esperan la reserva real y la creación de la sucesora.

Las pruebas end-to-end locales con API, PostgreSQL y job cubren la solicitud y
el rechazo, el bloqueo de la aprobación sin reserva real y la conclusión por
vencimiento sin solicitud. La cadena y los montos de dos periodos se verifican
en pruebas de persistencia; la aprobación completa por API y la generación
transaccional de la sucesora con su calendario siguen pendientes del contrato
de reserva de MS-PRG y de los datos oficiales del nuevo periodo.
