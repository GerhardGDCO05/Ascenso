# ms-pos/consolidacion Specification

## Purpose

Gestiona la consolidación automática, determinista e idempotente de las puntuaciones definitivas de evaluadores por postulación en `MS-POS`, cubriendo el soporte trazable de la excepción autorizada de evaluador único, la detección y revisión auditada de alertas de discrepancia entre evaluadores, las consultas con notas individuales asociadas y el aislamiento estricto por rol.

## Requirements

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

El sistema `MS-POS` SHALL exigir `COORDINADOR_PROGRAMA` o `DIRECCION` para consultar consolidados, alertas y ranking, solo `COORDINADOR_PROGRAMA` para revisar discrepancias, consolidar manualmente o confirmar el ranking, y `COORDINADOR_PROGRAMA` o la jefatura activa del comité (`ComiteEvaluador.jefaturaUsuarioId`) para resolver casos límite. Para `/internal/v1/postulaciones/{postulacionId}/adjudicable` SHALL aceptar `COORDINADOR_PROGRAMA`, `DIRECCION` o token de servicio. Otros actores SHALL recibir `403 PERMISO_DENEGADO`; sin token `401`.

#### Scenario: Dirección consulta en solo lectura y es denegada en escritura

- **GIVEN** un usuario autenticado con rol `DIRECCION`
- **WHEN** consulta `GET /api/v1/resultados-consolidados/postulaciones/{postulacionId}`, `GET /api/v1/resultados-consolidados/convocatorias/{convocatoriaId}` o `GET /api/v1/rankings/convocatorias/{convocatoriaId}`
- **THEN** la autorización se concede y la consulta se ejecuta
- **AND** cuando intenta invocar `POST /api/v1/resultados-consolidados/postulaciones/{postulacionId}`, `POST /api/v1/resultados-consolidados/postulaciones/{postulacionId}/discrepancia/revisar`, `POST /api/v1/rankings/convocatorias/{convocatoriaId}/casos-limite/resolver` o `POST /api/v1/rankings/convocatorias/{convocatoriaId}/confirmar`, el sistema responde `403 Forbidden` con `codigo = PERMISO_DENEGADO`.

#### Scenario: Miembro de comité y administrador financiero denegados en todos los endpoints

- **GIVEN** un usuario autenticado con rol `MIEMBRO_COMITE` o `ADMIN_FINANCIERO`
- **WHEN** invoca cualquier endpoint de lectura o escritura de resultados consolidados, ranking (consulta o confirmación) o adjudicabilidad interna
- **THEN** el sistema responde `403 Forbidden` con `codigo = PERMISO_DENEGADO` en `ProblemDetails` y no ejecuta la operación.

#### Scenario: Autorización diferenciada para resolución de casos límite entre jefatura activa y otros miembros del comité

- **GIVEN** una convocatoria cuyo comité activo define una `jefaturaUsuarioId` específica
- **WHEN** invoca `POST /api/v1/rankings/convocatorias/{convocatoriaId}/casos-limite/resolver` un `COORDINADOR_PROGRAMA`, la jefatura activa del comité (`MIEMBRO_COMITE` con `sub == jefaturaUsuarioId`), un `MIEMBRO_COMITE` distinto de la jefatura activa o un `ADMIN_FINANCIERO`
- **THEN** el sistema autoriza al `COORDINADOR_PROGRAMA` y a la jefatura activa del comité
- **AND** rechaza con `403 Forbidden` (`codigo = PERMISO_DENEGADO` en `ProblemDetails`) al `MIEMBRO_COMITE` distinto de la jefatura activa y al `ADMIN_FINANCIERO`.

#### Scenario: Solicitud sin autenticación

- **GIVEN** una solicitud sin token JWT válido
- **WHEN** invoca cualquier endpoint de `api/v1/resultados-consolidados`, `api/v1/rankings` (incluida la resolución de casos límite) o `internal/v1/postulaciones/{postulacionId}/adjudicable`
- **THEN** el sistema responde `401 Unauthorized`.

#### Scenario: Token de servicio interno autorizado en la consulta de adjudicabilidad

- **GIVEN** una solicitud con token JWT de servicio que incluye scope `postulaciones.internal.read` o permiso `postulaciones:leer`
- **WHEN** consulta `GET /internal/v1/postulaciones/{postulacionId}/adjudicable`
- **THEN** la autorización se concede y la consulta se ejecuta
- **AND** si ese mismo token intenta acceder a `api/v1/rankings` sin rol de negocio, recibe `403 Forbidden` con `codigo = PERMISO_DENEGADO`.

