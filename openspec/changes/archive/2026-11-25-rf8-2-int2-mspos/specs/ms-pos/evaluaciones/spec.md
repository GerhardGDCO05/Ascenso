# Spec Delta — `ms-pos/evaluaciones`

## ADDED Requirements

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

## MODIFIED Requirements

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
