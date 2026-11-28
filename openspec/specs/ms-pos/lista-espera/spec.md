# ms-pos/lista-espera Specification

## Purpose

Gestiona la lista de espera ordenada de postulantes elegibles fuera de cupo en `MS-POS` mediante el agregado raíz `EntradaListaEspera`, cubriendo su generación automática al confirmar el ranking, consultas FIFO paginadas, registro de vacante liberada sin promoción automática (`RN-POS-07`), promoción manual asistida con verificación en `MS-PRG`, concurrencia `C6` y autorización por rol.

## Requirements

### Requirement: Generación automática y ordenada de la lista de espera al confirmar el ranking

Al confirmarse el ranking de una convocatoria, `MS-POS` SHALL crear atómicamente una `EntradaListaEspera` en `EN_ESPERA` por cada `ResultadoConsolidadoRanking` confirmado en `LISTA_DE_ESPERA`, asignando `posicionOrden` secuencial (`1..M`) según `posicionRanking` ascendente (respetando HU 10.1), `programaBecaId`, `montoEstimadoBecaSnapshot` y `fechaIngreso` UTC. Si no hay postulaciones en `LISTA_DE_ESPERA`, no SHALL crear filas.

#### Scenario: Generación secuencial automática al confirmar un ranking con postulantes fuera de cupo

- **GIVEN** una convocatoria lista para confirmar con cuatro postulaciones consolidadas donde las posiciones `1` y `2` quedan en `DENTRO_DE_CUPO` y las posiciones `3` (monto `1500.00`) y `4` (monto `1200.00`) quedan en `LISTA_DE_ESPERA`
- **WHEN** el `COORDINADOR_PROGRAMA` confirma el ranking de la convocatoria
- **THEN** el sistema persiste atómicamente los cuatro resultados en `CONFIRMADO` y crea exactamente dos registros en `EntradaListaEspera` en estado `EN_ESPERA`
- **AND** asigna `posicionOrden = 1` a la postulación de `posicionRanking = 3` (`montoEstimadoBecaSnapshot = 1500.00`) y `posicionOrden = 2` a la postulación de `posicionRanking = 4` (`montoEstimadoBecaSnapshot = 1200.00`), ambos con `fechaIngreso` UTC igual a la fecha de confirmación y `fechaElegibilidad`, `fechaPromocion` y `promovidoPorUsuarioId` nulos.

#### Scenario: Preservación del orden definido por desempate o caso límite presupuestario de HU 10.1

- **GIVEN** una convocatoria donde un caso límite presupuestario o empate de HU 10.1 dejó en `LISTA_DE_ESPERA` a una postulación de mayor mérito (`posicionRanking = 2`) seguida por otra de menor mérito (`posicionRanking = 4`), habiendo sido promovida en el corte la de `posicionRanking = 3`
- **WHEN** el coordinador confirma el ranking de la convocatoria
- **THEN** el sistema genera la lista de espera respetando `posicionRanking` ascendente: `posicionOrden = 1` para `posicionRanking = 2` y `posicionOrden = 2` para `posicionRanking = 4`.

#### Scenario: Convocatoria sin postulantes en lista de espera no crea filas

- **GIVEN** una convocatoria lista para confirmar donde el 100% de las postulaciones quedan clasificadas como `DENTRO_DE_CUPO`
- **WHEN** el coordinador confirma el ranking de la convocatoria
- **THEN** la confirmación se completa exitosamente en estado `CONFIRMADO`
- **AND** no se crea ninguna `EntradaListaEspera` para la convocatoria.

### Requirement: Consultas de lista de espera y siguiente candidato en orden FIFO

El sistema `MS-POS` SHALL permitir consultar paginadamente (`pagina >= 1`, `1 <= tamano <= 100`) las entradas de lista de espera de una convocatoria ordenadas por `posicionOrden` ascendente, con filtro opcional por `estado` y contadores resumen, así como consultar al siguiente candidato pendiente en estricto orden FIFO (menor `posicionOrden` con `estado != PROMOVIDA`).

#### Scenario: Listado paginado de lista de espera con contadores resumen y filtro opcional por estado

- **GIVEN** una convocatoria cuya lista de espera contiene tres entradas: posición `1` en `PROMOVIDA`, posición `2` en `ELEGIBLE_POR_VACANTE` y posición `3` en `EN_ESPERA`
- **WHEN** un usuario autorizado consulta la lista de espera de la convocatoria sin filtro o filtrando por `estado = ELEGIBLE_POR_VACANTE`
- **THEN** el sistema responde `200 OK` con los contadores globales (`totalEnListaEspera = 3`, `totalEnEspera = 1`, `totalElegiblesPorVacante = 1`, `totalPromovidas = 1`)
- **AND** devuelve la página ordenada estrictamente por `posicionOrden` ascendente respetando el filtro, `pagina`, `tamano` y `total`.

