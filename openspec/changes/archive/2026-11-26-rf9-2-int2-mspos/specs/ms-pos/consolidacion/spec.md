# Spec Delta — `ms-pos/consolidacion`

## ADDED Requirements

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

El sistema `MS-POS` SHALL detectar empates reales de `puntajeConsolidado` exactamente en la línea de corte —cuando dos o más postulantes comparten puntaje, no caben todos en cupo y presupuesto restantes, pero al menos uno sí cabría—, marcándolos como empatados en frontera y bloqueando la confirmación final del ranking con `422 Unprocessable Entity` (`codigo = EMPATES_SIN_RESOLVER`).

#### Scenario: Detección de empate por cupo en la frontera y bloqueo de confirmación

- **GIVEN** una convocatoria con `cuposDisponiblesEfectivos = 2`, presupuesto suficiente y tres resultados consolidados con puntajes `95.00`, `88.00` y `88.00`
- **WHEN** se consulta el ranking provisional
- **THEN** el sistema informa `tieneEmpateEnLineaDeCorte = true`, `puntajeEmpateEnLineaDeCorte = 88.00` y marca `empatadoEnLineaDeCorte = true` sobre ambos postulantes con puntaje `88.00`
- **AND** cuando el coordinador intenta confirmar el ranking, el sistema responde `422 Unprocessable Entity` con `codigo = EMPATES_SIN_RESOLVER` y mantiene todos los resultados en `PROVISIONAL`.

#### Scenario: Detección de empate por presupuesto en la frontera y bloqueo de confirmación

- **GIVEN** una convocatoria con `cuposDisponiblesEfectivos = 5`, `presupuestoDisponibleEfectivo = 3000.00` y dos postulantes empatados en `90.00` con monto estimado de `$2,000.00` cada uno (total `$4,000.00 > $3,000.00`, individual `$2,000.00 <= $3,000.00`)
- **WHEN** se consulta el ranking provisional o se intenta confirmar el ranking
- **THEN** la consulta marca `tieneEmpateEnLineaDeCorte = true` sobre ambos postulantes
- **AND** la confirmación es rechazada con `422 Unprocessable Entity` y `codigo = EMPATES_SIN_RESOLVER`.

#### Scenario: Empate completamente dentro de cupo o completamente fuera por presupuesto no bloquea confirmación

- **GIVEN** una convocatoria donde dos postulantes comparten `puntajeConsolidado = 92.00` y ambos caben en los cupos y presupuesto restantes (o donde ambos superan individualmente el presupuesto restante y quedan ambos en `LISTA_DE_ESPERA`)
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

El sistema `MS-POS` SHALL permitir al `COORDINADOR_PROGRAMA` confirmar atómicamente el ranking (`PROVISIONAL → CONFIRMADO`), fijando `posicionRanking`, `montoEstimadoBeca`, `clasificacion`, `fechaConfirmacionRanking` UTC y `confirmadoPorUsuarioId`. SHALL exigir resultados consolidados, cero evaluaciones pendientes, periodo de revisión cerrado, disponibilidad en `MS-PRG`, cero empates en frontera, no confirmación previa y acuse de advertencias si aplica.

#### Scenario: Confirmación atómica exitosa habilitando postulantes dentro de cupo

- **GIVEN** una convocatoria con periodo de revisión cerrado en `MS-PRG`, todas sus asignaciones vigentes completadas y consolidadas en `PROVISIONAL`, disponibilidad válida en `MS-PRG`, sin empates en la línea de corte y sin advertencias de discrepancias sin acusar
- **WHEN** el `COORDINADOR_PROGRAMA` invoca `POST /api/v1/rankings/convocatorias/{convocatoriaId}/confirmar`
- **THEN** el sistema responde `200 OK` con `estadoRanking = CONFIRMADO`, `fechaConfirmacionRanking` UTC y `confirmadoPorUsuarioId` del token
- **AND** persiste atómicamente todos los `ResultadoConsolidadoRanking` de la convocatoria en estado `CONFIRMADO` con `posicionRanking`, `montoEstimadoBeca` y `clasificacion` fijados, incrementando su `version` y bloqueando cualquier recálculo o revisión posterior (`409 RANKING_YA_CONFIRMADO`).

#### Scenario: Rechazo de confirmación por evaluaciones pendientes, periodo abierto, sin resultados o ranking ya confirmado

