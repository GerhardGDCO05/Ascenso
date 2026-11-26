## ADDED Requirements

### Requirement: Autorización de la excepción de evaluador único

El sistema `MS-POS` SHALL permitir al `COORDINADOR_PROGRAMA` autorizar, con `justificacion` obligatoria de 10 a 1000 caracteres, una excepción de evaluador único para una postulación (RN-POS-04). El autorizador SHALL salir del usuario autenticado y la fecha UTC del servidor. SHALL existir como máximo una excepción por postulación y solo mientras tenga menos de 2 asignaciones vigentes.

#### Scenario: Autorización exitosa

- **GIVEN** una postulación con menos de 2 asignaciones vigentes y sin excepción
- **WHEN** el coordinador autoriza la excepción con una justificación válida
- **THEN** responde `201 Created` con la excepción (`id`, `postulacionId`, `justificacion`, `usuarioAutorizadorId` del token, `fechaAutorizacion` UTC del servidor) y un `Location` hacia la consulta de la excepción.

#### Scenario: Justificación ausente o inválida

- **GIVEN** una solicitud con `justificacion` vacía, solo espacios, menor de 10 o mayor de 1000 caracteres, o con `postulacionId` vacío
- **WHEN** el coordinador autoriza la excepción
- **THEN** responde `400 Bad Request` (`ProblemDetails`) y no se persiste nada; si la regla llega al dominio, el código es `JUSTIFICACION_REQUERIDA`.

#### Scenario: Excepción ya autorizada

- **GIVEN** una postulación que ya tiene una excepción autorizada
- **WHEN** el coordinador intenta autorizarla de nuevo (también con solicitudes simultáneas)
- **THEN** responde `409 Conflict` con `codigo = EXCEPCION_YA_AUTORIZADA`, la excepción original no cambia y no se genera una segunda fila.

#### Scenario: Excepción innecesaria

- **GIVEN** una postulación con 2 o más asignaciones vigentes
- **WHEN** el coordinador intenta autorizar la excepción
- **THEN** responde `422 Unprocessable Entity` con `codigo = EXCEPCION_NO_APLICABLE` y no se persiste nada.

#### Scenario: Usuario no autenticado en la capa de aplicación

- **GIVEN** un contexto sin usuario autenticado
- **WHEN** se ejecuta el comando
- **THEN** se rechaza con `NoAutenticadoException` sin persistir.

### Requirement: Consulta de la excepción de evaluador único

El sistema `MS-POS` SHALL permitir al `COORDINADOR_PROGRAMA` consultar la excepción de evaluador único de una postulación, incluyendo quién la autorizó, cuándo (UTC) y su justificación.

#### Scenario: Postulación con excepción

- **GIVEN** una postulación con excepción autorizada
- **WHEN** el coordinador consulta `GET /api/v1/asignaciones-evaluacion/postulaciones/{postulacionId}/excepcion-evaluador-unico`
- **THEN** responde `200 OK` con `id`, `postulacionId`, `justificacion`, `usuarioAutorizadorId` y `fechaAutorizacion`.

#### Scenario: Postulación sin excepción

- **GIVEN** una postulación sin excepción
- **WHEN** el coordinador la consulta
- **THEN** responde `404 Not Found` con `codigo = EXCEPCION_NO_ENCONTRADA`.

### Requirement: Autorización por rol de la excepción de evaluador único

Los endpoints de autorización y consulta de la excepción SHALL permitirse exclusivamente al rol literal `COORDINADOR_PROGRAMA`. Cualquier otro rol autenticado SHALL recibir `403 Forbidden` con `ProblemDetails` y `codigo = PERMISO_DENEGADO`; sin token SHALL responder `401`. Los contratos de `Postulantes`, `Rubricas`, `Comites` y de los demás endpoints de `Asignaciones` no cambian.

#### Scenario: Roles no permitidos

- **GIVEN** un usuario con rol `MIEMBRO_COMITE` o `ADMIN_FINANCIERO`
- **WHEN** intenta autorizar o consultar la excepción
- **THEN** responde `403 Forbidden` con `codigo = PERMISO_DENEGADO`.

#### Scenario: Sin autenticación

- **GIVEN** una solicitud sin token
- **WHEN** invoca cualquiera de los dos endpoints
- **THEN** responde `401 Unauthorized`.

### Requirement: Trazabilidad permanente e inmutable de la excepción

La autorización de excepción de evaluador único SHALL ser un registro append-only que nunca se edita ni se elimina de forma silenciosa (RF-35, RN-AUD-01). La persistencia SHALL almacenar las fechas como `timestamptz`, imponer un único registro por `postulacionId`, exigir una justificación no vacía mediante `CHECK` y rechazar `UPDATE` y `DELETE` mediante un trigger de PostgreSQL.

#### Scenario: Violación de unicidad por postulación

