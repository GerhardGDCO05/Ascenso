## ADDED Requirements

### Requirement: Bloqueo de la rúbrica publicada

El sistema `MS-POS` SHALL pasar una `RubricaEvaluacion` de `PUBLICADA` a `BLOQUEADA` al iniciar la fase de evaluación del ciclo o al registrarse la primera puntuación (lo que ocurra primero), registrando `motivoBloqueo` y `fechaBloqueo`. La transición es terminal y se aplica en el servidor mediante un caso de uso interno (actor `UsuarioSistema`), sin endpoint HTTP.

#### Scenario: Bloqueo por inicio de la evaluación

- **GIVEN** una rúbrica en estado `PUBLICADA`
- **WHEN** el sistema solicita su bloqueo con motivo `INICIO_EVALUACION`
- **THEN** la rúbrica pasa a `BLOQUEADA` con `motivoBloqueo = INICIO_EVALUACION` y `fechaBloqueo` igual a la fecha del bloqueo.

#### Scenario: Bloqueo por primera calificación

- **GIVEN** una rúbrica en estado `PUBLICADA`
- **WHEN** el sistema solicita su bloqueo con motivo `PRIMERA_CALIFICACION`
- **THEN** la rúbrica pasa a `BLOQUEADA` con `motivoBloqueo = PRIMERA_CALIFICACION` y `fechaBloqueo` registrada.

#### Scenario: Rechazo del bloqueo de una rúbrica en borrador

- **GIVEN** una rúbrica en estado `BORRADOR`
- **WHEN** el sistema solicita su bloqueo
- **THEN** responde con `409 Conflict` y `codigo = RUBRICA_NO_PUBLICADA`, y la rúbrica permanece en `BORRADOR` sin persistir cambios.

#### Scenario: Bloqueo de una rúbrica inexistente

- **GIVEN** un identificador de rúbrica que no existe
- **WHEN** el sistema solicita su bloqueo
- **THEN** el caso de uso falla con rúbrica no encontrada (`404` si se propaga por HTTP) y no persiste cambios.

#### Scenario: Sin endpoint HTTP de bloqueo

- **GIVEN** cualquier usuario autenticado, incluido `COORDINADOR_PROGRAMA`
- **WHEN** intenta bloquear una rúbrica por la API
- **THEN** no existe ninguna ruta que lo permita, por lo que el bloqueo solo lo ejecutan los disparadores internos del sistema.

### Requirement: Bloqueo idempotente y seguro bajo concurrencia

El sistema `MS-POS` SHALL tratar el bloqueo de una rúbrica ya `BLOQUEADA` como una operación idempotente: no modifica `motivoBloqueo`, `fechaBloqueo` ni la auditoría, no persiste cambios y termina sin error. Ante dos solicitudes de bloqueo simultáneas sobre la misma rúbrica `PUBLICADA`, SHALL producirse un único cambio efectivo (conservando el motivo y la fecha de la solicitud ganadora) y ambas solicitudes SHALL terminar sin error.

#### Scenario: Bloqueo repetido de una rúbrica bloqueada

- **GIVEN** una rúbrica `BLOQUEADA` por `INICIO_EVALUACION` en una fecha determinada
- **WHEN** el sistema solicita de nuevo su bloqueo (con el mismo u otro motivo)
- **THEN** termina sin error y `motivoBloqueo`, `fechaBloqueo` y la versión de la rúbrica no cambian.

#### Scenario: Dos bloqueos simultáneos

- **GIVEN** una rúbrica `PUBLICADA` y dos solicitudes de bloqueo que la leen antes de que cualquiera confirme su cambio
- **WHEN** ambas intentan confirmar
- **THEN** exactamente una produce el cambio a `BLOQUEADA`, la otra detecta el conflicto, relee el estado y termina sin error, y la rúbrica queda con un solo motivo y una sola fecha de bloqueo.

#### Scenario: Conflicto de concurrencia ajeno al bloqueo

- **GIVEN** un conflicto de concurrencia al confirmar el bloqueo, tras el cual la rúbrica releída no está `BLOQUEADA`
- **WHEN** el caso de uso evalúa el resultado
- **THEN** propaga el conflicto (`409`) en lugar de ocultarlo.

### Requirement: Mensaje explicativo ante modificación de una rúbrica no editable

El sistema `MS-POS` SHALL rechazar con `409 Conflict` y `codigo = RUBRICA_BLOQUEADA` cualquier intento de modificar una rúbrica no editable, con un mensaje que explica el motivo: para una rúbrica `PUBLICADA` indica que está publicada y que los cambios requieren una rúbrica nueva; para una rúbrica `BLOQUEADA` indica «bloqueada desde {fecha} por {motivo}» (fecha en UTC y motivo en texto legible) y que los cambios requieren una rúbrica nueva.

#### Scenario: Modificación de una rúbrica publicada

- **GIVEN** una rúbrica `PUBLICADA`
- **WHEN** el coordinador intenta agregar, editar o eliminar un criterio
- **THEN** responde `409 Conflict`, `codigo = RUBRICA_BLOQUEADA` y un mensaje que indica que la rúbrica está publicada.