### Requirement: Persistencia íntegra e idempotencia bajo concurrencia C5

El sistema `MS-POS` SHALL persistir `ResultadoConsolidadoRanking` en `postulaciones.resultados_consolidados_ranking` con `timestamptz`, `numeric(5,2)` y `numeric(12,2)`, enums en mayúsculas, unicidad 1:1 por `postulacion_id`, unicidad parcial de `(convocatoria_id, posicion_ranking)`, concurrencia optimista `C1` por `version` y `CHECK` coherentes para consolidación, confirmación y decisiones de caso límite sin interceptar las restricciones previas. Bajo concurrencia `C5` ambos envíos SHALL finalizar sin error y las operaciones de resolución y confirmación SHALL ser atómicas.

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

#### Scenario: Confirmación atómica del ranking, restricciones CHECK de confirmación y concurrencia optimista en PostgreSQL

- **GIVEN** una convocatoria con múltiples filas en `postulaciones.resultados_consolidados_ranking` en estado `PROVISIONAL`
- **WHEN** se intenta insertar una fila `PROVISIONAL` con campos de confirmación no nulos, una fila `CONFIRMADO` con campos de confirmación nulos, `clasificacion` fuera de catálogo, `posicion_ranking < 1`, `monto_estimado_beca <= 0`, o duplicar `(convocatoria_id, posicion_ranking)` confirmado
- **THEN** PostgreSQL rechaza cada intento por la restricción `CHECK` correspondiente o el índice único parcial `ux_resultados_consolidados_ranking_convocatoria_posicion`
- **AND** cuando dos coordinadores intentan confirmar el mismo ranking en simultáneo, una transacción confirma todas las filas atómicamente incrementando `version` y la otra recibe `409 Conflict` (`CONFLICTO_CONCURRENCIA`) sin dejar filas parcialmente confirmadas.

#### Scenario: Persistencia íntegra, restricciones CHECK y concurrencia optimista al resolver casos límite en PostgreSQL

- **GIVEN** una convocatoria con postulaciones en estado `PROVISIONAL` sujetas a empate o caso límite en PostgreSQL
- **WHEN** se intenta insertar un `tipo_caso_limite` fuera de `{EMPATE_POR_CUPO, EMPATE_POR_PRESUPUESTO, CASO_LIMITE_PRESUPUESTARIO}` o una fila con campos de caso límite parcialmente poblados o justificación menor de 10 caracteres, o cuando dos decisores intentan resolver el mismo empate simultáneamente
- **THEN** PostgreSQL rechaza las filas incoherentes mediante `ck_resultados_consolidados_tipo_caso_limite_valido` o `ck_resultados_consolidados_caso_limite_coherente` sin interceptar las 10 restricciones `CHECK` previas
- **AND** bajo resolución simultánea una transacción persiste atómicamente todo el grupo incrementando `version` y la segunda recibe `409 Conflict` (`CONFLICTO_CONCURRENCIA`) sin sobrescribir la decisión registrada.

### Requirement: Cálculo del ranking de postulantes y trazado automático de línea de corte por cupo y presupuesto

El sistema `MS-POS` SHALL ordenar el ranking de una convocatoria por `puntajeConsolidado` descendente (desempate técnico por `fechaConsolidacion` e `id` solo para presentación). En estado `PROVISIONAL` SHALL consultar *fail-closed* a `MS-PRG` cupos y presupuesto (descontando renovaciones pendientes) para clasificar `DENTRO_DE_CUPO` o `LISTA_DE_ESPERA`; en `CONFIRMADO` SHALL devolver la fotografía inmutable sin consultar `MS-PRG`, con paginación y filtro por `clasificacion`.

#### Scenario: Consulta de ranking provisional con trazado de línea de corte por cupo disponible

- **GIVEN** una convocatoria con cuatro resultados consolidados en estado `PROVISIONAL` con puntajes `95.00`, `88.50`, `82.00` y `75.00`, monto estimado de beca `$1,000.00` cada uno, y `MS-PRG` reporta `cuposAsignadosConvocatoria = 3`, `cuposDisponiblesPrograma = 2`, `presupuestoDisponibleBruto = 10000.00` y `presupuestoComprometidoRenovaciones = 2000.00` (`presupuestoDisponibleEfectivo = 8000.00`, `cuposDisponiblesEfectivos = 2`)
- **WHEN** un usuario autorizado consulta `GET /api/v1/rankings/convocatorias/{convocatoriaId}?pagina=1&tamano=20`
- **THEN** el sistema responde `200 OK` con `estadoRanking = PROVISIONAL`, `totalPostulantes = 4`, `totalDentroDeCupo = 2`, `totalListaDeEspera = 2`, `disponibilidad` reflejando cupos y presupuesto efectivos tras descontar renovaciones pendientes
- **AND** ordena los resultados con posiciones `1` (`95.00`) y `2` (`88.50`) clasificados como `DENTRO_DE_CUPO`, y posiciones `3` (`82.00`) y `4` (`75.00`) clasificados como `LISTA_DE_ESPERA`.

