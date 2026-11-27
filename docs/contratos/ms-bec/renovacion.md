# Modelo de renovación de MS-BEC

`SolicitudRenovacion` es el agregado de la decisión de continuidad entre dos
periodos distintos de una beca. Nace en `SOLICITADA_EN_CURSO` y puede terminar
en `APROBADA`, `RECHAZADA` o `EXPIRADA_CONCLUIDA`. Los estados finales no se
reabren. La solicitud guarda la fecha límite oficial de la ventana, los
resultados de cumplimiento académico y disponibilidad presupuestaria, el monto
solicitado, el monto y la referencia de la reserva confirmada, la decisión, su autor y la referencia
del job de vencimiento.
La migración añade `monto_solicitado` con valor `0` para solicitudes antiguas
que no conservaron ese dato; esas solicitudes no se aprueban sin una nueva
solicitud verificable. Las solicitudes nuevas exigen un monto positivo con dos
decimales como máximo.

La solicitud solo puede crearse para una beca `ACTIVA`, dentro de la ventana
oficial de `CalendarioAcademicoPrograma`, con un `ReporteAcademico` aprobado
del periodo de origen cuya `EvaluacionPermanencia` esté confirmada y cumpla
todas las condiciones, y con disponibilidad presupuestaria confirmada por
MS-PRG. Una consulta preliminar de presupuesto no reemplaza la reserva atómica
de 19.5. Si faltan la ventana o la respuesta presupuestaria, MS-BEC falla
cerrado; no crea una solicitud ni una beca sucesora. El contrato de consulta
por periodo con MS-PRG sigue pendiente de acuerdo entre servicios.

La aprobación exige cumplimiento académico, presupuesto disponible, datos
oficiales del periodo siguiente y una reserva confirmada de monto positivo.
La decisión, la transición de la beca anterior a `RENOVADA`, la creación de
su sucesora `ACTIVA` y el calendario se confirman en una transacción local.
El rechazo ordena liberar una reserva existente antes del cambio local.

Para la reserva de renovación, MS-BEC envía `referenciaTipo = RENOVACION`,
`referenciaId = solicitudRenovacionId` e `Idempotency-Key:
renovacion-{solicitudRenovacionId}` al contrato interno de reservas. Las
transiciones de la reserva reutilizan el identificador de la solicitud para
sus claves de idempotencia. Este cliente tiene pruebas de contrato locales. La aprobación propia de
MS-BEC se prueba con dobles de los puertos externos; el adaptador de datos
del nuevo periodo falla cerrado, así que la API real aún no aprueba con
la implementación actual de MS-PRG.
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
límite el monto comprometido de esa propia beca. La aprobación local genera
el calendario con el plan oficial recibido por su puerto; falta conectar el
adaptador real de MS-PRG.

`EXPIRADA_CONCLUIDA` solo puede registrarse en una solicitud existente que
continúa en curso después de su fecha límite. El job recurrente
`ReconciliarRenovacionesJob` busca solicitudes vencidas por lotes y despacha
un caso de uso idempotente que confirma la ventana y la política tardía
oficiales. Con bloqueo y transacción local, libera primero la reserva si
existe y exige a MS-PRG confirmar la liberación del cupo ocupado antes de
expirar la solicitud y concluir la beca. Si faltan datos oficiales o falla
MS-PRG, conserva los estados para reintentar; continúa con las demás
solicitudes del lote. Si vence la ventana sin solicitud, el job recurrente
`DescubrirConclusionesRenovacionJob` obtiene el periodo siguiente y la ventana
desde un puerto oficial, recorre las becas activas sin solicitud y despacha
el caso de uso idempotente de conclusión. No infiere periodos desde la beca ni
concluye con ventanas inactivas. El adaptador productivo del periodo siguiente
permanece no disponible hasta acordar la fuente oficial. También falta
reconciliar reservas huérfanas tras una caída entre servicios.

La conclusión por vencimiento relee la beca con bloqueo, consulta la ventana
oficial del periodo siguiente y exige el valor explícito de
`permiteRenovacionTardiaExcepcional`. Si la excepción está activa, el plazo no
venció o existe una solicitud, conserva la beca. Si corresponde concluir,
MS-PRG debe confirmar primero la liberación idempotente del cupo **ocupado**
por esa beca y periodo; ante respuesta ausente o negativa, MS-BEC conserva
`ACTIVA` para reintentar. El endpoint `/liberar` existente de MS-PRG libera
reservas pendientes y no sirve para un cupo ocupado. Ambos jobs están
programados, pero no pueden concluir en producción hasta recibir la ventana
oficial y la confirmación financiera que MS-PRG aún no publica.

El comando local de solicitud persiste la solicitud y marca
`Beca.TieneRenovacionEnCurso` en una transacción con bloqueo de la beca. El
rechazo sin reserva desmarca esa condición. La aprobación valida nuevamente
la evaluación académica y evita el doble beneficio. Reserva, confirma y activa
por el puerto financiero antes de persistir la sucesora, el calendario y los
eventos. Si falla la transacción local, intenta liberar la reserva. Una caída
de proceso entre la activación externa y el commit local requiere reconciliación
entre servicios; aún falta el contrato de MS-PRG para verificar ese caso.

La API ofrece `POST /api/v1/becas/{id}/renovaciones` al becario dueño,
`POST /api/v1/renovaciones/{id}/evaluacion` al coordinador con alcance y 2FA,
`GET /api/v1/renovaciones/{id}` y
`GET /api/v1/becas/{id}/renovaciones?pagina=&tamano=` al dueño o a coordinación
con alcance. El listado limita `tamano` a 50 y no expone datos personales.
`RenovacionBecaSolicitada`, `RenovacionBecaAprobada`,
`BecaActivadaNuevoPeriodo`, `DesembolsoProgramado` y
`BecaConcluidaPorVencimientoRenovacion` se encolan en el Outbox junto con la
transacción local correspondiente. Los eventos de aprobación y activación
requieren reserva confirmada y sucesora creada.

Las pruebas end-to-end locales con API, PostgreSQL y job cubren la solicitud,
la aprobación con dobles de reserva y plan oficial, la sucesora y su calendario,
el rechazo, una reserva inválida, el vencimiento sin solicitud y la
reconciliación idempotente de una solicitud vencida. También verifican que la
excepción de renovación tardía conserva la solicitud en curso. La prueba con
los contratos reales de MS-PRG sigue pendiente.
