# ms-pos/consolidacion Specification

## Purpose

Gestiona la consolidación automática, determinista e idempotente de las puntuaciones definitivas de evaluadores por postulación en `MS-POS`, cubriendo el soporte trazable de la excepción autorizada de evaluador único, la detección y revisión auditada de alertas de discrepancia entre evaluadores, las consultas con notas individuales asociadas y el aislamiento estricto por rol.

## ADDED Requirements

### Requirement: Consolidación automática y determinista de puntuaciones definitivas por postulación

El sistema `MS-POS` SHALL consolidar automáticamente un `ResultadoConsolidadoRanking` en estado `PROVISIONAL` cuando todas las asignaciones vigentes estén en `COMPLETADA` y se alcance el mínimo de evaluaciones `ENVIADA_DEFINITIVA` requerido (por defecto `2`, `RN-POS-04`). El `puntajeConsolidado` SHALL ser el promedio aritmético redondeado a 2 decimales (`MidpointRounding.AwayFromZero`) en `[0.00, 100.00]`, excluyendo borradores e invalidadas, y recalculándose idempotentemente en `PROVISIONAL`.

#### Scenario: Consolidación automática al completarse la segunda evaluación definitiva

- **GIVEN** una postulación con dos asignaciones vigentes en `EN_PROGRESO` donde el evaluador A ya envió su evaluación definitiva con `puntajeTotalPonderado = 80.00`
- **WHEN** el evaluador B envía su evaluación definitiva con `puntajeTotalPonderado = 90.00`
- **THEN** el envío definitivo responde `200 OK`
- **AND** el sistema crea automáticamente un `ResultadoConsolidadoRanking` en estado `PROVISIONAL` con `cantidadEvaluacionesValidas = 2`, `puntajeConsolidado = 85.00`, `diferenciaEntreEvaluadores = 10.00`, `excepcionEvaluadorUnico = false` y `fechaConsolidacion` UTC.

#### Scenario: No consolidación prematura cuando aún quedan evaluadores asignados pendientes

- **GIVEN** una postulación con dos asignaciones vigentes y sin excepción de evaluador único
- **WHEN** únicamente el primer evaluador envía su evaluación definitiva mientras el segundo permanece en `PENDIENTE` o `EN_PROGRESO`
- **THEN** el envío definitivo del primer evaluador se completa correctamente
- **AND** no se crea ningún `ResultadoConsolidadoRanking` para la postulación.

#### Scenario: Promedio aritmético con tres evaluadores y redondeo determinista

- **GIVEN** una postulación con tres asignaciones vigentes completadas cuyas evaluaciones `ENVIADA_DEFINITIVA` obtienen `80.00`, `80.01` y `80.01`
- **WHEN** se consolida la postulación
- **THEN** el sistema calcula `puntajeConsolidado = 80.01` (promedio `80.0066...` redondeado con `MidpointRounding.AwayFromZero` a 2 decimales), `cantidadEvaluacionesValidas = 3` y `diferenciaEntreEvaluadores = 0.01`.

#### Scenario: Recálculo determinista al completarse un evaluador adicional sobre un resultado provisional

- **GIVEN** una postulación ya consolidada en estado `PROVISIONAL` con 2 evaluaciones (`70.00` y `95.00`, con alerta de discrepancia revisada) a la cual el coordinador asigna un tercer evaluador
- **WHEN** el tercer evaluador envía su evaluación definitiva con `85.00`
- **THEN** el sistema recalcula automáticamente el `ResultadoConsolidadoRanking` existente con `cantidadEvaluacionesValidas = 3`, `puntajeConsolidado = 83.33`, `diferenciaEntreEvaluadores = 25.00`
- **AND** reinicia `discrepanciaRevisada = false` y limpia el comentario de revisión anterior al haberse modificado el conjunto de notas evaluadas.

#### Scenario: Exclusión estricta de borradores y evaluaciones invalidadas por conflicto

