## MODIFIED Requirements

### Requirement: Confirmación atómica del ranking de convocatoria (PROVISIONAL a CONFIRMADO)

El sistema `MS-POS` SHALL permitir al `COORDINADOR_PROGRAMA` confirmar atómicamente el ranking (`PROVISIONAL → CONFIRMADO`), fijando posición, monto, clasificación y metadatos, preservando decisiones de caso límite y generando en la misma transacción `EntradaListaEspera` en `EN_ESPERA` (`1..M` por `posicionRanking ASC`) para `LISTA_DE_ESPERA`. SHALL exigir resultados consolidados, cero pendientes, periodo cerrado, disponibilidad en `MS-PRG`, cero empates sin resolver y acuse de advertencias.

#### Scenario: Confirmación atómica exitosa habilitando postulantes dentro de cupo

- **GIVEN** una convocatoria con periodo de revisión cerrado en `MS-PRG`, todas sus asignaciones vigentes completadas y consolidadas en `PROVISIONAL`, disponibilidad válida en `MS-PRG`, sin empates en la línea de corte y sin advertencias de discrepancias sin acusar
- **WHEN** el `COORDINADOR_PROGRAMA` invoca `POST /api/v1/rankings/convocatorias/{convocatoriaId}/confirmar`
- **THEN** el sistema responde `200 OK` con `estadoRanking = CONFIRMADO`, `fechaConfirmacionRanking` UTC y `confirmadoPorUsuarioId` del token
- **AND** persiste atómicamente todos los `ResultadoConsolidadoRanking` de la convocatoria en estado `CONFIRMADO` con `posicionRanking`, `montoEstimadoBeca` y `clasificacion` fijados, incrementando su `version` y bloqueando cualquier recálculo, revisión o resolución posterior (`409 RANKING_YA_CONFIRMADO`)
- **AND** genera en la misma transacción las entradas `EntradaListaEspera` en estado `EN_ESPERA` ordenadas secuencialmente (`1..M`) por `posicionRanking` ascendente únicamente para las postulaciones clasificadas como `LISTA_DE_ESPERA`.

#### Scenario: Confirmación atómica exitosa tras resolver empate o caso límite habilitando postulantes dentro de cupo

- **GIVEN** una convocatoria en estado `PROVISIONAL` con periodo de revisión cerrado, disponibilidad válida en `MS-PRG` y un empate en frontera o caso límite presupuestario previamente resuelto de forma válida y vigente con justificación obligatoria
- **WHEN** el `COORDINADOR_PROGRAMA` invoca `POST /api/v1/rankings/convocatorias/{convocatoriaId}/confirmar`
- **THEN** el sistema responde `200 OK` con `estadoRanking = CONFIRMADO` y preserva los metadatos auditables de la decisión registrada
- **AND** persiste atómicamente las postulaciones seleccionadas como `DENTRO_DE_CUPO`, las no seleccionadas del grupo afectado como `LISTA_DE_ESPERA` y genera sus registros correspondientes en `EntradaListaEspera` respetando el `posicionRanking` confirmado.

#### Scenario: Rechazo de confirmación por evaluaciones pendientes, periodo abierto, sin resultados o ranking ya confirmado

- **GIVEN** una convocatoria sin resultados consolidados, o ya confirmada, o con al menos una asignación vigente sin completar o sin consolidar, o con periodo de revisión aún abierto/no iniciado/cancelado en `MS-PRG`
- **WHEN** el coordinador intenta confirmar el ranking
- **THEN** el sistema responde respectivamente `422 RANKING_SIN_RESULTADOS`, `409 RANKING_YA_CONFIRMADO`, `409 EVALUACIONES_PENDIENTES` o `409` (`PERIODO_REVISION_ABIERTO` / `PERIODO_REVISION_NO_INICIADO` / `CONVOCATORIA_CANCELADA`), sin alterar ningún registro ni crear entradas de lista de espera.

#### Scenario: Rechazo de confirmación cuando una decisión de caso límite presupuestario quedó inválida por cambio de presupuesto

- **GIVEN** una convocatoria en estado `PROVISIONAL` donde previamente se resolvió un `CASO_LIMITE_PRESUPUESTARIO` seleccionando una postulación de `$400.00`, pero antes de confirmar `MS-PRG` redujo el presupuesto restante a `$200.00`
- **WHEN** el coordinador intenta confirmar el ranking
- **THEN** el sistema responde `422 Unprocessable Entity` con `codigo = DECISION_CASO_LIMITE_EXCEDE_DISPONIBILIDAD` y mantiene todos los resultados en `PROVISIONAL` sin crear entradas de lista de espera.

