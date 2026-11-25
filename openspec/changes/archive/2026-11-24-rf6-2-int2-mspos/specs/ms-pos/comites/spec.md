## ADDED Requirements

### Requirement: Asignación de una postulación a evaluadores del comité

El sistema `MS-POS` SHALL permitir al `COORDINADOR_PROGRAMA` asignar una postulación a evaluadores activos de un `ComiteEvaluador` en una operación atómica, con `fechaLimiteEvaluacion` futura. Cada `AsignacionEvaluacion` SHALL nacer `PENDIENTE` con la `convocatoriaId` del comité. Tras la operación la postulación SHALL tener al menos 2 asignaciones vigentes de evaluadores distintos del mismo comité (RN-POS-04); si no, no se persiste nada.

#### Scenario: Asignación manual a dos evaluadores

- **GIVEN** un comité con al menos dos miembros activos y una postulación sin asignaciones
- **WHEN** el coordinador asigna la postulación indicando dos `usuariosEvaluadoresIds` distintos y una fecha límite futura
- **THEN** responde `201 Created` con dos asignaciones en `PENDIENTE`, con `convocatoriaId` del comité, `miembroComiteId` y `usuarioEvaluadorId` de cada evaluador y `fechaAsignacion` del servidor.

#### Scenario: Menos de dos evaluadores resultantes

- **GIVEN** una postulación sin asignaciones vigentes
- **WHEN** el coordinador solicita un solo evaluador (o dos veces el mismo usuario)
- **THEN** responde `422 Unprocessable Entity` con `codigo = EVALUACIONES_INSUFICIENTES` y no se persiste ninguna asignación.

#### Scenario: Evaluador adicional sobre una postulación ya asignada

- **GIVEN** una postulación con dos asignaciones vigentes
- **WHEN** el coordinador asigna un tercer evaluador activo del mismo comité
- **THEN** responde `201 Created` con la nueva asignación `PENDIENTE` y la postulación conserva sus asignaciones anteriores.

#### Scenario: Comité distinto al de las asignaciones vigentes

- **GIVEN** una postulación con asignaciones vigentes de un comité
- **WHEN** el coordinador intenta asignarla con otro comité
- **THEN** responde `422 Unprocessable Entity` con `codigo = ASIGNACION_INVALIDA` y no se persiste nada.

#### Scenario: Datos de entrada inválidos

- **GIVEN** una solicitud con `comiteId` o `postulacionId` vacíos, `fechaLimiteEvaluacion` vacía, identificadores de evaluador vacíos, o `cantidadEvaluadores` menor que 2 sin lista de evaluadores
- **WHEN** se envía la asignación
- **THEN** responde `400 Bad Request` (`ProblemDetails` de validación) y no ejecuta la operación.

#### Scenario: Fecha límite no futura

- **GIVEN** una `fechaLimiteEvaluacion` igual o anterior al instante de la asignación
- **WHEN** el coordinador asigna la postulación
- **THEN** responde `422 Unprocessable Entity` con `codigo = ASIGNACION_INVALIDA` y no se persiste nada.

#### Scenario: Comité inexistente o inactivo

- **GIVEN** un `comiteId` inexistente, o de un comité con `activo = false`
- **WHEN** el coordinador asigna la postulación
- **THEN** responde `404 Not Found` con `codigo = COMITE_NO_ENCONTRADO` o `422 Unprocessable Entity` con `codigo = COMITE_INACTIVO`, respectivamente.

### Requirement: Evaluador activo perteneciente al comité

El sistema `MS-POS` SHALL asignar únicamente a usuarios con una membresía activa (`activo = true`) en el comité indicado; los miembros `INTERNO` y `EXTERNO` son elegibles. Un usuario no miembro, o cuya membresía fue desvinculada, SHALL ser rechazado. El evaluador SHALL ser independiente: un mismo `usuarioEvaluadorId` SHALL NOT aparecer dos veces en una misma solicitud ni estar vigente dos veces para la misma postulación.

#### Scenario: Usuario que no es miembro activo del comité

