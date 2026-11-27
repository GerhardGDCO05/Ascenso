# ms-pos/evaluaciones Specification

## Purpose

Gestiona el registro de puntuaciones por criterio y comentarios de los evaluadores sobre sus postulaciones asignadas en `MS-POS`, cubriendo el guardado de borradores, el envío definitivo con cálculo de puntaje ponderado e inmutabilidad, el bloqueo de la rúbrica al iniciar la calificación y el aislamiento estricto de evaluación ciega.

## Requirements

### Requirement: Guardado en borrador de puntuaciones y comentarios

El sistema `MS-POS` SHALL permitir al `MIEMBRO_COMITE` autenticado guardar una `Evaluacion` en estado `BORRADOR` para una `AsignacionEvaluacion` propia con plazo vigente, registrando una o más `CalificacionCriterio` y comentarios opcionales. Al crear el primer borrador, la asignación SHALL pasar de `PENDIENTE` a `EN_PROGRESO` y el sistema SHALL bloquear la `RubricaEvaluacion` publicada con motivo `PRIMERA_CALIFICACION`.

#### Scenario: Creación del primer borrador y bloqueo de la rúbrica

- **GIVEN** un `MIEMBRO_COMITE` con una `AsignacionEvaluacion` propia en `PENDIENTE`, con `fechaLimiteEvaluacion` vigente y cuya `RubricaEvaluacion` está en `PUBLICADA`
- **WHEN** guarda un borrador con al menos una calificación válida y comentarios opcionales
- **THEN** el sistema responde `201 Created` con la evaluación en `BORRADOR`, `fechaInicio` UTC, `fechaEnvio = null` y `puntajeTotalPonderado = null`
- **AND** la `AsignacionEvaluacion` pasa a `EN_PROGRESO` y la `RubricaEvaluacion` pasa a `BLOQUEADA` con motivo `PRIMERA_CALIFICACION`.

#### Scenario: Actualización iterativa de un borrador existente

- **GIVEN** un `MIEMBRO_COMITE` con una evaluación propia en `BORRADOR` y su asignación en `EN_PROGRESO` dentro del plazo
- **WHEN** guarda nuevamente el borrador modificando puntuaciones, agregando nuevos criterios o actualizando comentarios
- **THEN** el sistema responde `200 OK` con el borrador actualizado, versión incrementada y asignación aún en `EN_PROGRESO`, manteniendo una única evaluación por asignación.

#### Scenario: Rechazo cuando la rúbrica no está publicada

- **GIVEN** una asignación cuya `RubricaEvaluacion` asociada permanece en estado `BORRADOR`
- **WHEN** el evaluador intenta guardar el primer borrador
- **THEN** el sistema responde `409 Conflict` con `codigo = RUBRICA_NO_PUBLICADA` y no persiste cambios.

#### Scenario: Rechazo cuando el plazo de evaluación ya venció

- **GIVEN** una asignación propia cuya `fechaLimiteEvaluacion` es anterior al instante actual del servidor
- **WHEN** el evaluador intenta guardar o actualizar un borrador
- **THEN** el sistema responde `409 Conflict` con `codigo = PLAZO_EVALUACION_VENCIDO` y no modifica la evaluación.

#### Scenario: Rechazo cuando la asignación no existe o fue reasignada por conflicto

- **GIVEN** un identificador de asignación inexistente o una asignación en estado `REASIGNADA_POR_CONFLICTO`
- **WHEN** el evaluador intenta guardar un borrador
- **THEN** responde `404 Not Found` con `codigo = ASIGNACION_NO_ENCONTRADA` o `409 Conflict` con `codigo = ASIGNACION_NO_EVALUABLE`, respectivamente.

#### Scenario: Rechazo por solicitud sin calificaciones o con datos inválidos

- **GIVEN** una solicitud de borrador sin calificaciones, con criterios repetidos, con más de dos decimales en el puntaje o con comentarios que exceden el máximo permitido
- **WHEN** el sistema valida la solicitud
- **THEN** responde `400 Bad Request` o `422 Unprocessable Entity` con `codigo = EVALUACION_INVALIDA` y no bloquea la rúbrica ni persiste cambios.