- **GIVEN** una postulación con dos evaluaciones en `ENVIADA_DEFINITIVA` (`80.00` y `90.00`) y una evaluación adicional en `INVALIDADA_POR_CONFLICTO` (`20.00`) cuya asignación está `REASIGNADA_POR_CONFLICTO`
- **WHEN** se consolida la postulación
- **THEN** el sistema utiliza únicamente las dos evaluaciones `ENVIADA_DEFINITIVA`, obteniendo `cantidadEvaluacionesValidas = 2`, `puntajeConsolidado = 85.00` y `diferenciaEntreEvaluadores = 10.00`.

### Requirement: Soporte trazable de excepción autorizada de evaluador único

El sistema `MS-POS` SHALL consolidar con `1` única evaluación `ENVIADA_DEFINITIVA` solo cuando todas las asignaciones vigentes estén completadas y exista una `AutorizacionExcepcionEvaluadorUnico` (`RN-POS-04`), registrando `excepcionEvaluadorUnico = true`, `justificacionEvaluadorUnico`, `autorizadorExcepcionId`, `diferenciaEntreEvaluadores = 0.00` y `alertaDiscrepancia = false`, incluso si la excepción se autoriza existiendo ya esa evaluación.

#### Scenario: Consolidación automática con excepción autorizada antes de la evaluación

- **GIVEN** una postulación con una `AutorizacionExcepcionEvaluadorUnico` registrada por el coordinador C con justificación válida y una única asignación vigente
- **WHEN** el único evaluador asignado envía su evaluación definitiva con `puntajeTotalPonderado = 88.50`
- **THEN** se consolida automáticamente un `ResultadoConsolidadoRanking` con `excepcionEvaluadorUnico = true`, `justificacionEvaluadorUnico` y `autorizadorExcepcionId = C`, `cantidadEvaluacionesValidas = 1`, `puntajeConsolidado = 88.50`, `diferenciaEntreEvaluadores = 0.00` y `alertaDiscrepancia = false`.

#### Scenario: Consolidación automática cuando la excepción se autoriza después de existir una evaluación definitiva

- **GIVEN** una postulación que ya cuenta con una única asignación vigente en `COMPLETADA` y su correspondiente evaluación `ENVIADA_DEFINITIVA` (`76.25`), sin excepción previa y sin `ResultadoConsolidadoRanking`
- **WHEN** el coordinador autoriza una `AutorizacionExcepcionEvaluadorUnico` válida para esa postulación
- **THEN** la autorización responde `201 Created`
- **AND** el sistema dispara automáticamente la consolidación creando el `ResultadoConsolidadoRanking` con `excepcionEvaluadorUnico = true`, `puntajeConsolidado = 76.25`, `diferenciaEntreEvaluadores = 0.00` y `alertaDiscrepancia = false`.

#### Scenario: Consolidación multi-evaluador prevalece cuando existen dos o más evaluaciones definitivas

- **GIVEN** una postulación que cuenta con dos evaluaciones `ENVIADA_DEFINITIVA` (`70.00` y `95.00`) aun cuando se hubiera registrado previamente una excepción de evaluador único
- **WHEN** se consolida la postulación
- **THEN** el sistema consolida en modo multi-evaluador con `excepcionEvaluadorUnico = false`, `justificacionEvaluadorUnico = null`, `autorizadorExcepcionId = null`, `cantidadEvaluacionesValidas = 2`, `puntajeConsolidado = 82.50` y `diferenciaEntreEvaluadores = 25.00`.

### Requirement: Detección automática de alertas de discrepancia entre evaluadores