### Requirement: Consulta interna de adjudicabilidad por postulación para otorgamiento (MS-BEC)

El sistema `MS-POS` SHALL exponer `GET /internal/v1/postulaciones/{postulacionId}/adjudicable` (DDD V7 §6.2), devolviendo `adjudicable = true` cuando el `ResultadoConsolidadoRanking` está en `CONFIRMADO` y `DENTRO_DE_CUPO`, o en `CONFIRMADO` y `LISTA_DE_ESPERA` con `EntradaListaEspera` en `PROMOVIDA`. En `PROVISIONAL` o `LISTA_DE_ESPERA` sin promover SHALL responder `adjudicable = false` con `motivoNoAdjudicable`; sin resultado consolidado, `404 RESULTADO_CONSOLIDADO_NO_ENCONTRADO`.

#### Scenario: Postulación confirmada dentro de cupo reportada como adjudicable

- **GIVEN** una postulación cuyo `ResultadoConsolidadoRanking` está en estado `CONFIRMADO` con `clasificacion = DENTRO_DE_CUPO`, `posicionRanking = 1` y `montoEstimadoBeca = 1500.00`
- **WHEN** se consulta `GET /internal/v1/postulaciones/{postulacionId}/adjudicable` con credencial autorizada
- **THEN** el sistema responde `200 OK` con `adjudicable = true`, `promovidaDeListaEspera = false`, `estadoConfirmacionRanking = CONFIRMADO`, `clasificacion = DENTRO_DE_CUPO`, `posicionRanking = 1`, `montoEstimadoBeca = 1500.00` y `motivoNoAdjudicable = null`.

#### Scenario: Postulación promovida manualmente desde lista de espera reportada como adjudicable

- **GIVEN** una postulación cuyo `ResultadoConsolidadoRanking` está en estado `CONFIRMADO` con `clasificacion = LISTA_DE_ESPERA`, `posicionRanking = 3` y `montoEstimadoBeca = 1200.00`, y cuya `EntradaListaEspera` fue promovida manualmente a estado `PROMOVIDA` por un `COORDINADOR_PROGRAMA`
- **WHEN** se consulta `GET /internal/v1/postulaciones/{postulacionId}/adjudicable` con credencial autorizada
- **THEN** el sistema responde `200 OK` con `adjudicable = true`, `motivoNoAdjudicable = null`, `promovidaDeListaEspera = true`, `estadoListaEspera = PROMOVIDA`, `posicionListaEspera = 1`, `fechaPromocionListaEspera` UTC, `programaBecaId`, `estadoConfirmacionRanking = CONFIRMADO`, `clasificacion = LISTA_DE_ESPERA`, `posicionRanking = 3` y `montoEstimadoBeca = 1200.00`.

#### Scenario: Postulación en ranking provisional o en lista de espera reportada como no adjudicable

- **GIVEN** una postulación cuyo `ResultadoConsolidadoRanking` está en estado `PROVISIONAL`, o en estado `CONFIRMADO` con `clasificacion = LISTA_DE_ESPERA` cuya `EntradaListaEspera` está en `EN_ESPERA` o `ELEGIBLE_POR_VACANTE` (o no promovida)
- **WHEN** se consulta `GET /internal/v1/postulaciones/{postulacionId}/adjudicable` con credencial autorizada
- **THEN** el sistema responde `200 OK` con `adjudicable = false`, `promovidaDeListaEspera = false` y `motivoNoAdjudicable` igual a `RANKING_NO_CONFIRMADO` o `FUERA_DE_CUPO_EN_LISTA_DE_ESPERA`, respectivamente.

#### Scenario: Consulta interna de adjudicabilidad sobre postulación sin resultado consolidado

- **GIVEN** una `postulacionId` sin `ResultadoConsolidadoRanking`
- **WHEN** se consulta `GET /internal/v1/postulaciones/{postulacionId}/adjudicable` con credencial autorizada
- **THEN** el sistema responde `404 Not Found` con `codigo = RESULTADO_CONSOLIDADO_NO_ENCONTRADO`.