### Requirement: Validación de pertenencia y rango de puntaje por criterio

El sistema `MS-POS` SHALL aceptar únicamente calificaciones cuyos `criterioEvaluacionId` pertenezcan a la `RubricaEvaluacion` asociada al programa y convocatoria de la asignación, y cuyo `puntajeAsignado` (máximo 2 decimales) se encuentre dentro del intervalo cerrado `[puntajeMinimo, puntajeMaximo]` definido para cada criterio.

#### Scenario: Puntaje en los límites exactos del criterio

- **GIVEN** un criterio de la rúbrica con rango `[0.00, 20.00]`
- **WHEN** el evaluador asigna `0.00` o `20.00`
- **THEN** el sistema acepta la calificación como válida.

#### Scenario: Puntaje fuera del rango del criterio

- **GIVEN** un criterio de la rúbrica con rango `[1.00, 10.00]`
- **WHEN** el evaluador envía `0.99` o `10.01`
- **THEN** el sistema responde `422 Unprocessable Entity` con `codigo = PUNTAJE_FUERA_DE_RANGO` y no persiste cambios ni bloquea la rúbrica.

#### Scenario: Criterio ajeno a la rúbrica de la convocatoria

- **GIVEN** un `criterioEvaluacionId` inexistente o perteneciente a otra rúbrica
- **WHEN** el evaluador intenta guardar el borrador
- **THEN** el sistema responde `422 Unprocessable Entity` con `codigo = CRITERIO_NO_PERTENECE_A_RUBRICA` y no persiste cambios.

### Requirement: Envío definitivo, completitud y cálculo de puntaje ponderado

El sistema `MS-POS` SHALL permitir al `MIEMBRO_COMITE` enviar definitivamente una `Evaluacion` en `BORRADOR` únicamente cuando el plazo esté vigente y el 100% de los criterios de la rúbrica tengan una calificación válida. Al enviar, el servidor SHALL calcular de forma determinista el `puntajeTotalPonderado` en `[0.00, 100.00]`, registrar `fechaEnvio` UTC, pasar la evaluación a `ENVIADA_DEFINITIVA` y la asignación a `COMPLETADA`.

#### Scenario: Envío definitivo exitoso con cálculo ponderado exacto

- **GIVEN** un borrador propio con todos los criterios de la rúbrica puntuados dentro de su rango y `fechaLimiteEvaluacion` vigente
- **WHEN** el evaluador confirma el envío definitivo
- **THEN** el sistema responde `200 OK` con estado `ENVIADA_DEFINITIVA`, `fechaEnvio` UTC y `puntajeTotalPonderado` calculado con normalización por criterio y ponderación por `pesoPorcentual` redondeado a 2 decimales
- **AND** la `AsignacionEvaluacion` pasa de `EN_PROGRESO` a `COMPLETADA`.

#### Scenario: Rechazo de envío con criterios faltantes

- **GIVEN** un borrador que solo puntúa una parte de los criterios de la rúbrica
- **WHEN** el evaluador intenta enviarlo definitivamente
- **THEN** el sistema responde `422 Unprocessable Entity` con `codigo = EVALUACION_INCOMPLETA` indicando la cantidad de criterios pendientes
- **AND** la evaluación permanece en `BORRADOR` y la asignación en `EN_PROGRESO`.

#### Scenario: Rechazo de envío sin borrador previo o con plazo vencido

- **GIVEN** una asignación propia en `PENDIENTE` sin borrador guardado, o un borrador cuyo plazo ya venció
- **WHEN** el evaluador intenta enviar definitivamente
- **THEN** responde `404 Not Found` con `codigo = EVALUACION_NO_ENCONTRADA` o `409 Conflict` con `codigo = PLAZO_EVALUACION_VENCIDO`, respectivamente.

### Requirement: Inmutabilidad tras el envío definitivo

El sistema `MS-POS` SHALL impedir cualquier modificación o reenvío de una `Evaluacion` una vez alcanzada la transición a `ENVIADA_DEFINITIVA` o `INVALIDADA_POR_CONFLICTO`.