El sistema `MS-POS` SHALL calcular `diferenciaEntreEvaluadores = max(notas) - min(notas)` sobre las evaluaciones `ENVIADA_DEFINITIVA` y activar `alertaDiscrepancia = true` únicamente cuando existan al menos 2 evaluaciones y `diferenciaEntreEvaluadores > umbralDiscrepanciaPuntos`. Si la diferencia es menor o igual al umbral, o aplica excepción de evaluador único, `alertaDiscrepancia` SHALL ser `false`, conservando el `umbralDiscrepanciaAplicado` en el resultado.

#### Scenario: Discrepancia que supera estrictamente el umbral configurado

- **GIVEN** un `umbralDiscrepanciaPuntos` vigente de `20.00` y dos evaluaciones definitivas con `60.00` y `80.01`
- **WHEN** se consolida la postulación
- **THEN** el resultado registra `diferenciaEntreEvaluadores = 20.01`, `umbralDiscrepanciaAplicado = 20.00`, `alertaDiscrepancia = true` y `discrepanciaRevisada = false`.

#### Scenario: Diferencia exactamente igual al umbral no genera alerta

- **GIVEN** un `umbralDiscrepanciaPuntos` vigente de `20.00` y dos evaluaciones definitivas con `65.00` y `85.00`
- **WHEN** se consolida la postulación
- **THEN** el resultado registra `diferenciaEntreEvaluadores = 20.00`, `umbralDiscrepanciaAplicado = 20.00`, `alertaDiscrepancia = false` y `discrepanciaRevisada = false`.

#### Scenario: Discrepancia con tres evaluadores calculada sobre extremos máximo y mínimo

- **GIVEN** un `umbralDiscrepanciaPuntos` vigente de `20.00` y tres evaluaciones definitivas con `55.00`, `72.00` y `81.00`
- **WHEN** se consolida la postulación
- **THEN** el resultado calcula `diferenciaEntreEvaluadores = 26.00` (`81.00 - 55.00`) y establece `alertaDiscrepancia = true`.

### Requirement: Revisión trazable de alertas de discrepancia por el coordinador

El sistema `MS-POS` SHALL permitir al `COORDINADOR_PROGRAMA` marcar como revisada la alerta de discrepancia de un `ResultadoConsolidadoRanking` en estado `PROVISIONAL` únicamente cuando `alertaDiscrepancia == true` y `discrepanciaRevisada == false`, exigiendo un `comentarioRevisionDiscrepancia` obligatorio de 10 a 1000 caracteres. El identificador `revisadoPorUsuarioId` SHALL tomarse del usuario autenticado y `fechaRevisionDiscrepancia` del reloj UTC del servidor.

#### Scenario: Revisión exitosa de una alerta de discrepancia activa

- **GIVEN** un `ResultadoConsolidadoRanking` en estado `PROVISIONAL` con `alertaDiscrepancia = true` y `discrepanciaRevisada = false`
- **WHEN** el `COORDINADOR_PROGRAMA` solicita marcar la discrepancia como revisada con un comentario válido de al menos 10 caracteres
- **THEN** el sistema responde `200 OK` con `discrepanciaRevisada = true`, `comentarioRevisionDiscrepancia` normalizado, `revisadoPorUsuarioId` del token, `fechaRevisionDiscrepancia` UTC y `version` incrementada.

#### Scenario: Rechazo cuando el resultado no tiene alerta de discrepancia

- **GIVEN** un `ResultadoConsolidadoRanking` con `alertaDiscrepancia = false`
- **WHEN** el coordinador intenta marcar una discrepancia como revisada
- **THEN** el sistema responde `422 Unprocessable Entity` con `codigo = DISCREPANCIA_NO_DETECTADA` y no modifica el registro.

#### Scenario: Rechazo cuando la discrepancia ya fue revisada previamente

- **GIVEN** un `ResultadoConsolidadoRanking` con `alertaDiscrepancia = true` y `discrepanciaRevisada = true`
- **WHEN** un coordinador intenta sobrescribir la revisión de discrepancia
- **THEN** el sistema responde `409 Conflict` con `codigo = DISCREPANCIA_YA_REVISADA` y preserva intactos el comentario, el revisor y la fecha originales.