#### Scenario: Trazado de línea de corte limitado por presupuesto disponible tras descontar renovaciones pendientes

- **GIVEN** una convocatoria con tres resultados consolidados en estado `PROVISIONAL` con puntajes `94.00` (`$2,000.00`), `89.00` (`$2,000.00`) y `80.00` (`$1,000.00`), y `MS-PRG` reporta `cuposDisponiblesEfectivos = 5`, `presupuestoDisponibleBruto = 5500.00` y `presupuestoComprometidoRenovaciones = 3000.00` (`presupuestoDisponibleEfectivo = 2500.00`)
- **WHEN** un usuario autorizado consulta el ranking de la convocatoria
- **THEN** el sistema responde `200 OK` clasificando únicamente la posición `1` (`94.00`, `$2,000.00`) como `DENTRO_DE_CUPO`
- **AND** traza la línea de corte inmediatamente antes de la posición `2` (`89.00`, `$2,000.00`, que excede los `$500.00` restantes), clasificando las posiciones `2` y `3` como `LISTA_DE_ESPERA` sin saltar el orden de mérito.

#### Scenario: Consulta de ranking ya confirmado devuelve la fotografía inmutable sin depender de MS-PRG

- **GIVEN** una convocatoria cuyos resultados consolidados se encuentran en estado `CONFIRMADO` con posiciones, montos estimados y clasificaciones fijadas
- **WHEN** un usuario autorizado consulta `GET /api/v1/rankings/convocatorias/{convocatoriaId}` filtrando opcionalmente por `clasificacion = DENTRO_DE_CUPO`
- **THEN** el sistema responde `200 OK` con `estadoRanking = CONFIRMADO`, `fechaConfirmacionRanking`, `confirmadoPorUsuarioId` y los elementos confirmados ordenados por `posicionRanking` ascendente sin invocar a `MS-PRG`.

#### Scenario: Falla cerrada ante convocatoria inexistente o indisponibilidad de MS-PRG en consulta provisional

- **GIVEN** una convocatoria en estado `PROVISIONAL` cuya consulta a `MS-PRG` responde `404 Not Found` para la convocatoria o el programa, o falla por timeout, error de red, estado HTTP no exitoso o respuesta inválida
- **WHEN** un usuario autorizado consulta el ranking de la convocatoria
- **THEN** el sistema responde `404 Not Found` (`CONVOCATORIA_NO_ENCONTRADA` o `PROGRAMA_NO_ENCONTRADO`) o `503 Service Unavailable` (`DEPENDENCIA_NO_DISPONIBLE`), respectivamente.

#### Scenario: Rechazo de parámetros de consulta de ranking inválidos

- **GIVEN** una consulta de ranking con `convocatoriaId` vacío, `pagina < 1`, `tamano < 1`, `tamano > 100` o `clasificacion` fuera de `{DENTRO_DE_CUPO, LISTA_DE_ESPERA}`
- **WHEN** se invoca `GET /api/v1/rankings/convocatorias/{convocatoriaId}`
- **THEN** el sistema responde `400 Bad Request` (`ValidationProblemDetails`).

### Requirement: Detección de empates en la línea de corte y bloqueo de confirmación (RN-POS-07)

El sistema `MS-POS` SHALL detectar empates de `puntajeConsolidado` en la línea de corte (`EMPATE_POR_CUPO` o `EMPATE_POR_PRESUPUESTO`) y casos límite presupuestarios sin empate (`CASO_LIMITE_PRESUPUESTARIO`). Un empate sin resolución válida vigente SHALL bloquear la confirmación con `422 EMPATES_SIN_RESOLVER`; una vez resuelto de forma válida contra la disponibilidad actual de `MS-PRG`, SHALL reflejarse en consulta y desbloquear la confirmación, volviendo a bloquear si la disponibilidad cambia y la selección ya no cabe.

#### Scenario: Detección de empate por cupo en la frontera y bloqueo de confirmación