#### Scenario: Consulta del siguiente candidato pendiente en estricto orden FIFO

- **GIVEN** una convocatoria cuya entrada en posición `1` ya está en `PROMOVIDA`, la posición `2` está en `ELEGIBLE_POR_VACANTE` y la posición `3` está en `EN_ESPERA`
- **WHEN** un usuario autorizado consulta el siguiente candidato en cola de la convocatoria
- **THEN** el sistema responde `200 OK` devolviendo exclusivamente la entrada de `posicionOrden = 2`.

#### Scenario: Consulta del siguiente candidato cuando la cola está vacía o completamente promovida

- **GIVEN** una convocatoria sin entradas en lista de espera o con todas sus entradas en estado `PROMOVIDA`
- **WHEN** un usuario autorizado consulta el siguiente candidato en cola
- **THEN** el sistema responde `404 Not Found` con `codigo = LISTA_ESPERA_SIN_CANDIDATOS`.

#### Scenario: Rechazo de parámetros de consulta inválidos

- **GIVEN** una consulta de lista de espera con `convocatoriaId` vacío, `pagina < 1`, `tamano < 1`, `tamano > 100` o `estado` fuera de `{EN_ESPERA, ELEGIBLE_POR_VACANTE, PROMOVIDA}`
- **WHEN** se invoca el endpoint de consulta
- **THEN** el sistema responde `400 Bad Request` (`ValidationProblemDetails`).

### Requirement: Registro de vacante liberada y marcación de elegibilidad sin promoción automática (RN-POS-07)

Ante una vacante liberada por rechazo, expiración o anulación de oferta (`RF-15`/`RF-16`), `MS-POS` SHALL seleccionar y bloquear atómicamente (`C6`) al candidato con menor `posicionOrden` en `EN_ESPERA` y marcarlo `ELEGIBLE_POR_VACANTE` con `fechaElegibilidad` UTC. Esta operación NEVER SHALL promover automáticamente al candidato ni emitir una oferta desatendida.

#### Scenario: Marcación del primer candidato en espera como elegible por vacante

- **GIVEN** una convocatoria con dos entradas en `EN_ESPERA` en posiciones `1` y `2`
- **WHEN** se registra una vacante liberada para la convocatoria (por coordinación o por invocación interna desde `MS-BEC`)
- **THEN** el sistema responde `200 OK` con la entrada de `posicionOrden = 1` en estado `ELEGIBLE_POR_VACANTE`, `fechaElegibilidad` UTC, `fechaPromocion = null`, `promovidoPorUsuarioId = null` y `version` incrementada
- **AND** la entrada de `posicionOrden = 2` permanece en `EN_ESPERA`
- **AND** la postulación de `posicionOrden = 1` continúa respondiendo `adjudicable = false` en `/internal/v1/postulaciones/{postulacionId}/adjudicable` hasta que el coordinador confirme la promoción manual.

#### Scenario: Registro de una segunda vacante marca al siguiente candidato todavía en espera

- **GIVEN** una convocatoria donde la posición `1` ya se encuentra en `ELEGIBLE_POR_VACANTE` y la posición `2` está en `EN_ESPERA`
- **WHEN** se registra una nueva vacante liberada para la convocatoria
- **THEN** el sistema transiciona únicamente la posición `2` de `EN_ESPERA` a `ELEGIBLE_POR_VACANTE` sin alterar la posición `1`.

#### Scenario: Rechazo cuando no existen candidatos en estado EN_ESPERA para marcar

- **GIVEN** una convocatoria sin lista de espera o donde todas las entradas ya están en `ELEGIBLE_POR_VACANTE` o `PROMOVIDA`
- **WHEN** se intenta registrar una vacante liberada
- **THEN** el sistema responde `404 Not Found` con `codigo = LISTA_ESPERA_SIN_CANDIDATOS` y no modifica ningún registro.

### Requirement: Promoción manual asistida por el coordinador con verificación fail-closed en MS-PRG y orden FIFO estricto

Únicamente el `COORDINADOR_PROGRAMA` SHALL promover manualmente a un candidato (`ELEGIBLE_POR_VACANTE → PROMOVIDA` o `EN_ESPERA → PROMOVIDA`) respetando estricto orden FIFO por `posicionOrden` y verificando en tiempo real (*fail-closed*) en `MS-PRG` cupos efectivos `>= 1` y presupuesto efectivo `>= montoEstimadoBeca`, registrando `promovidoPorUsuarioId` del token, `fechaPromocion` UTC y `motivoPromocion` opcional (máx. 500 chars).