#### Scenario: Rechazo por comentario ausente o fuera de longitud permitida

- **GIVEN** una solicitud de revisión de discrepancia con comentario vacío, solo espacios, menor de 10 caracteres o mayor de 1000 caracteres
- **WHEN** el sistema valida la solicitud
- **THEN** responde `400 Bad Request` (`ValidationProblemDetails` en API) o `422 Unprocessable Entity` con `codigo = COMENTARIO_REVISION_REQUERIDO` en dominio, y no persiste cambios.

#### Scenario: Rechazo sobre un resultado consolidado inexistente o ya confirmado

- **GIVEN** una `postulacionId` sin resultado consolidado o un `ResultadoConsolidadoRanking` en estado `CONFIRMADO`
- **WHEN** el coordinador intenta marcar la discrepancia como revisada
- **THEN** el sistema responde `404 Not Found` con `codigo = RESULTADO_CONSOLIDADO_NO_ENCONTRADO` o `409 Conflict` con `codigo = RANKING_YA_CONFIRMADO`, respectivamente.

### Requirement: Consolidación o recálculo manual por el coordinador

El sistema `MS-POS` SHALL permitir al `COORDINADOR_PROGRAMA` solicitar explícitamente la consolidación o recálculo idempotente de una postulación en estado `PROVISIONAL` cuando todas sus asignaciones vigentes estén completadas y alcance el mínimo efectivo de evaluaciones definitivas. SHALL rechazar la operación si existen asignaciones vigentes pendientes, evaluaciones insuficientes sin excepción o un resultado ya en estado `CONFIRMADO`.

#### Scenario: Consolidación manual exitosa creando o recalculando el resultado

- **GIVEN** una postulación con todas sus asignaciones vigentes en `COMPLETADA` y al menos el mínimo efectivo de evaluaciones `ENVIADA_DEFINITIVA`
- **WHEN** el `COORDINADOR_PROGRAMA` invoca `POST /api/v1/resultados-consolidados/postulaciones/{postulacionId}`
- **THEN** responde `201 Created` (con cabecera `Location` al detalle) si se creó por primera vez, o `200 OK` si el resultado ya existía en `PROVISIONAL` y se recalculó o verificó idempotente.

#### Scenario: Rechazo cuando existen asignaciones vigentes pendientes de evaluar

- **GIVEN** una postulación con dos asignaciones vigentes donde al menos una sigue en estado `PENDIENTE` o `EN_PROGRESO`
- **WHEN** el coordinador solicita la consolidación manual
- **THEN** el sistema responde `409 Conflict` con `codigo = EVALUACIONES_PENDIENTES` y no persiste ningún resultado consolidado.

#### Scenario: Rechazo cuando no se alcanza el mínimo de evaluaciones definitivas sin excepción

- **GIVEN** una postulación sin asignaciones pendientes pero con menos de `evaluadoresMinimosRequeridos` evaluaciones `ENVIADA_DEFINITIVA` y sin `AutorizacionExcepcionEvaluadorUnico` (o sin ninguna evaluación definitiva)
- **WHEN** el coordinador solicita la consolidación manual
- **THEN** el sistema responde `422 Unprocessable Entity` con `codigo = EVALUACIONES_INSUFICIENTES` y no persiste cambios.

#### Scenario: Rechazo de recálculo manual cuando el ranking ya fue confirmado

- **GIVEN** un `ResultadoConsolidadoRanking` persistido en estado `CONFIRMADO`
- **WHEN** el coordinador intenta recalcular la consolidación de esa postulación
- **THEN** el sistema responde `409 Conflict` con `codigo = RANKING_YA_CONFIRMADO` y no altera el registro.

### Requirement: Consultas de resultados consolidados, notas individuales y alertas por convocatoria