- **GIVEN** una convocatoria con `cuposDisponiblesEfectivos = 2`, presupuesto suficiente y tres resultados consolidados con puntajes `95.00`, `88.00` y `88.00` sin decisión registrada
- **WHEN** se consulta el ranking provisional
- **THEN** el sistema informa `tieneEmpateEnLineaDeCorte = true`, `puntajeEmpateEnLineaDeCorte = 88.00`, `tieneCasoLimiteActivo = true`, `tipoCasoLimiteActivo = EMPATE_POR_CUPO` y marca `empatadoEnLineaDeCorte = true` sobre ambos postulantes con puntaje `88.00`
- **AND** cuando el coordinador intenta confirmar el ranking, el sistema responde `422 Unprocessable Entity` con `codigo = EMPATES_SIN_RESOLVER` y mantiene todos los resultados en `PROVISIONAL`.

#### Scenario: Detección de empate por presupuesto en la frontera y bloqueo de confirmación

- **GIVEN** una convocatoria con `cuposDisponiblesEfectivos = 5`, `presupuestoDisponibleEfectivo = 3000.00` y dos postulantes empatados en `90.00` con monto estimado de `$2,000.00` cada uno (total `$4,000.00 > $3,000.00`, individual `$2,000.00 <= $3,000.00`) sin decisión registrada
- **WHEN** se consulta el ranking provisional o se intenta confirmar el ranking
- **THEN** la consulta marca `tieneEmpateEnLineaDeCorte = true` y `tipoCasoLimiteActivo = EMPATE_POR_PRESUPUESTO` sobre ambos postulantes
- **AND** la confirmación es rechazada con `422 Unprocessable Entity` y `codigo = EMPATES_SIN_RESOLVER`.

#### Scenario: Desbloqueo de la confirmación tras resolución válida de empate y re-bloqueo ante reducción de disponibilidad

- **GIVEN** una convocatoria cuyo empate en la línea de corte fue resuelto válidamente seleccionando una postulación dentro del cupo y presupuesto efectivos
- **WHEN** se consulta el ranking provisional o se solicita confirmar mientras la disponibilidad de `MS-PRG` sigue cubriendo la selección registrada
- **THEN** la consulta refleja `tieneEmpateEnLineaDeCorte = false`, `tieneCasoLimiteResuelto = true` y la selección aplicada, y la confirmación deja de bloquear por `EMPATES_SIN_RESOLVER`
- **AND** si la disponibilidad de `MS-PRG` se reduce antes de confirmar de modo que la selección registrada ya no cabe en cupo o presupuesto, la confirmación vuelve a ser rechazada con `422 Unprocessable Entity` (`EMPATES_SIN_RESOLVER` o `DECISION_CASO_LIMITE_EXCEDE_DISPONIBILIDAD`).

#### Scenario: Detección informativa de caso límite presupuestario sin empate sin bloquear corte estricto por mérito

- **GIVEN** una convocatoria con `cuposDisponiblesEfectivos = 5`, `presupuestoDisponibleEfectivo = 2500.00` y tres postulantes con puntajes `94.00` (`$2,000.00`), `89.00` (`$2,000.00`) y `80.00` (`$400.00`) sin decisión registrada
- **WHEN** se consulta el ranking provisional
- **THEN** el sistema mantiene el corte estricto por mérito (`94.00` en `DENTRO_DE_CUPO`; `89.00` y `80.00` en `LISTA_DE_ESPERA`, `tieneEmpateEnLineaDeCorte = false`)
- **AND** informa `tieneCasoLimiteActivo = true`, `tipoCasoLimiteActivo = CASO_LIMITE_PRESUPUESTARIO`, `presupuestoDisponibleEnCasoLimite = 500.00` y `postulacionesEnCasoLimiteActivo` conteniendo la postulación de `80.00`.

#### Scenario: Empate completamente dentro de cupo o completamente fuera por presupuesto no bloquea confirmación

- **GIVEN** una convocatoria donde dos postulantes comparten `puntajeConsolidado = 92.00` y ambos caben en los cupos y presupuesto restantes (o donde ambos superan individualmente el presupuesto restante sin candidatos posteriores elegibles y quedan ambos en `LISTA_DE_ESPERA`)
- **WHEN** se calcula el ranking de la convocatoria
- **THEN** el sistema determina `tieneEmpateEnLineaDeCorte = false` y permite continuar con la confirmación final si se cumplen las demás precondiciones.

### Requirement: Advertencia por discrepancias sin revisar dentro o adyacentes a la línea de corte