- **GIVEN** una convocatoria sin resultados consolidados, o ya confirmada, o con al menos una asignación vigente sin completar o sin consolidar, o con periodo de revisión aún abierto/no iniciado/cancelado en `MS-PRG`
- **WHEN** el coordinador intenta confirmar el ranking
- **THEN** el sistema responde respectivamente `422 RANKING_SIN_RESULTADOS`, `409 RANKING_YA_CONFIRMADO`, `409 EVALUACIONES_PENDIENTES` o `409` (`PERIODO_REVISION_ABIERTO` / `PERIODO_REVISION_NO_INICIADO` / `CONVOCATORIA_CANCELADA`), sin alterar ningún registro.

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

## MODIFIED Requirements

### Requirement: Autorización por rol sobre la consolidación y alertas de discrepancia

El sistema `MS-POS` SHALL exigir `COORDINADOR_PROGRAMA` o `DIRECCION` para consultar consolidados, alertas y ranking, y solo `COORDINADOR_PROGRAMA` para revisar discrepancias, consolidar manualmente o confirmar el ranking. Para `/internal/v1/postulaciones/{postulacionId}/adjudicable` SHALL aceptar `COORDINADOR_PROGRAMA`, `DIRECCION` o token de servicio (`postulaciones.internal.read` / `postulaciones:leer`). Otros roles (incluidos `MIEMBRO_COMITE` y `ADMIN_FINANCIERO`) SHALL recibir `403 PERMISO_DENEGADO`; sin token `401`.

#### Scenario: Dirección consulta en solo lectura y es denegada en escritura

- **GIVEN** un usuario autenticado con rol `DIRECCION`
- **WHEN** consulta `GET /api/v1/resultados-consolidados/postulaciones/{postulacionId}`, `GET /api/v1/resultados-consolidados/convocatorias/{convocatoriaId}` o `GET /api/v1/rankings/convocatorias/{convocatoriaId}`
- **THEN** la autorización se concede y la consulta se ejecuta
- **AND** cuando intenta invocar `POST /api/v1/resultados-consolidados/postulaciones/{postulacionId}`, `POST /api/v1/resultados-consolidados/postulaciones/{postulacionId}/discrepancia/revisar` o `POST /api/v1/rankings/convocatorias/{convocatoriaId}/confirmar`, el sistema responde `403 Forbidden` con `codigo = PERMISO_DENEGADO`.

#### Scenario: Miembro de comité y administrador financiero denegados en todos los endpoints

- **GIVEN** un usuario autenticado con rol `MIEMBRO_COMITE` o `ADMIN_FINANCIERO`
- **WHEN** invoca cualquier endpoint de lectura o escritura de resultados consolidados, ranking o adjudicabilidad interna
- **THEN** el sistema responde `403 Forbidden` con `codigo = PERMISO_DENEGADO` en `ProblemDetails` y no ejecuta la operación.

#### Scenario: Solicitud sin autenticación

- **GIVEN** una solicitud sin token JWT válido
- **WHEN** invoca cualquier endpoint de `api/v1/resultados-consolidados`, `api/v1/rankings` o `internal/v1/postulaciones/{postulacionId}/adjudicable`
- **THEN** el sistema responde `401 Unauthorized`.

#### Scenario: Token de servicio interno autorizado en la consulta de adjudicabilidad

- **GIVEN** una solicitud con token JWT de servicio que incluye scope `postulaciones.internal.read` o permiso `postulaciones:leer`
- **WHEN** consulta `GET /internal/v1/postulaciones/{postulacionId}/adjudicable`
- **THEN** la autorización se concede y la consulta se ejecuta
- **AND** si ese mismo token intenta acceder a `api/v1/rankings` sin rol de negocio, recibe `403 Forbidden` con `codigo = PERMISO_DENEGADO`.

### Requirement: Persistencia íntegra e idempotencia bajo concurrencia C5

El sistema `MS-POS` SHALL persistir `ResultadoConsolidadoRanking` en `postulaciones.resultados_consolidados_ranking` con `timestamptz`, `numeric(5,2)` y `numeric(12,2)`, enums en mayúsculas, unicidad 1:1 por `postulacion_id`, unicidad parcial de `(convocatoria_id, posicion_ranking)` cuando no es nula, concurrencia optimista por `version` y `CHECK` de coherencia en `PROVISIONAL` y `CONFIRMADO`. Bajo concurrencia `C5` ambos envíos SHALL finalizar sin error y la confirmación SHALL ser atómica.

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