#### Scenario: Modificación de una rúbrica bloqueada

- **GIVEN** una rúbrica `BLOQUEADA` el `2026-11-22` por `INICIO_EVALUACION`
- **WHEN** el coordinador intenta agregar, editar, eliminar un criterio o publicar
- **THEN** responde `409 Conflict`, `codigo = RUBRICA_BLOQUEADA` y un mensaje que contiene la fecha de bloqueo y el motivo, sin modificar la rúbrica.

### Requirement: Consulta de la rúbrica en solo lectura con indicador de inalterabilidad

El sistema `MS-POS` SHALL incluir en la consulta de la rúbrica los campos `motivoBloqueo` y `fechaBloqueo` (nulos si no está bloqueada) y `soloLectura`, que es `true` cuando `estado` es distinto de `BORRADOR` y `false` en `BORRADOR`. La consulta SHALL estar disponible para coordinadores y evaluadores (miembros de comité) y nunca altera el estado de la rúbrica.

#### Scenario: Consulta de una rúbrica bloqueada

- **GIVEN** una rúbrica `BLOQUEADA` por `PRIMERA_CALIFICACION`
- **WHEN** un `COORDINADOR_PROGRAMA` o un `MIEMBRO_COMITE` la consulta
- **THEN** responde `200 OK` con `estado = BLOQUEADA`, `motivoBloqueo = PRIMERA_CALIFICACION`, `fechaBloqueo` y `soloLectura = true`.

#### Scenario: Consulta de una rúbrica publicada

- **GIVEN** una rúbrica `PUBLICADA`
- **WHEN** se consulta
- **THEN** responde `200 OK` con `soloLectura = true`, `motivoBloqueo = null` y `fechaBloqueo = null`.

#### Scenario: Consulta de una rúbrica en borrador

- **GIVEN** una rúbrica `BORRADOR`
- **WHEN** el coordinador la consulta
- **THEN** responde `200 OK` con `soloLectura = false`, `motivoBloqueo = null` y `fechaBloqueo = null`.

### Requirement: Coherencia persistida del bloqueo

El sistema `MS-POS` SHALL garantizar en la base de datos que `motivoBloqueo` y `fechaBloqueo` sean ambos no nulos si y solo si el estado es `BLOQUEADA`, y ambos nulos en `BORRADOR` y `PUBLICADA`.

#### Scenario: Estado bloqueado sin motivo o sin fecha

- **GIVEN** una fila con estado `BLOQUEADA` y `motivo_bloqueo` o `fecha_bloqueo` nulos
- **WHEN** se intenta persistir
- **THEN** la restricción `CHECK` rechaza la operación.

#### Scenario: Estado no bloqueado con datos de bloqueo

- **GIVEN** una fila con estado `BORRADOR` o `PUBLICADA` y `motivo_bloqueo` o `fecha_bloqueo` no nulos
- **WHEN** se intenta persistir
- **THEN** la restricción `CHECK` rechaza la operación.

## MODIFIED Requirements

### Requirement: Autorización por rol sobre las rúbricas

El sistema `MS-POS` SHALL exigir la política de escritura `CoordinadorPrograma` (rol `COORDINADOR_PROGRAMA` en el claim `roles`) para crear, editar, eliminar criterios y publicar rúbricas, y una política de lectura independiente (`LecturaRubrica`, que permite `COORDINADOR_PROGRAMA` y `MIEMBRO_COMITE`, este último solo en lectura) para consultarlas. Cualquier otro rol SHALL recibir `403 Forbidden` con `ProblemDetails` y `codigo = PERMISO_DENEGADO`, sin alterar los endpoints de Postulantes.

#### Scenario: Acceso del coordinador

- **GIVEN** un usuario autenticado con rol `COORDINADOR_PROGRAMA`
- **WHEN** invoca cualquier operación de rúbricas
- **THEN** el sistema procesa la solicitud según las reglas de negocio.

#### Scenario: Acceso del administrador financiero

- **GIVEN** un usuario autenticado con rol `ADMIN_FINANCIERO`
- **WHEN** intenta crear, editar, publicar o consultar una rúbrica
- **THEN** el sistema responde `403 Forbidden` con `codigo = PERMISO_DENEGADO` y no ejecuta la operación.

#### Scenario: Acceso de un miembro de comité

- **GIVEN** un usuario autenticado con rol `MIEMBRO_COMITE`
- **WHEN** consulta una rúbrica por identificador
- **THEN** el sistema responde `200 OK` con la rúbrica en solo lectura
- **AND** si intenta crear una rúbrica, agregar, editar o eliminar criterios o publicar, responde `403 Forbidden` con `codigo = PERMISO_DENEGADO` y no ejecuta la operación.

#### Scenario: Usuario sin autenticar

- **GIVEN** una solicitud sin token válido
- **WHEN** invoca cualquier operación de rúbricas
- **THEN** el sistema responde `401 Unauthorized`.