El sistema `MS-POS` SHALL detectar discrepancias sin revisar (`alertaDiscrepancia = true` y `discrepanciaRevisada = false`) en postulantes `DENTRO_DE_CUPO`, empatados en frontera o en el primer grupo de `LISTA_DE_ESPERA` adyacente al corte, exponiendo la advertencia en consulta. Al confirmar con advertencia activa SHALL exigir `confirmarConAdvertenciaDiscrepancias = true`, rechazando con `422 DISCREPANCIAS_SIN_REVISAR_EN_CORTE` si no se acusó recibo y permitiendo confirmar al acusarla.

#### Scenario: Advertencia visible cuando existe discrepancia sin revisar dentro de cupo o adyacente al corte

- **GIVEN** una convocatoria con `cuposDisponiblesEfectivos = 2` y cuatro postulantes con puntajes `95.00` (sin alerta), `90.00` (alerta sin revisar, `DENTRO_DE_CUPO`), `85.00` (alerta sin revisar, primer puesto de `LISTA_DE_ESPERA`) y `70.00` (alerta sin revisar, lejos de la frontera)
- **WHEN** se consulta el ranking provisional
- **THEN** el sistema responde `200 OK` con `tieneAdvertenciaDiscrepanciasEnCorte = true`, `cantidadDiscrepanciasSinRevisarEnCorte = 2` e identifica las postulaciones de `90.00` y `85.00` (excluyendo la de `70.00` por no ser adyacente al corte).

#### Scenario: Rechazo sin acuse explícito y confirmación permitida tras acusar la advertencia de discrepancias

- **GIVEN** una convocatoria lista para confirmar, sin empates en frontera, pero con una postulación `DENTRO_DE_CUPO` (o adyacente en `LISTA_DE_ESPERA`) que tiene `alertaDiscrepancia = true` y `discrepanciaRevisada = false`
- **WHEN** el coordinador intenta confirmar el ranking con `confirmarConAdvertenciaDiscrepancias = false` (o sin indicarlo)
- **THEN** el sistema responde `422 Unprocessable Entity` con `codigo = DISCREPANCIAS_SIN_REVISAR_EN_CORTE` y no modifica ningún resultado
- **AND** cuando el coordinador reintenta enviando `confirmarConAdvertenciaDiscrepancias = true`, el sistema permite confirmar el ranking transicionando todos los resultados a `CONFIRMADO`.

#### Scenario: Discrepancia sin revisar lejos de la frontera no genera advertencia de corte

- **GIVEN** una convocatoria con `cuposDisponiblesEfectivos = 1`, postulantes `95.00` (sin alerta), `85.00` (sin alerta, adyacente en `LISTA_DE_ESPERA`) y `70.00` (alerta sin revisar, por debajo del grupo adyacente)
- **WHEN** se consulta o confirma el ranking con `confirmarConAdvertenciaDiscrepancias = false`
- **THEN** `tieneAdvertenciaDiscrepanciasEnCorte` es `false` y la confirmación se completa sin requerir acuse de advertencia.

### Requirement: Confirmación atómica del ranking de convocatoria (PROVISIONAL a CONFIRMADO)

El sistema `MS-POS` SHALL permitir al `COORDINADOR_PROGRAMA` confirmar atómicamente el ranking (`PROVISIONAL → CONFIRMADO`), fijando `posicionRanking`, `montoEstimadoBeca`, `clasificacion`, `fechaConfirmacionRanking` UTC y `confirmadoPorUsuarioId`, preservando las decisiones de caso límite registradas. SHALL exigir resultados consolidados, cero evaluaciones pendientes, periodo cerrado, disponibilidad en `MS-PRG`, cero empates sin resolver, vigencia de decisiones y acuse de advertencias si aplica.

#### Scenario: Confirmación atómica exitosa habilitando postulantes dentro de cupo

- **GIVEN** una convocatoria con periodo de revisión cerrado en `MS-PRG`, todas sus asignaciones vigentes completadas y consolidadas en `PROVISIONAL`, disponibilidad válida en `MS-PRG`, sin empates en la línea de corte y sin advertencias de discrepancias sin acusar
- **WHEN** el `COORDINADOR_PROGRAMA` invoca `POST /api/v1/rankings/convocatorias/{convocatoriaId}/confirmar`
- **THEN** el sistema responde `200 OK` con `estadoRanking = CONFIRMADO`, `fechaConfirmacionRanking` UTC y `confirmadoPorUsuarioId` del token
- **AND** persiste atómicamente todos los `ResultadoConsolidadoRanking` de la convocatoria en estado `CONFIRMADO` con `posicionRanking`, `montoEstimadoBeca` y `clasificacion` fijados, incrementando su `version` y bloqueando cualquier recálculo, revisión o resolución posterior (`409 RANKING_YA_CONFIRMADO`).