El sistema `MS-POS` SHALL permitir consultar el detalle del `ResultadoConsolidadoRanking` de una postulación junto con sus evaluaciones `ENVIADA_DEFINITIVA` individuales ordenadas (excluyendo borradores e invalidadas), así como listar paginadamente (`pagina >= 1`, `1 <= tamano <= 100`) los resultados consolidados de una convocatoria con filtros opcionales por `alertaDiscrepancia` y `discrepanciaRevisada`.

#### Scenario: Consulta de resultado consolidado por postulación con sus notas individuales

- **GIVEN** una postulación consolidada con dos evaluaciones `ENVIADA_DEFINITIVA` (`75.00` y `96.00`, alerta activa) y una evaluación previa en `INVALIDADA_POR_CONFLICTO`
- **WHEN** un usuario autorizado consulta `GET /api/v1/resultados-consolidados/postulaciones/{postulacionId}`
- **THEN** el sistema responde `200 OK` con todos los campos del `ResultadoConsolidadoRanking` (incluido `estadoConfirmacionRanking = PROVISIONAL`, `umbralDiscrepanciaAplicado` y datos de excepción/revisión)
- **AND** adjunta exactamente las dos evaluaciones `ENVIADA_DEFINITIVA` ordenadas deterministamente por `(fechaEnvio, evaluacionId)` con `evaluacionId`, `asignacionEvaluacionId`, `evaluadorId`, `puntajeTotalPonderado`, `fechaEnvio` y `comentariosGenerales`.

#### Scenario: Consulta por postulación sin resultado consolidado

- **GIVEN** una `postulacionId` que todavía no cuenta con un `ResultadoConsolidadoRanking`
- **WHEN** un usuario autorizado consulta `GET /api/v1/resultados-consolidados/postulaciones/{postulacionId}`
- **THEN** el sistema responde `404 Not Found` con `codigo = RESULTADO_CONSOLIDADO_NO_ENCONTRADO`.

#### Scenario: Listado paginado por convocatoria filtrado por alertas pendientes de revisión

- **GIVEN** una convocatoria con múltiples resultados consolidados (algunos sin alerta, otros con alerta pendiente y otros con alerta ya revisada)
- **WHEN** un usuario autorizado consulta `GET /api/v1/resultados-consolidados/convocatorias/{convocatoriaId}?alertaDiscrepancia=true&discrepanciaRevisada=false&pagina=1&tamano=20`
- **THEN** el sistema responde `200 OK` con una página que incluye únicamente los resultados con `alertaDiscrepancia = true` y `discrepanciaRevisada = false`, ordenados deterministamente por `(puntajeConsolidado DESC, fechaConsolidacion ASC, id ASC)`, junto con `pagina`, `tamano` y `total`.

#### Scenario: Rechazo de parámetros de consulta inválidos

- **GIVEN** una consulta con `convocatoriaId` o `postulacionId` vacío, `pagina < 1`, `tamano < 1` o `tamano > 100`
- **WHEN** se invoca cualquiera de los endpoints de consulta
- **THEN** el sistema responde `400 Bad Request` (`ValidationProblemDetails`).

### Requirement: Autorización por rol sobre la consolidación y alertas de discrepancia

El sistema `MS-POS` SHALL exigir `COORDINADOR_PROGRAMA` o `DIRECCION` (claim `roles`) para consultar resultados consolidados y alertas, y exclusivamente `COORDINADOR_PROGRAMA` para revisar discrepancias o consolidar manualmente. Todo usuario autenticado sin el rol requerido —incluidos `MIEMBRO_COMITE` y `ADMIN_FINANCIERO`— SHALL recibir `403 Forbidden` con `codigo = PERMISO_DENEGADO` en `ProblemDetails`; sin token SHALL responder `401 Unauthorized`.

#### Scenario: Dirección consulta en solo lectura y es denegada en escritura

