# Contrato del modelo de seguimiento académico de MS-BEC

`ReporteAcademico` registra el rendimiento de una beca por periodo. Además de
los campos del DDD, acepta `horasServicioCumplidas` opcional. Cuando el valor
es positivo, exige `urlComprobanteServicio` como referencia `gs://` y
`hashComprobanteServicioSha256` de 64 caracteres hexadecimales. El binario no
se guarda en PostgreSQL. Si la política exige horas y el reporte no las
declara, la evaluación falla cerrada; cero horas declaradas se evalúan como
incumplimiento cuando el mínimo es positivo.

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