- **GIVEN** una excepción persistida para una postulación
- **WHEN** se intenta insertar otra para la misma postulación
- **THEN** la base de datos rechaza la fila y `TraductorExcepcionesPersistencia` la traduce a `EXCEPCION_YA_AUTORIZADA` (`409`).

#### Scenario: Justificación vacía en la base de datos

- **GIVEN** una inserción directa con justificación vacía o de solo espacios
- **WHEN** se confirma
- **THEN** la base de datos rechaza la fila por la restricción `CHECK`.

#### Scenario: Intento de modificar o borrar

- **GIVEN** una excepción persistida
- **WHEN** se ejecuta un `UPDATE` o `DELETE` sobre su fila
- **THEN** PostgreSQL rechaza la operación y la fila permanece idéntica.

#### Scenario: Recarga fiel

- **GIVEN** una excepción persistida
- **WHEN** se recarga con otro `DbContext`
- **THEN** conserva `postulacionId`, `justificacion`, `usuarioAutorizadorId`, `fechaAutorizacion` en UTC y `correlacionId`.

## MODIFIED Requirements

### Requirement: Asignación de una postulación a evaluadores del comité

El sistema `MS-POS` SHALL permitir al `COORDINADOR_PROGRAMA` asignar una postulación a evaluadores activos de un `ComiteEvaluador` en una operación atómica, con `fechaLimiteEvaluacion` futura. Cada `AsignacionEvaluacion` SHALL nacer `PENDIENTE` con la `convocatoriaId` del comité. Tras la operación la postulación SHALL tener al menos 2 asignaciones vigentes de evaluadores distintos del mismo comité (RN-POS-04), salvo que exista una excepción de evaluador único autorizada para esa postulación, caso en que el mínimo efectivo SHALL ser 1; si no se cumple el mínimo efectivo, no se persiste nada.

#### Scenario: Asignación manual a dos evaluadores

- **GIVEN** un comité con al menos dos miembros activos y una postulación sin asignaciones
- **WHEN** el coordinador asigna la postulación indicando dos `usuariosEvaluadoresIds` distintos y una fecha límite futura
- **THEN** responde `201 Created` con dos asignaciones en `PENDIENTE`, con `convocatoriaId` del comité, `miembroComiteId` y `usuarioEvaluadorId` de cada evaluador y `fechaAsignacion` del servidor.

#### Scenario: Menos de dos evaluadores resultantes

- **GIVEN** una postulación sin asignaciones vigentes y sin excepción de evaluador único
- **WHEN** el coordinador solicita un solo evaluador (o dos veces el mismo usuario)
- **THEN** responde `422 Unprocessable Entity` con `codigo = EVALUACIONES_INSUFICIENTES` y no se persiste ninguna asignación.

#### Scenario: Un solo evaluador con excepción autorizada en modo manual

- **GIVEN** una postulación sin asignaciones vigentes y con excepción de evaluador único autorizada
- **WHEN** el coordinador asigna un único evaluador activo del comité
- **THEN** responde `201 Created` con una asignación `PENDIENTE`.

#### Scenario: Un solo evaluador con excepción autorizada en modo automático

- **GIVEN** una postulación con excepción autorizada y un comité con miembros activos
- **WHEN** el coordinador solicita `cantidadEvaluadores = 1`
- **THEN** se selecciona el miembro elegible de menor carga y responde `201 Created` con una asignación `PENDIENTE`.

#### Scenario: La excepción no relaja otras reglas

- **GIVEN** una postulación con excepción autorizada
- **WHEN** el coordinador no indica ningún evaluador, o indica un usuario que no es miembro activo del comité o ya vigente
- **THEN** responde con el mismo error que sin excepción (`400`, `EVALUADOR_NO_MIEMBRO_ACTIVO` o `ASIGNACION_DUPLICADA`) y no se persiste nada.

#### Scenario: Evaluador adicional sobre una postulación ya asignada

- **GIVEN** una postulación con dos asignaciones vigentes
- **WHEN** el coordinador asigna un tercer evaluador activo del mismo comité
- **THEN** responde `201 Created` con la nueva asignación `PENDIENTE` y la postulación conserva sus asignaciones anteriores.

#### Scenario: Comité distinto al de las asignaciones vigentes

- **GIVEN** una postulación con asignaciones vigentes de un comité
- **WHEN** el coordinador intenta asignarla con otro comité
- **THEN** responde `422 Unprocessable Entity` con `codigo = ASIGNACION_INVALIDA` y no se persiste nada.

#### Scenario: Datos de entrada inválidos

- **GIVEN** una solicitud con `comiteId` o `postulacionId` vacíos, `fechaLimiteEvaluacion` vacía, identificadores de evaluador vacíos, o `cantidadEvaluadores` menor que 1 sin lista de evaluadores
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