#### Scenario: Intento de editar una evaluación ya enviada

- **GIVEN** una evaluación en estado `ENVIADA_DEFINITIVA`
- **WHEN** el evaluador intenta guardar un borrador o cambiar puntuaciones o comentarios sobre la misma asignación
- **THEN** el sistema responde `409 Conflict` con `codigo = EVALUACION_YA_ENVIADA` y no altera las puntuaciones, el puntaje ponderado ni la versión.

#### Scenario: Intento de reenviar una evaluación ya enviada

- **GIVEN** una evaluación en estado `ENVIADA_DEFINITIVA`
- **WHEN** el evaluador solicita nuevamente el envío definitivo
- **THEN** el sistema responde `409 Conflict` con `codigo = EVALUACION_YA_ENVIADA`.

#### Scenario: Intento de modificar una evaluación invalidada por conflicto

- **GIVEN** una evaluación en estado `INVALIDADA_POR_CONFLICTO`
- **WHEN** se intenta modificarla o enviarla
- **THEN** el sistema responde `409 Conflict` con `codigo = EVALUACION_INVALIDADA`.

### Requirement: Evaluación ciega y consultas aisladas por rol

El sistema `MS-POS` SHALL garantizar que un `MIEMBRO_COMITE` solo pueda registrar, enviar y consultar sus propias evaluaciones (identidad obtenida del token), rechazando con `403 Forbidden` y `codigo = EVALUACION_CONFIDENCIAL` cualquier acceso sobre la asignación o evaluación de otro evaluador, habilitando únicamente la consulta en solo lectura de evaluaciones definitivas de coevaluadores tras el cierre formal del periodo de revisión. El `COORDINADOR_PROGRAMA` SHALL poder consultar únicamente las evaluaciones no borrador de una postulación en solo lectura, sin ver borradores privados.

#### Scenario: El evaluador consulta su propia evaluación por asignación

- **GIVEN** un `MIEMBRO_COMITE` con una evaluación propia en `BORRADOR` o `ENVIADA_DEFINITIVA`
- **WHEN** consulta su evaluación por `asignacionEvaluacionId`
- **THEN** responde `200 OK` con sus propias calificaciones por criterio, comentarios, estado, fechas y `puntajeTotalPonderado` (nulo en `BORRADOR`), sin exponer datos de coevaluadores.

#### Scenario: Bloqueo de acceso a la evaluación de otro evaluador

- **GIVEN** una asignación perteneciente al evaluador B
- **WHEN** el evaluador A (`MIEMBRO_COMITE`) intenta consultar, guardar borrador o enviar sobre la asignación del evaluador B
- **THEN** el sistema responde `403 Forbidden` con `codigo = EVALUACION_CONFIDENCIAL` y no revela si existe evaluación ni sus puntuaciones.

#### Scenario: Listado paginado de evaluaciones propias del evaluador

- **GIVEN** un `MIEMBRO_COMITE` con múltiples evaluaciones propias en distintos estados
- **WHEN** consulta su listado paginado con filtro opcional por `estado`, `pagina` desde 1 y `tamano` hasta 100
- **THEN** responde `200 OK` únicamente con sus propias evaluaciones y el total para paginar; parámetros fuera de rango o estado inválido responden `400 Bad Request`.

#### Scenario: Supervisión del coordinador sobre evaluaciones definitivas por postulación

- **GIVEN** una postulación con una evaluación en `ENVIADA_DEFINITIVA` y otra en `BORRADOR`
- **WHEN** un `COORDINADOR_PROGRAMA` consulta las evaluaciones de esa postulación
- **THEN** responde `200 OK` incluyendo únicamente la evaluación en `ENVIADA_DEFINITIVA` y excluyendo el borrador privado en curso.

### Requirement: Autorización por rol sobre las evaluaciones