- **GIVEN** un `usuarioEvaluadorId` que no pertenece al comité o cuya membresía está desvinculada
- **WHEN** el coordinador lo incluye en la asignación
- **THEN** responde `422 Unprocessable Entity` con `codigo = EVALUADOR_NO_MIEMBRO_ACTIVO` y no se persiste ninguna asignación de la solicitud.

#### Scenario: Evaluador externo elegible

- **GIVEN** un miembro activo `EXTERNO` del comité
- **WHEN** el coordinador lo asigna junto con otro miembro activo
- **THEN** la asignación del evaluador externo se crea en `PENDIENTE`.

#### Scenario: Evaluador repetido en la misma solicitud

- **GIVEN** una solicitud con el mismo `usuarioEvaluadorId` dos veces
- **WHEN** se envía la asignación
- **THEN** el evaluador cuenta una sola vez y, si el total resultante es menor que 2, responde `422` con `codigo = EVALUACIONES_INSUFICIENTES`.

### Requirement: No duplicar evaluador vigente por postulación y concurrencia

El sistema `MS-POS` SHALL impedir que un evaluador tenga dos asignaciones vigentes para la misma postulación, incluso ante solicitudes simultáneas, garantizándolo con un índice único parcial en PostgreSQL (`postulacion_id`, `usuario_evaluador_id` para estados distintos de `REASIGNADA_POR_CONFLICTO`). Un evaluador con asignación `REASIGNADA_POR_CONFLICTO` SHALL NOT bloquear una nueva asignación de ese usuario por los mecanismos del sistema.

#### Scenario: Evaluador ya vigente en la postulación

- **GIVEN** una postulación con una asignación vigente del evaluador A
- **WHEN** el coordinador intenta asignar de nuevo al evaluador A
- **THEN** responde `409 Conflict` con `codigo = ASIGNACION_DUPLICADA` y no se persiste ninguna asignación de la solicitud.

#### Scenario: Dos solicitudes simultáneas con el mismo evaluador

- **GIVEN** dos coordinadores que asignan la misma postulación al mismo evaluador a la vez
- **WHEN** ambas confirman en la base de datos
- **THEN** exactamente una se persiste y la otra responde `409 Conflict` con `codigo = ASIGNACION_DUPLICADA`.

### Requirement: Selección balanceada de evaluadores

El sistema `MS-POS` SHALL permitir al coordinador delegar la selección omitiendo `usuariosEvaluadoresIds` e indicando `cantidadEvaluadores` (mínimo 2). El sistema SHALL elegir miembros activos no vigentes en la postulación con menor carga (asignaciones del comité `PENDIENTE` o `EN_PROGRESO`), con desempate determinista por `fechaAsignacion` de la membresía y `miembroComiteId`.

#### Scenario: Selección por menor carga

- **GIVEN** un comité con tres evaluadores activos con 0, 1 y 3 asignaciones abiertas
- **WHEN** el coordinador solicita `cantidadEvaluadores = 2` sin lista explícita
- **THEN** se asignan los evaluadores con 0 y 1 asignaciones abiertas.

#### Scenario: Empate de carga determinista

- **GIVEN** evaluadores activos con la misma carga
- **WHEN** se solicita selección automática repetida sobre el mismo estado
- **THEN** se elige siempre en el mismo orden (`fechaAsignacion` de la membresía, luego `miembroComiteId`).

#### Scenario: Miembros elegibles insuficientes

- **GIVEN** un comité con un solo miembro elegible para la postulación
- **WHEN** el coordinador solicita selección automática de 2 evaluadores
- **THEN** responde `422 Unprocessable Entity` con `codigo = EVALUACIONES_INSUFICIENTES` y no se persiste nada.

### Requirement: Consulta de asignaciones por postulación y carga por evaluador

El sistema `MS-POS` SHALL permitir al `COORDINADOR_PROGRAMA` consultar las asignaciones de una postulación (todos los estados, ordenadas por `fechaAsignacion` e `id`; lista vacía si no tiene) y la carga por evaluador de un comité (miembros activos con su cantidad de asignaciones abiertas, incluidos los de carga cero).

#### Scenario: Asignaciones de una postulación

- **GIVEN** una postulación con asignaciones
- **WHEN** el coordinador consulta `GET /api/v1/asignaciones-evaluacion/postulaciones/{postulacionId}`
- **THEN** responde `200 OK` con todas las asignaciones, incluidos `usuarioEvaluadorId`, `estado`, fechas y `version`.