- **GIVEN** un usuario autenticado con rol `DIRECCION`
- **WHEN** consulta `GET /api/v1/resultados-consolidados/postulaciones/{postulacionId}` o `GET /api/v1/resultados-consolidados/convocatorias/{convocatoriaId}`
- **THEN** la autorización se concede y la consulta se ejecuta
- **AND** cuando intenta invocar `POST /api/v1/resultados-consolidados/postulaciones/{postulacionId}` o `POST /api/v1/resultados-consolidados/postulaciones/{postulacionId}/discrepancia/revisar`, el sistema responde `403 Forbidden` con `codigo = PERMISO_DENEGADO`.

#### Scenario: Miembro de comité y administrador financiero denegados en todos los endpoints

- **GIVEN** un usuario autenticado con rol `MIEMBRO_COMITE` o `ADMIN_FINANCIERO`
- **WHEN** invoca cualquier endpoint de lectura o escritura de resultados consolidados
- **THEN** el sistema responde `403 Forbidden` con `codigo = PERMISO_DENEGADO` en `ProblemDetails` y no ejecuta la operación.

#### Scenario: Solicitud sin autenticación

- **GIVEN** una solicitud sin token JWT válido
- **WHEN** invoca cualquier endpoint de `api/v1/resultados-consolidados`
- **THEN** el sistema responde `401 Unauthorized`.

### Requirement: Persistencia íntegra e idempotencia bajo concurrencia C5

El sistema `MS-POS` SHALL persistir `ResultadoConsolidadoRanking` en `postulaciones.resultados_consolidados_ranking` con `timestamptz`, `numeric(5,2)`, enum `estado_confirmacion_ranking` en mayúsculas, unicidad 1:1 por `postulacion_id`, concurrencia optimista por `version` y `CHECK` de coherencia. Ante el envío definitivo simultáneo de dos evaluadores sobre la misma postulación (`C5`), ambos envíos SHALL finalizar sin error y SHALL persistirse una única fila consolidada.

#### Scenario: Envío simultáneo de dos evaluadores sobre la misma postulación (concurrencia C5)

- **GIVEN** una postulación con dos asignaciones vigentes en `EN_PROGRESO` cuyos borradores completos (`72.00` y `96.00`) están listos para envío definitivo
- **WHEN** ambos evaluadores ejecutan el envío definitivo de forma simultánea contra PostgreSQL
- **THEN** ambos envíos finalizan correctamente sin lanzar conflicto al cliente
- **AND** en `postulaciones.resultados_consolidados_ranking` existe exactamente una fila para la postulación con `cantidadEvaluacionesValidas = 2`, `puntajeConsolidado = 84.00`, `diferenciaEntreEvaluadores = 24.00` y `alertaDiscrepancia = true`.

#### Scenario: Violación de restricciones de integridad en PostgreSQL

- **GIVEN** un intento directo de insertar una segunda fila para la misma `postulacion_id`, o una fila con `discrepancia_revisada = true` teniendo `alerta_discrepancia = false`, o `excepcion_evaluador_unico = true` sin justificación o con `alerta_discrepancia = true`, o `puntaje_consolidado` fuera de `[0.00, 100.00]`, o `estado_confirmacion_ranking` fuera de catálogo
- **WHEN** se intenta confirmar en PostgreSQL
- **THEN** la base de datos rechaza la operación por índice único (traducido a `RESULTADO_CONSOLIDADO_DUPLICADO`) o por la restricción `CHECK` correspondiente.

#### Scenario: Conflicto de concurrencia optimista al revisar la misma discrepancia en simultáneo

- **GIVEN** un `ResultadoConsolidadoRanking` con alerta activa cargado simultáneamente por dos coordinadores
- **WHEN** ambos intentan registrar su revisión de discrepancia concurrentemente
- **THEN** la primera transacción confirma incrementando `version` y la segunda recibe `409 Conflict` (`CONFLICTO_CONCURRENCIA`) sin sobrescribir la revisión confirmada.