El sistema `MS-POS` SHALL exigir el rol literal `MIEMBRO_COMITE` para guardar borradores, enviar evaluaciones definitivas, consultar evaluaciones propias y consultar evaluaciones de coevaluadores post-cierre, y el rol literal `COORDINADOR_PROGRAMA` para la consulta de supervisión por postulación. Todo usuario autenticado con otro rol, incluido `ADMIN_FINANCIERO` en todos los endpoints, SHALL recibir `403 Forbidden` con `codigo = PERMISO_DENEGADO` en `ProblemDetails`; sin token SHALL responder `401 Unauthorized`.

#### Scenario: Administrador financiero denegado en todos los endpoints

- **GIVEN** un usuario autenticado con rol `ADMIN_FINANCIERO`
- **WHEN** invoca cualquier operación de escritura o lectura de evaluaciones, incluida la consulta de coevaluadores
- **THEN** el sistema responde `403 Forbidden` con `codigo = PERMISO_DENEGADO` y no ejecuta la operación.

#### Scenario: Coordinador intenta puntuar o acceder a rutas exclusivas de evaluador

- **GIVEN** un usuario autenticado con rol `COORDINADOR_PROGRAMA`
- **WHEN** intenta guardar borrador, enviar evaluación, consultar evaluaciones propias de evaluador o consultar la ruta de coevaluadores
- **THEN** el sistema responde `403 Forbidden` con `codigo = PERMISO_DENEGADO`.

#### Scenario: Miembro de comité intenta acceder a la consulta de supervisión por postulación

- **GIVEN** un usuario autenticado con rol `MIEMBRO_COMITE`
- **WHEN** intenta consultar las evaluaciones consolidadas de supervisión de una postulación
- **THEN** el sistema responde `403 Forbidden` con `codigo = PERMISO_DENEGADO`.

#### Scenario: Solicitud sin autenticación

- **GIVEN** una solicitud sin token válido
- **WHEN** invoca cualquier endpoint de evaluaciones, incluida la consulta de coevaluadores
- **THEN** el sistema responde `401 Unauthorized`.

### Requirement: Validación de la ventana de revisión de la convocatoria

El sistema `MS-POS` SHALL validar contra la `Convocatoria` asociada en `MS-PRG` que el periodo de revisión esté abierto (`estado` igual a `PUBLICADA` o `CERRADA` y el instante UTC del servidor dentro de `[fechaInicioEvaluacion, fechaCierreEvaluacion]`) antes de guardar un borrador o enviar una `Evaluacion` definitiva, en adición al plazo individual `fechaLimiteEvaluacion`. Ante indisponibilidad de `MS-PRG`, el sistema SHALL fallar cerrado.

#### Scenario: Rechazo de borrador o envío cuando el periodo de revisión ya cerró

- **GIVEN** una `AsignacionEvaluacion` propia con plazo individual aún futuro pero cuya `Convocatoria` en `MS-PRG` está en `PUBLICADA` o `CERRADA` con `fechaCierreEvaluacion` anterior al instante UTC actual del servidor
- **WHEN** el `MIEMBRO_COMITE` intenta guardar un borrador o enviar definitivamente su `Evaluacion`
- **THEN** el sistema responde `409 Conflict` con `codigo = PERIODO_REVISION_CERRADO`
- **AND** no persiste cambios ni bloquea la rúbrica.

#### Scenario: Rechazo de borrador o envío antes del inicio del periodo de revisión

- **GIVEN** una `AsignacionEvaluacion` propia cuya `Convocatoria` está en estado `BORRADOR` o tiene `fechaInicioEvaluacion` posterior al instante UTC actual del servidor
- **WHEN** el `MIEMBRO_COMITE` intenta guardar un borrador o enviar definitivamente su `Evaluacion`
- **THEN** el sistema responde `409 Conflict` con `codigo = PERIODO_REVISION_NO_INICIADO`
- **AND** no persiste cambios ni bloquea la rúbrica.

#### Scenario: Rechazo de borrador o envío cuando la convocatoria fue cancelada

- **GIVEN** una `AsignacionEvaluacion` propia cuya `Convocatoria` en `MS-PRG` se encuentra en estado `CANCELADA`
- **WHEN** el `MIEMBRO_COMITE` intenta guardar un borrador o enviar definitivamente su `Evaluacion`
- **THEN** el sistema responde `409 Conflict` con `codigo = CONVOCATORIA_CANCELADA`
- **AND** no persiste cambios.