#### Scenario: Carga por evaluador del comité

- **GIVEN** un comité existente
- **WHEN** el coordinador consulta la carga del comité
- **THEN** responde `200 OK` con cada miembro activo y su número de asignaciones abiertas, y `404 COMITE_NO_ENCONTRADO` si el comité no existe.

### Requirement: Asignaciones propias del evaluador con evaluación ciega

El sistema `MS-POS` SHALL permitir al `MIEMBRO_COMITE` consultar únicamente sus propias asignaciones (usuario del claim `sub`, nunca enviado por el cliente), paginadas y con filtro opcional por `estado`. La respuesta SHALL NOT incluir la identidad del postulante ni de los demás evaluadores (RN-POS-04, evaluación ciega).

#### Scenario: El evaluador ve solo sus asignaciones

- **GIVEN** asignaciones de varios evaluadores
- **WHEN** un `MIEMBRO_COMITE` consulta `GET /api/v1/asignaciones-evaluacion/mis-asignaciones`
- **THEN** responde `200 OK` solo con las asignaciones de su propio usuario, con el total para paginar, `pagina` desde 1, `tamano` máximo 100, orden por `fechaLimiteEvaluacion` e `id`, y los campos `asignacionEvaluacionId`, `postulacionId`, `convocatoriaId`, `estado`, `fechaAsignacion` y `fechaLimiteEvaluacion`, sin campos que revelen a los coevaluadores.

#### Scenario: Filtro y paginación

- **GIVEN** un evaluador con asignaciones en distintos estados
- **WHEN** consulta con `estado = PENDIENTE`, `pagina` y `tamano`
- **THEN** recibe solo las `PENDIENTE` de esa página; un `tamano` mayor que 100, una `pagina` menor que 1 o un `estado` inválido responden `400 Bad Request`.

### Requirement: Autorización de la asignación de evaluación

El sistema `MS-POS` SHALL exigir el rol literal `COORDINADOR_PROGRAMA` para asignar y para las consultas por postulación y por carga del comité, y el rol literal `MIEMBRO_COMITE` para `mis-asignaciones`. Cualquier otro rol, incluido `ADMIN_FINANCIERO` en todos los endpoints, SHALL recibir `403 Forbidden` con `codigo = PERMISO_DENEGADO` en `ProblemDetails`; sin token SHALL responder `401`. Los endpoints de comités de HU 6.1 mantienen su autorización sin cambios.

#### Scenario: Rol no permitido

- **GIVEN** un usuario con rol `ADMIN_FINANCIERO`, o `MIEMBRO_COMITE` en endpoints de coordinador, o `COORDINADOR_PROGRAMA` en `mis-asignaciones`
- **WHEN** invoca el endpoint
- **THEN** responde `403 Forbidden` con `codigo = PERMISO_DENEGADO`.

#### Scenario: Sin autenticación

- **GIVEN** una solicitud sin token válido
- **WHEN** invoca cualquier endpoint de asignaciones
- **THEN** responde `401 Unauthorized`.

### Requirement: Persistencia e integridad de las asignaciones

El sistema `MS-POS` SHALL persistir las asignaciones en `postulaciones.asignaciones_evaluacion` con fechas `timestamptz`, `CHECK` de estado y de `fecha_limite_evaluacion > fecha_asignacion`, claves foráneas sin cascada e índice único parcial de evaluador vigente por postulación. Los conflictos de `Version` SHALL responder `409 Conflict`.

#### Scenario: Violación de restricciones en la base de datos

- **GIVEN** una inserción con estado fuera de catálogo, fecha límite no posterior a la asignación o evaluador vigente duplicado
- **WHEN** se confirma en PostgreSQL
- **THEN** la base de datos la rechaza y la infraestructura traduce el índice único a `ASIGNACION_DUPLICADA`.

#### Scenario: Desvinculación de un miembro con asignaciones

- **GIVEN** un miembro desvinculado (baja lógica de HU 6.1) con asignaciones previas
- **WHEN** se consulta su historial de asignaciones
- **THEN** las asignaciones se conservan sin alteración (la membresía no se borra físicamente).