#### Scenario: Promoción manual exitosa del siguiente candidato en cola con cupo y presupuesto reales en MS-PRG

- **GIVEN** una convocatoria donde el candidato de `posicionOrden = 1` está en `ELEGIBLE_POR_VACANTE` con `montoEstimadoBeca = 1500.00`, y `MS-PRG` responde en tiempo real `cuposDisponiblesEfectivos = 1` y `presupuestoDisponibleEfectivo = 2000.00`
- **WHEN** el `COORDINADOR_PROGRAMA` confirma la promoción manual (por `entradaListaEsperaId` o mediante promoción del siguiente candidato) con un `motivoPromocion` válido opcional
- **THEN** el sistema responde `200 OK` con la entrada en estado `PROMOVIDA`, `promovidoPorUsuarioId` del token, `fechaPromocion` UTC del servidor, `version` incrementada y `habilitadaParaAdjudicacion = true`
- **AND** una consulta posterior a `GET /internal/v1/postulaciones/{postulacionId}/adjudicable` responde `200 OK` con `adjudicable = true`.

#### Scenario: Rechazo por intento de saltar a un candidato anterior todavía pendiente en cola (orden FIFO)

- **GIVEN** una convocatoria donde la entrada de `posicionOrden = 1` sigue en `EN_ESPERA` o `ELEGIBLE_POR_VACANTE` y la entrada de `posicionOrden = 2` está pendiente
- **WHEN** el `COORDINADOR_PROGRAMA` intenta promover directamente la entrada de `posicionOrden = 2`
- **THEN** el sistema responde `409 Conflict` con `codigo = ORDEN_LISTA_ESPERA_VIOLADO` y no modifica ninguna entrada.

#### Scenario: Rechazo cuando MS-PRG reporta sin cupo disponible o presupuesto insuficiente

- **GIVEN** una convocatoria cuyo siguiente candidato en cola tiene `montoEstimadoBeca = 2000.00`
- **WHEN** el `COORDINADOR_PROGRAMA` solicita su promoción y `MS-PRG` reporta `cuposDisponiblesEfectivos = 0`, o bien `cuposDisponiblesEfectivos >= 1` con `presupuestoDisponibleEfectivo = 1500.00` (`< 2000.00`)
- **THEN** el sistema responde respectivamente `409 Conflict` (`codigo = SIN_CUPO_DISPONIBLE_PROMOCION`) o `422 Unprocessable Entity` (`codigo = PRESUPUESTO_INSUFICIENTE_PROMOCION`)
- **AND** mantiene la entrada sin cambios en su estado anterior.

#### Scenario: Falla cerrada ante indisponibilidad o ausencia de convocatoria/programa en MS-PRG

- **GIVEN** una solicitud de promoción manual válida en orden FIFO
- **WHEN** la consulta a `MS-PRG` falla por timeout, error de red, respuesta inválida o retorna `404` para la convocatoria o el programa
- **THEN** el sistema responde `503 Service Unavailable` (`codigo = DEPENDENCIA_NO_DISPONIBLE`) o `404 Not Found` (`CONVOCATORIA_NO_ENCONTRADA` / `PROGRAMA_NO_ENCONTRADO`), respectivamente, sin promover al candidato.

#### Scenario: Rechazo por entrada inexistente, ya promovida o motivo de promoción demasiado largo

- **GIVEN** un `entradaListaEsperaId` inexistente en la convocatoria, o una entrada ya en estado `PROMOVIDA`, o un `motivoPromocion` superior a 500 caracteres
- **WHEN** el `COORDINADOR_PROGRAMA` invoca la promoción manual
- **THEN** el sistema responde respectivamente `404 Not Found` (`ENTRADA_LISTA_ESPERA_NO_ENCONTRADA`), `409 Conflict` (`ENTRADA_LISTA_ESPERA_YA_PROMOVIDA`) o `400 Bad Request` (`ValidationProblemDetails` en API) / `422 MOTIVO_PROMOCION_INVALIDO` en dominio.

### Requirement: Integridad de persistencia y control de concurrencia atómico C6

El sistema `MS-POS` SHALL persistir `EntradaListaEspera` con unicidad por `postulacionId` y por `(convocatoriaId, posicionOrden)`, coherencia de estados y fechas, `posicionOrden >= 1`, `montoEstimadoBecaSnapshot > 0` y concurrencia optimista (`version`). Ante operaciones paralelas sobre la misma convocatoria (`C6`), SHALL serializar atómicamente la selección y mutación de la cola para evitar promociones duplicadas o corrupción FIFO.