#### Scenario: Confirmación atómica exitosa tras resolver empate o caso límite habilitando postulantes dentro de cupo

- **GIVEN** una convocatoria en estado `PROVISIONAL` con periodo de revisión cerrado, disponibilidad válida en `MS-PRG` y un empate en frontera o caso límite presupuestario previamente resuelto de forma válida y vigente con justificación obligatoria
- **WHEN** el `COORDINADOR_PROGRAMA` invoca `POST /api/v1/rankings/convocatorias/{convocatoriaId}/confirmar`
- **THEN** el sistema responde `200 OK` con `estadoRanking = CONFIRMADO` y preserva los metadatos auditables de la decisión registrada
- **AND** persiste atómicamente las postulaciones seleccionadas como `DENTRO_DE_CUPO` y las no seleccionadas del grupo afectado como `LISTA_DE_ESPERA`.

#### Scenario: Rechazo de confirmación por evaluaciones pendientes, periodo abierto, sin resultados o ranking ya confirmado

- **GIVEN** una convocatoria sin resultados consolidados, o ya confirmada, o con al menos una asignación vigente sin completar o sin consolidar, o con periodo de revisión aún abierto/no iniciado/cancelado en `MS-PRG`
- **WHEN** el coordinador intenta confirmar el ranking
- **THEN** el sistema responde respectivamente `422 RANKING_SIN_RESULTADOS`, `409 RANKING_YA_CONFIRMADO`, `409 EVALUACIONES_PENDIENTES` o `409` (`PERIODO_REVISION_ABIERTO` / `PERIODO_REVISION_NO_INICIADO` / `CONVOCATORIA_CANCELADA`), sin alterar ningún registro.

#### Scenario: Rechazo de confirmación cuando una decisión de caso límite presupuestario quedó inválida por cambio de presupuesto

- **GIVEN** una convocatoria en estado `PROVISIONAL` donde previamente se resolvió un `CASO_LIMITE_PRESUPUESTARIO` seleccionando una postulación de `$400.00`, pero antes de confirmar `MS-PRG` redujo el presupuesto restante a `$200.00`
- **WHEN** el coordinador intenta confirmar el ranking
- **THEN** el sistema responde `422 Unprocessable Entity` con `codigo = DECISION_CASO_LIMITE_EXCEDE_DISPONIBILIDAD` y mantiene todos los resultados en `PROVISIONAL`.

### Requirement: Consulta interna de adjudicabilidad por postulación para otorgamiento (MS-BEC)

El sistema `MS-POS` SHALL exponer `GET /internal/v1/postulaciones/{postulacionId}/adjudicable` (DDD V7 §6.2), devolviendo `adjudicable = true` solo cuando el `ResultadoConsolidadoRanking` está en `CONFIRMADO` y `DENTRO_DE_CUPO`. Si está en `PROVISIONAL` o `LISTA_DE_ESPERA`, SHALL responder `200 OK` con `adjudicable = false` y `motivoNoAdjudicable`; si no existe resultado consolidado, SHALL responder `404 RESULTADO_CONSOLIDADO_NO_ENCONTRADO`.

#### Scenario: Postulación confirmada dentro de cupo reportada como adjudicable

- **GIVEN** una postulación cuyo `ResultadoConsolidadoRanking` está en estado `CONFIRMADO` con `clasificacion = DENTRO_DE_CUPO`, `posicionRanking = 1` y `montoEstimadoBeca = 1500.00`
- **WHEN** se consulta `GET /internal/v1/postulaciones/{postulacionId}/adjudicable` con credencial autorizada
- **THEN** el sistema responde `200 OK` con `adjudicable = true`, `estadoConfirmacionRanking = CONFIRMADO`, `clasificacion = DENTRO_DE_CUPO`, `posicionRanking = 1`, `montoEstimadoBeca = 1500.00` y `motivoNoAdjudicable = null`.

#### Scenario: Postulación en ranking provisional o en lista de espera reportada como no adjudicable

- **GIVEN** una postulación cuyo `ResultadoConsolidadoRanking` está en estado `PROVISIONAL`, o en estado `CONFIRMADO` con `clasificacion = LISTA_DE_ESPERA`
- **WHEN** se consulta `GET /internal/v1/postulaciones/{postulacionId}/adjudicable` con credencial autorizada
- **THEN** el sistema responde `200 OK` con `adjudicable = false` y `motivoNoAdjudicable` igual a `RANKING_NO_CONFIRMADO` o `FUERA_DE_CUPO_EN_LISTA_DE_ESPERA`, respectivamente.

