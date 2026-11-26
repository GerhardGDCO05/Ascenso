# Contrato del modelo de seguimiento académico de MS-BEC

`ReporteAcademico` registra el rendimiento de una beca por periodo. Además de
los campos del DDD, acepta `horasServicioCumplidas` opcional. Cuando el valor
es positivo, exige `urlComprobanteServicio` como referencia estable del proveedor de archivos y
`hashComprobanteServicioSha256` de 64 caracteres hexadecimales. El binario no
se guarda en PostgreSQL. Si la política exige horas y el reporte no las
declara, la evaluación falla cerrada; cero horas declaradas se evalúan como
incumplimiento cuando el mínimo es positivo.

Las referencias admiten `gs://` para Google Cloud Storage y `file://` o una
URL HTTP(S) estable del almacenamiento local. Se rechazan parámetros de
consulta y fragmentos para no persistir enlaces firmados temporales.

`EvaluacionPermanencia` es una entidad hija de `ReporteAcademico`. Compara las
cuatro condiciones del snapshot de la oferta: promedio mínimo, materias
aprobadas mínimas, máximo de materias reprobadas y horas de servicio por
periodo. Cada resultado conserva condición, valor requerido, valor obtenido y
si se cumple. Con todas las condiciones satisfechas no recomienda ninguna
consecuencia; en caso contrario recomienda la consecuencia configurada en el
snapshot. La recomendación no cambia por sí sola el estado de la beca.

La decisión del coordinador registra el motivo y el siguiente paso visible
para el becario. Una consecuencia confirmada queda pendiente de aplicación
hasta que el caso de uso ejecute el cambio correspondiente; el modelo distingue
`consecuenciaRecomendada` de `consecuenciaAplicada`. El reporte no se puede
aprobar sin evaluación confirmada. Los códigos locales de las enumeraciones
requieren revisión con los consumidores antes de publicarse como contrato
compartido.

La API expone `POST /api/v1/becas/{becaId}/reportes-academicos` con formulario
multipart y `POST /api/v1/becas/{becaId}/reportes-academicos/{reporteId}/evaluaciones`
para coordinación con 2FA. La carga exige PDF de hasta 5 MB, firma `%PDF-` y
SHA-256 coincidente; el becario debe ser dueño de la beca. La evaluación exige
alcance sobre el programa. Las consultas de reporte y evaluación verifican la
misma propiedad o alcance antes de devolver datos.

La fecha límite se obtiene de `CalendarioAcademicoPrograma` del programa y
periodo. La solicitud de registro no puede proporcionarla. Si MS-PRG no la
publica, MS-BEC responde `422 CONFIGURACION_PROGRAMA_NO_DISPONIBLE` y no guarda
el reporte. Las condiciones se evalúan contra el snapshot inmutable de la
oferta, aunque MS-PRG cambie después su configuración.

`ReporteAcademicoEntregado` anuncia cada entrega sin datos personales ni
calificaciones; `AlertaReporteAcademicoTardio` se emite solo cuando la fecha
de recepción supera la fecha límite y solicita a MS-PLT alertar a la
coordinación del programa. La compatibilidad y el consumo real en MS-PLT
siguen pendientes de validación con ese servicio.

`EvaluacionPermanenciaRegistrada` anuncia el resultado y la consecuencia
recomendada sin publicar calificaciones ni datos socioeconómicos.
`ConsecuenciaPermanenciaAplicada` se emite solo al aplicar la consecuencia
confirmada. El llamado de atención conserva su trazabilidad en
`EvaluacionPermanencia`, la auditoría de `ReporteAcademico` y el evento. Según
el DDD, `HistorialEstadoBeca` registra únicamente transiciones de estado de
beca, oferta o compromiso; una suspensión o revocación académica se registrará
allí cuando se ejecute su transición real. La entrega repetida con igual
`Idempotency-Key` y contenido devuelve la respuesta anterior sin ejecutar de
nuevo el comando ni encolar otro evento.