#### Scenario: Dos coordinadores intentan promover simultáneamente al mismo candidato (concurrencia C6)

- **GIVEN** una convocatoria cuya entrada de `posicionOrden = 1` está lista para promoción y `MS-PRG` reporta cupo y presupuesto disponibles
- **WHEN** dos coordinadores invocan simultáneamente la promoción manual de la misma entrada contra la base de datos
- **THEN** exactamente una operación confirma la transición a `PROMOVIDA` incrementando `version`
- **AND** la segunda operación es rechazada con `409 Conflict` (`ENTRADA_LISTA_ESPERA_YA_PROMOVIDA` o `CONFLICTO_CONCURRENCIA`), sin promover al candidato dos veces ni alterar `promovidoPorUsuarioId` ni `fechaPromocion` originales.

#### Scenario: Registro paralelo de dos vacantes liberadas sobre la misma convocatoria (concurrencia C6)

- **GIVEN** una convocatoria con al menos dos entradas en `EN_ESPERA` (posiciones `1` y `2`)
- **WHEN** se ejecutan simultáneamente dos operaciones de registro de vacante liberada sobre la misma convocatoria
- **THEN** ambas operaciones se serializan atómicamente sin colisionar: una marca `posicionOrden = 1` como `ELEGIBLE_POR_VACANTE` y la otra marca `posicionOrden = 2` como `ELEGIBLE_POR_VACANTE`, preservando intacto el orden FIFO.

#### Scenario: Rechazo de filas incoherentes o duplicadas por restricciones de integridad

- **GIVEN** un intento de insertar una segunda `EntradaListaEspera` para la misma `postulacionId`, duplicar `(convocatoriaId, posicionOrden)`, usar `posicionOrden < 1`, `montoEstimadoBecaSnapshot <= 0`, un `estado` fuera de catálogo o una combinación incoherente entre `estado` y `fechaElegibilidad`/`fechaPromocion`/`promovidoPorUsuarioId`
- **WHEN** se intenta confirmar la escritura en persistencia
- **THEN** la persistencia rechaza la operación por índice único (traducido a `ENTRADA_LISTA_ESPERA_DUPLICADA` o `RESTRICCION_UNICIDAD`) o por la restricción `CHECK` correspondiente.

### Requirement: Autorización por rol y token interno sobre la lista de espera

El sistema `MS-POS` SHALL exigir `COORDINADOR_PROGRAMA` o `DIRECCION` para lectura, solo `COORDINADOR_PROGRAMA` para mutaciones públicas, y `COORDINADOR_PROGRAMA` o token de servicio interno (`postulaciones.internal.write` / `postulaciones:escribir`) en el registro interno de vacante. `MIEMBRO_COMITE`, `ADMIN_FINANCIERO` y actores no autorizados SHALL recibir `403 PERMISO_DENEGADO`; sin token `401`.

#### Scenario: Dirección consulta lista de espera en solo lectura y es denegada en mutaciones

- **GIVEN** un usuario autenticado con rol `DIRECCION`
- **WHEN** consulta la lista de espera o el siguiente candidato de una convocatoria
- **THEN** el sistema autoriza la consulta
- **AND** cuando intenta registrar una vacante liberada o promover a un candidato (por API pública o interna), el sistema responde `403 Forbidden` con `codigo = PERMISO_DENEGADO` en `ProblemDetails`.

#### Scenario: Miembro de comité y administrador financiero denegados en todos los endpoints de lista de espera

- **GIVEN** un usuario autenticado con rol `MIEMBRO_COMITE` o `ADMIN_FINANCIERO`
- **WHEN** invoca cualquier endpoint de lectura o escritura de lista de espera (público o interno)
- **THEN** el sistema responde `403 Forbidden` con `codigo = PERMISO_DENEGADO` en `ProblemDetails`.

#### Scenario: Token de servicio interno autorizado exclusivamente en el registro interno de vacante liberada

- **GIVEN** un token JWT de servicio con scope `postulaciones.internal.write` (o permiso `postulaciones:escribir`)
- **WHEN** invoca el endpoint interno de registro de vacante liberada
- **THEN** el sistema autoriza la operación
- **AND** si ese mismo token intenta invocar la promoción manual o las consultas públicas sin rol de negocio, o si un token solo con `postulaciones.internal.read` invoca el registro interno de vacante, recibe `403 Forbidden` con `codigo = PERMISO_DENEGADO`.

#### Scenario: Solicitud sin autenticación en endpoints de lista de espera

- **GIVEN** una solicitud sin token JWT válido
- **WHEN** invoca cualquier endpoint público o interno de lista de espera
- **THEN** el sistema responde `401 Unauthorized`.