#### Scenario: Consulta interna de adjudicabilidad sobre postulación sin resultado consolidado

- **GIVEN** una `postulacionId` sin `ResultadoConsolidadoRanking`
- **WHEN** se consulta `GET /internal/v1/postulaciones/{postulacionId}/adjudicable` con credencial autorizada
- **THEN** el sistema responde `404 Not Found` con `codigo = RESULTADO_CONSOLIDADO_NO_ENCONTRADO`.

### Requirement: Resolución manual y auditada de empates en la línea de corte y casos límite presupuestarios (HU 10.1)

El sistema `MS-POS` SHALL permitir al `COORDINADOR_PROGRAMA` o a la jefatura activa del comité resolver manualmente un empate en frontera (`EMPATE_POR_CUPO` o `EMPATE_POR_PRESUPUESTO`) o un `CASO_LIMITE_PRESUPUESTARIO` en estado `PROVISIONAL`, con evaluaciones consolidadas y periodo de revisión cerrado, exigiendo selección válida dentro del grupo que quepa en cupo y presupuesto efectivos y justificación de 10 a 1000 caracteres.

#### Scenario: Resolución exitosa de empate por cupo en la línea de corte por el coordinador

- **GIVEN** una convocatoria en estado `PROVISIONAL` con periodo de revisión cerrado, todas las evaluaciones completadas y consolidadas, `cuposDisponiblesEfectivos = 2`, presupuesto suficiente y tres postulaciones con puntajes `95.00`, `88.00` (A) y `88.00` (B)
- **WHEN** el `COORDINADOR_PROGRAMA` invoca `POST /api/v1/rankings/convocatorias/{convocatoriaId}/casos-limite/resolver` seleccionando únicamente a la postulación B con una justificación válida de al menos 10 caracteres
- **THEN** el sistema responde `200 OK` con el ranking provisional actualizado mostrando `tieneEmpateEnLineaDeCorte = false`, `tieneCasoLimiteResuelto = true`, B en posición `2` clasificada como `DENTRO_DE_CUPO` y A en posición `3` clasificada como `LISTA_DE_ESPERA`
- **AND** persiste atómicamente sobre ambas postulaciones del grupo afectado `tipoCasoLimite = EMPATE_POR_CUPO`, `seleccionadoEnCasoLimite` (`true` para B, `false` para A), `justificacionDesempateCasoLimite` normalizada, `decididoPorUsuarioId` del token y `fechaDecisionCasoLimite` UTC del servidor.

#### Scenario: Resolución exitosa de empate o caso límite por la jefatura activa del comité evaluador

- **GIVEN** una convocatoria en estado `PROVISIONAL` con un empate en la línea de corte activo y un `ComiteEvaluador` activo cuya `jefaturaUsuarioId` corresponde al usuario autenticado con rol `MIEMBRO_COMITE`
- **WHEN** dicha jefatura invoca `POST /api/v1/rankings/convocatorias/{convocatoriaId}/casos-limite/resolver` con selección y justificación válidas
- **THEN** el sistema autoriza la operación, responde `200 OK` con el caso límite resuelto y registra `decididoPorUsuarioId = jefaturaUsuarioId`.

#### Scenario: Resolución exitosa de caso límite presupuestario sin empate

- **GIVEN** una convocatoria en estado `PROVISIONAL` con periodo de revisión cerrado, `cuposDisponiblesEfectivos = 5`, `presupuestoDisponibleEfectivo = 2500.00` y tres postulaciones con puntajes `94.00` (`$2,000.00`), `89.00` (`$2,000.00`, excede los `$500.00` restantes) y `80.00` (`$400.00`, cabe en los `$500.00` restantes)
- **WHEN** el coordinador o la jefatura del comité resuelve el caso límite seleccionando la postulación de `80.00` con justificación obligatoria
- **THEN** el sistema responde `200 OK`, registra `tipoCasoLimite = CASO_LIMITE_PRESUPUESTARIO` y `seleccionadoEnCasoLimite = true` sobre la postulación de `80.00`, manteniéndola clasificada como `DENTRO_DE_CUPO` mientras la postulación de `89.00` conserva su posición de mérito en `LISTA_DE_ESPERA`.

#### Scenario: Rechazo cuando no existe caso límite activo que resolver

