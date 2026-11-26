## ADDED Requirements

### Requirement: Conformación del comité evaluador por programa y convocatoria

El sistema `MS-POS` SHALL permitir al `COORDINADOR_PROGRAMA` crear el `ComiteEvaluador` de un programa y convocatoria con nombre (máximo 200 caracteres) y `jefaturaUsuarioId`, registrando `fechaConformacion` (fecha UTC) y `conformadoPorUsuarioId` (usuario autenticado). El comité SHALL nacer activo con la jefatura como primer miembro (`JEFATURA`, `INTERNO`) y SHALL existir como máximo uno por programa y convocatoria.

#### Scenario: Creación exitosa del comité

- **GIVEN** un `COORDINADOR_PROGRAMA` autenticado y un programa y convocatoria sin comité
- **WHEN** crea el comité con nombre y jefatura válidos
- **THEN** responde `201 Created` con el comité activo, `fechaConformacion` y `conformadoPorUsuarioId` registrados y un único miembro activo con `rol = JEFATURA` y `tipo = INTERNO` correspondiente a la jefatura.

#### Scenario: Comité duplicado para el programa y la convocatoria

- **GIVEN** un programa y convocatoria que ya tienen comité
- **WHEN** se intenta crear otro comité para la misma combinación, incluso de forma simultánea
- **THEN** responde `409 Conflict` con `codigo = COMITE_DUPLICADO` y no se persiste un segundo comité.

#### Scenario: Datos obligatorios inválidos

- **GIVEN** una solicitud con nombre vacío o de más de 200 caracteres, o con identificadores vacíos (`programaBecaId`, `convocatoriaId`, `jefaturaUsuarioId`)
- **WHEN** se envía la creación
- **THEN** responde `400 Bad Request` (`ProblemDetails` con código de validación) y no ejecuta la operación.

### Requirement: Miembros internos y externos del comité

El sistema `MS-POS` SHALL permitir al `COORDINADOR_PROGRAMA` agregar al comité evaluadores `INTERNO` o `EXTERNO` con `rol = EVALUADOR`, registrando `fechaAsignacion` (fecha UTC del servidor) y `activo = true`. Un mismo `usuarioEvaluadorId` SHALL NOT estar activo dos veces en el mismo comité. Un evaluador desvinculado SHALL poder reincorporarse mediante una nueva membresía.

#### Scenario: Agregar evaluador interno

- **GIVEN** un comité existente
- **WHEN** el coordinador agrega un usuario con `tipo = INTERNO`
- **THEN** responde `201 Created` con el comité y el nuevo miembro activo con `rol = EVALUADOR`, `tipo = INTERNO` y `fechaAsignacion`.

#### Scenario: Agregar evaluador externo

- **GIVEN** un comité existente
- **WHEN** el coordinador agrega un usuario con `tipo = EXTERNO`
- **THEN** el comité incluye al miembro activo con `tipo = EXTERNO` y `rol = EVALUADOR`.

#### Scenario: Miembro ya activo en el comité

- **GIVEN** un usuario que ya es miembro activo del comité (incluida la jefatura)
- **WHEN** se intenta agregarlo de nuevo
- **THEN** responde `409 Conflict` con `codigo = MIEMBRO_COMITE_DUPLICADO` y el comité no cambia.

#### Scenario: Tipo de evaluador no válido

- **GIVEN** una solicitud con un `tipo` distinto de `INTERNO` o `EXTERNO`, o con `usuarioEvaluadorId` vacío
- **WHEN** se envía la solicitud
- **THEN** responde `400 Bad Request` y no ejecuta la operación.

#### Scenario: Comité inexistente

- **GIVEN** un identificador de comité que no existe
- **WHEN** se agrega un miembro, se desvincula uno o se consulta
- **THEN** responde `404 Not Found` con `codigo = COMITE_NO_ENCONTRADO`.

#### Scenario: Reincorporación de un miembro desvinculado

- **GIVEN** un evaluador previamente desvinculado del comité
- **WHEN** el coordinador lo agrega de nuevo
- **THEN** se crea una nueva membresía activa y la membresía anterior conserva su `fechaDesvinculacion`.

### Requirement: Desvinculación de miembros con historial

El sistema `MS-POS` SHALL permitir al `COORDINADOR_PROGRAMA` desvincular a un miembro activo del comité marcándolo `activo = false` y registrando `fechaDesvinculacion` (fecha UTC del servidor), sin borrado físico. La jefatura SHALL NOT poder desvincularse. Desvincular a un miembro ya desvinculado SHALL ser idempotente: no cambia la `fechaDesvinculacion` ni la versión del comité y termina sin error.

#### Scenario: Desvinculación exitosa

- **GIVEN** un evaluador activo del comité
- **WHEN** el coordinador lo desvincula
- **THEN** responde `200 OK` con el miembro `activo = false`, `fechaDesvinculacion` registrada y el resto del comité intacto.

#### Scenario: Desvinculación de la jefatura

- **GIVEN** la membresía con `rol = JEFATURA`
- **WHEN** se intenta desvincularla
- **THEN** responde `422 Unprocessable Entity` con `codigo = JEFATURA_NO_DESVINCULABLE` y el comité no cambia.

