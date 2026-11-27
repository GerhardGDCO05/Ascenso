## ADDED Requirements

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

## MODIFIED Requirements

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