- **GIVEN** una convocatoria en estado `PROVISIONAL` sin empate en la línea de corte ni candidatos elegibles en un caso límite presupuestario frente a la disponibilidad real de `MS-PRG`
- **WHEN** un usuario autorizado intenta invocar `POST /api/v1/rankings/convocatorias/{convocatoriaId}/casos-limite/resolver`
- **THEN** el sistema responde `422 Unprocessable Entity` con `codigo = CASO_LIMITE_NO_DETECTADO` y no persiste cambios.

#### Scenario: Rechazo por selección fuera del grupo afectado o que excede cupo o presupuesto efectivo

- **GIVEN** una convocatoria con un empate activo entre las postulaciones A y B para `1` cupo restante y `$1,000.00` de presupuesto restante
- **WHEN** el decisor intenta seleccionar una postulación ajena al grupo en conflicto o seleccionar simultáneamente a A y B excediendo el cupo o presupuesto disponible
- **THEN** el sistema responde `422 Unprocessable Entity` con `codigo = SELECCION_CASO_LIMITE_INVALIDA` o `codigo = SELECCION_CASO_LIMITE_EXCEDE_DISPONIBILIDAD`, respectivamente, y no persiste cambios.

#### Scenario: Rechazo por justificación ausente, fuera de longitud o precondiciones incumplidas

- **GIVEN** una solicitud de resolución con justificación vacía, solo espacios, menor de 10 caracteres o mayor de 1000 caracteres, o con ranking ya confirmado, evaluaciones pendientes/sin consolidar, periodo de revisión abierto/no iniciado/cancelado, convocatoria inexistente o `MS-PRG` indisponible
- **WHEN** se invoca el endpoint de resolución de caso límite
- **THEN** el sistema responde respectivamente `400 Bad Request` (`ValidationProblemDetails` en API) o `422 JUSTIFICACION_CASO_LIMITE_REQUERIDA`, `409 RANKING_YA_CONFIRMADO`, `409 EVALUACIONES_PENDIENTES`, `409` (`PERIODO_REVISION_ABIERTO` / `PERIODO_REVISION_NO_INICIADO` / `CONVOCATORIA_CANCELADA`), `404` (`CONVOCATORIA_NO_ENCONTRADA` / `PROGRAMA_NO_ENCONTRADO`) o `503 DEPENDENCIA_NO_DISPONIBLE`.

#### Scenario: Rechazo cuando el decisor tiene conflicto de interés declarado sobre el grupo afectado

- **GIVEN** una convocatoria con un caso límite activo donde el verificador de conflictos de interés reporta un conflicto del usuario decisor con al menos una postulación del grupo afectado (`RN-POS-06`)
- **WHEN** dicho usuario intenta resolver el caso límite
- **THEN** el sistema responde `403 Forbidden` con `codigo = CONFLICTO_INTERES_DECISOR` y no persiste cambios.

### Requirement: Inalterabilidad de decisiones de desempate y casos límite registrados (RN-AUD-01)

El sistema `MS-POS` SHALL garantizar que toda decisión de desempate o caso límite registrada sobre un `ResultadoConsolidadoRanking` sea inalterable una vez persistida, rechazando con `409 Conflict` (`codigo = CASO_LIMITE_YA_RESUELTO`) cualquier intento posterior de sobrescribir la decisión o alterar el puntaje consolidado de esa postulación.

#### Scenario: Rechazo de edición o sobrescritura posterior de una decisión ya registrada

- **GIVEN** un empate o caso límite cuyas postulaciones afectadas ya cuentan con una decisión registrada (`tipoCasoLimite`, `seleccionadoEnCasoLimite`, `justificacionDesempateCasoLimite`, `decididoPorUsuarioId` y `fechaDecisionCasoLimite`)
- **WHEN** el coordinador o la jefatura del comité intenta registrar una nueva decisión sobre cualquiera de esas postulaciones
- **THEN** el sistema responde `409 Conflict` con `codigo = CASO_LIMITE_YA_RESUELTO` y conserva intactos la selección, justificación, decisor y marca de tiempo originales.

#### Scenario: Protección contra recálculo mutante sobre una postulación con decisión de caso límite registrada

- **GIVEN** un `ResultadoConsolidadoRanking` en estado `PROVISIONAL` que ya cuenta con una decisión de caso límite registrada
- **WHEN** se intenta recalcular su resultado con un puntaje o conjunto de evaluaciones distinto al que originó la decisión
- **THEN** el sistema rechaza la modificación con `409 Conflict` (`codigo = CASO_LIMITE_YA_RESUELTO`) preservando íntegro el registro auditado.