#### Scenario: Desvinculación repetida

- **GIVEN** un miembro ya desvinculado en una fecha determinada
- **WHEN** se solicita de nuevo su desvinculación
- **THEN** responde `200 OK` sin error y `fechaDesvinculacion` y la versión del comité no cambian.

#### Scenario: Miembro inexistente en el comité

- **GIVEN** un identificador de miembro que no pertenece al comité
- **WHEN** se solicita su desvinculación
- **THEN** responde `404 Not Found` con `codigo = MIEMBRO_COMITE_NO_ENCONTRADO`.

### Requirement: Consulta del comité evaluador

El sistema `MS-POS` SHALL permitir al `COORDINADOR_PROGRAMA` consultar un comité por su identificador y por programa y convocatoria, incluyendo todos sus miembros (activos y desvinculados) con `tipo`, `rol`, fechas y `activo`. Los códigos de enumeración se exponen en mayúsculas (`INTERNO`, `EXTERNO`, `JEFATURA`, `EVALUADOR`).

#### Scenario: Consulta por identificador

- **GIVEN** un comité con miembros activos y desvinculados
- **WHEN** el coordinador consulta `GET /api/v1/comites-evaluadores/{id}`
- **THEN** responde `200 OK` con el comité y todos sus miembros.

#### Scenario: Consulta por programa y convocatoria

- **GIVEN** un programa y convocatoria con comité
- **WHEN** el coordinador consulta por programa y convocatoria
- **THEN** responde `200 OK` con ese comité; si no existe, `404 Not Found` con `codigo = COMITE_NO_ENCONTRADO`.

### Requirement: Autorización por rol sobre los comités evaluadores

El sistema `MS-POS` SHALL exigir el rol `COORDINADOR_PROGRAMA` (claim `roles`) para crear, consultar y modificar comités evaluadores. Todo usuario autenticado con otro rol, incluidos `ADMIN_FINANCIERO` y `MIEMBRO_COMITE`, SHALL recibir `403 Forbidden` con `codigo = PERMISO_DENEGADO` en formato `ProblemDetails` y la operación no se ejecuta. Una solicitud sin autenticar SHALL recibir `401 Unauthorized`. Los endpoints de `Postulantes` y `Rubricas` SHALL mantener su comportamiento.

#### Scenario: Coordinador autorizado

- **GIVEN** un usuario con rol `COORDINADOR_PROGRAMA`
- **WHEN** invoca cualquier endpoint de comités
- **THEN** la autorización se concede.

#### Scenario: Rol financiero denegado

- **GIVEN** un usuario con rol `ADMIN_FINANCIERO`
- **WHEN** intenta crear, consultar, agregar o desvincular miembros
- **THEN** responde `403 Forbidden` con `codigo = PERMISO_DENEGADO` y no ejecuta la operación.

#### Scenario: Miembro del comité denegado

- **GIVEN** un usuario con rol `MIEMBRO_COMITE`
- **WHEN** intenta consultar o modificar un comité
- **THEN** responde `403 Forbidden` con `codigo = PERMISO_DENEGADO`.

#### Scenario: Sin autenticación

- **GIVEN** una solicitud sin token válido
- **WHEN** invoca un endpoint de comités
- **THEN** responde `401 Unauthorized`.

### Requirement: Persistencia íntegra del comité y sus miembros

El sistema `MS-POS` SHALL persistir el comité y sus miembros en el esquema `postulaciones` (`comites_evaluadores`, `miembros_comite`) con auditoría `timestamptz` y fechas de negocio `date`. La base de datos SHALL imponer por sí misma la unicidad por programa y convocatoria, una sola membresía activa por usuario, una sola jefatura activa, los valores válidos de `tipo` y `rol`, la coherencia entre `activo` y `fecha_desvinculacion` y que la jefatura sea `INTERNO`.

#### Scenario: Violación de unicidad por programa y convocatoria

- **GIVEN** un comité persistido para un programa y convocatoria
- **WHEN** se inserta otro comité para la misma combinación
- **THEN** PostgreSQL rechaza la operación por `ux_comites_programa_convocatoria`, traducida a `COMITE_DUPLICADO`.

#### Scenario: Violación de unicidad de membresía activa

- **GIVEN** un usuario con membresía activa en un comité
- **WHEN** se inserta otra membresía activa del mismo usuario en el comité
- **THEN** PostgreSQL rechaza la operación por el índice parcial, traducida a `MIEMBRO_COMITE_DUPLICADO`.

#### Scenario: Violación de restricciones CHECK

- **GIVEN** una fila de miembro con `activo = false` sin `fecha_desvinculacion`, con `activo = true` y `fecha_desvinculacion`, con `tipo` o `rol` inválidos, o con `rol = JEFATURA` y `tipo = EXTERNO`
- **WHEN** se intenta persistir
- **THEN** PostgreSQL rechaza la operación por la restricción `CHECK` correspondiente.

#### Scenario: Recarga fiel del agregado

- **GIVEN** un comité persistido con miembros internos, externos y desvinculados
- **WHEN** se recarga desde la base de datos
- **THEN** conserva `tipo`, `rol`, fechas, `activo`, `Version` y la jefatura.