#### Scenario: Rechazo cuando la convocatoria no existe o MS-PRG no está disponible

- **GIVEN** una `AsignacionEvaluacion` propia cuya `Convocatoria` responde `404` en `MS-PRG`, o una falla de comunicación, timeout o respuesta inválida desde `MS-PRG`
- **WHEN** el `MIEMBRO_COMITE` intenta guardar un borrador o enviar definitivamente su `Evaluacion`
- **THEN** el sistema responde `404 Not Found` con `codigo = CONVOCATORIA_NO_ENCONTRADA` o `503 Service Unavailable` con `codigo = DEPENDENCIA_NO_DISPONIBLE`, respectivamente
- **AND** no persiste cambios.

#### Scenario: Escritura permitida en los límites exactos de la ventana de revisión

- **GIVEN** una `AsignacionEvaluacion` propia con plazo vigente y una `Convocatoria` en estado `PUBLICADA` o `CERRADA` con el instante UTC actual igual a `fechaInicioEvaluacion` o `fechaCierreEvaluacion`
- **WHEN** el `MIEMBRO_COMITE` guarda un borrador válido o envía su `Evaluacion` completa
- **THEN** el sistema acepta la validación de la ventana y ejecuta la operación de evaluación normalmente.

### Requirement: Visibilidad cruzada de evaluaciones definitivas tras el cierre del periodo de revisión

El sistema `MS-POS` SHALL permitir a un `MIEMBRO_COMITE` con `AsignacionEvaluacion` vigente (distinta de `REASIGNADA_POR_CONFLICTO`) consultar en solo lectura las evaluaciones `ENVIADA_DEFINITIVA` de sus coevaluadores únicamente tras cerrar el periodo de revisión (`PUBLICADA` o `CERRADA` y `ahora > fechaCierreEvaluacion`), excluyendo borradores, invalidadas e identidad del postulante o del par.

#### Scenario: Consulta exitosa de evaluaciones definitivas de coevaluadores tras el cierre

- **GIVEN** un `MIEMBRO_COMITE` A con asignación vigente sobre una postulación cuya `Convocatoria` está en `CERRADA` o `PUBLICADA` con `fechaCierreEvaluacion` anterior al instante actual, y donde existen una `Evaluacion` `ENVIADA_DEFINITIVA` del coevaluador B, una `BORRADOR` del coevaluador C, una `INVALIDADA_POR_CONFLICTO` del coevaluador D y la propia `Evaluacion` de A
- **WHEN** el evaluador A consulta las evaluaciones de coevaluadores de esa postulación
- **THEN** el sistema responde `200 OK` con únicamente la `Evaluacion` en `ENVIADA_DEFINITIVA` del coevaluador B (sus `CalificacionCriterio`, `puntajeTotalPonderado`, `comentariosGenerales` y `fechaEnvio`) y `fechaCierreRevision`
- **AND** excluye la evaluación propia de A, el borrador de C, la invalidada de D y cualquier identidad del postulante o identificador personal/de asignación del coevaluador B.

#### Scenario: Consulta tras el cierre sin evaluaciones definitivas de otros coevaluadores

- **GIVEN** un `MIEMBRO_COMITE` con asignación vigente (por ejemplo bajo excepción de evaluador único o cuando ningún coevaluador envió definitiva) y periodo de revisión formalmente cerrado
- **WHEN** consulta las evaluaciones de coevaluadores de esa postulación
- **THEN** el sistema responde `200 OK` con una lista vacía de evaluaciones y la `fechaCierreRevision` correspondiente.

#### Scenario: Bloqueo de consulta antes del cierre del periodo de revisión

- **GIVEN** un `MIEMBRO_COMITE` con asignación vigente sobre una postulación cuya `Convocatoria` está en `BORRADOR` o tiene `fechaCierreEvaluacion` mayor o igual al instante UTC actual del servidor
- **WHEN** intenta consultar las evaluaciones de coevaluadores de esa postulación
- **THEN** el sistema responde `409 Conflict` con `codigo = PERIODO_REVISION_ABIERTO` y no expone puntuaciones ni comentarios de otros evaluadores.

#### Scenario: Bloqueo de consulta cuando la convocatoria fue cancelada

- **GIVEN** un `MIEMBRO_COMITE` con asignación vigente sobre una postulación cuya `Convocatoria` en `MS-PRG` se encuentra en estado `CANCELADA`
- **WHEN** intenta consultar las evaluaciones de coevaluadores de esa postulación
- **THEN** el sistema responde `409 Conflict` con `codigo = CONVOCATORIA_CANCELADA` y no expone evaluaciones de pares.

#### Scenario: Acceso denegado a miembro de comité no asignado o reasignado por conflicto

- **GIVEN** un `MIEMBRO_COMITE` sin `AsignacionEvaluacion` sobre la postulación solicitada o cuya asignación se encuentra en estado `REASIGNADA_POR_CONFLICTO`
- **WHEN** intenta consultar las evaluaciones de coevaluadores de esa postulación, independientemente del estado de la convocatoria
- **THEN** el sistema responde `403 Forbidden` con `codigo = EVALUACION_CONFIDENCIAL` sin consultar `MS-PRG` ni revelar información de la postulación.

#### Scenario: Falla cerrada en consulta de coevaluadores ante convocatoria inexistente o MS-PRG no disponible

- **GIVEN** un `MIEMBRO_COMITE` con asignación vigente cuya convocatoria responde `404` en `MS-PRG` o `MS-PRG` presenta timeout, error HTTP o respuesta inválida
- **WHEN** intenta consultar las evaluaciones de coevaluadores de esa postulación
- **THEN** el sistema responde `404 Not Found` con `codigo = CONVOCATORIA_NO_ENCONTRADA` o `503 Service Unavailable` con `codigo = DEPENDENCIA_NO_DISPONIBLE`, respectivamente, sin exponer evaluaciones.

#### Scenario: Solicitud con identificador de postulación vacío

- **GIVEN** un `MIEMBRO_COMITE` autenticado que envía un `postulacionId` vacío
- **WHEN** invoca la consulta de evaluaciones de coevaluadores
- **THEN** el sistema responde `400 Bad Request` (`ValidationProblemDetails`) sin ejecutar consultas.

### Requirement: Persistencia íntegra y concurrencia de evaluaciones

El sistema `MS-POS` SHALL persistir `Evaluacion` y `CalificacionCriterio` garantizando unicidad 1:0..1 por `asignacionEvaluacionId`, unicidad por par `(evaluacionId, criterioEvaluacionId)`, coherencia entre estado, `fechaEnvio`, `puntajeTotalPonderado` y `motivoInvalidacion`, y control de concurrencia optimista por versión. Dos evaluadores calificando simultáneamente la misma postulación SHALL poder guardar y enviar sin bloquearse entre sí, con un único bloqueo efectivo de la rúbrica.

#### Scenario: Dos evaluadores califican la misma postulación en simultáneo

- **GIVEN** dos evaluadores con asignaciones distintas sobre la misma postulación y una rúbrica en estado `PUBLICADA`
- **WHEN** ambos guardan su primer borrador concurrentemente
- **THEN** ambos borradores se persisten sin conflicto en filas separadas, ambas asignaciones pasan a `EN_PROGRESO` y la rúbrica queda `BLOQUEADA` por `PRIMERA_CALIFICACION` una sola vez.

#### Scenario: Carrera al crear dos borradores simultáneos para la misma asignación

- **GIVEN** dos solicitudes simultáneas del mismo evaluador creando el primer borrador para la misma asignación
- **WHEN** ambas intentan confirmar en la base de datos
- **THEN** exactamente una se persiste y la otra responde `409 Conflict` con `codigo = EVALUACION_DUPLICADA`.

#### Scenario: Conflicto de concurrencia optimista sobre la misma evaluación

- **GIVEN** dos actualizaciones simultáneas sobre el mismo borrador ya persistido
- **WHEN** ambas intentan confirmar
- **THEN** la primera actualiza la versión y la segunda responde `409 Conflict` por conflicto de concurrencia.
