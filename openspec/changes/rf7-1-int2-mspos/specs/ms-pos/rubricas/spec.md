# Spec Delta: Rúbricas de Evaluación (`ms-pos/rubricas`)

## Purpose

Permite al Coordinador de programa definir la rúbrica de evaluación de su programa, con criterios ponderados y rango de calificación, como instrumento de medición cuantitativo que se publica al sumar exactamente 100.00%.

## ADDED Requirements

### Requirement: Creación de la rúbrica de evaluación en borrador

El sistema `MS-POS` SHALL permitir al Coordinador de programa crear una `RubricaEvaluacion` con `programaBecaId`, `convocatoriaId` y `nombre` (obligatorio, máximo 200 caracteres), que nace en estado `BORRADOR` sin criterios, asociada al ciclo de postulación indicado. Solo puede existir una rúbrica por combinación de programa y convocatoria.

#### Scenario: Creación exitosa

- **GIVEN** un usuario con rol `COORDINADOR_PROGRAMA`
- **WHEN** envía programa, convocatoria y nombre válidos
- **THEN** el sistema responde `201 Created` con la rúbrica en estado `BORRADOR`, sin criterios y con suma de pesos `0.00`.

#### Scenario: Rechazo por datos obligatorios inválidos

- **GIVEN** una solicitud con nombre vacío, nombre de más de 200 caracteres o identificadores vacíos
- **WHEN** el sistema valida la solicitud
- **THEN** responde `400 Bad Request` y no persiste la rúbrica.

#### Scenario: Rechazo de rúbrica duplicada

- **GIVEN** una rúbrica existente para el mismo programa y convocatoria
- **WHEN** el coordinador intenta crear otra
- **THEN** el sistema responde `409 Conflict` con `codigo = RUBRICA_DUPLICADA`.

### Requirement: Gestión de criterios de evaluación

El sistema `MS-POS` SHALL permitir agregar, editar y eliminar `CriterioEvaluacion` en una rúbrica en `BORRADOR`. Cada criterio tiene `nombre` obligatorio (máximo 200 caracteres), `descripcion` obligatoria (máximo 1000 caracteres), `pesoPorcentual` decimal (`(0, 100.00]`, máx. 2 dec.), `puntajeMinimo` y `puntajeMaximo` decimales (`[0.00, 1000.00]`, máx. 2 dec., `puntajeMinimo < puntajeMaximo`) y `orden` asignado por el sistema.

#### Scenario: Agregar un criterio válido

- **GIVEN** una rúbrica en `BORRADOR`
- **WHEN** el coordinador agrega un criterio con nombre, descripción, peso `30.50` y rango `0` a `10`
- **THEN** el sistema responde `201 Created` con la rúbrica actualizada, el criterio con su `orden` y la suma de pesos recalculada.

#### Scenario: Editar un criterio

- **GIVEN** una rúbrica en `BORRADOR` con un criterio
- **WHEN** el coordinador modifica su nombre, descripción, peso o rango con datos válidos
- **THEN** el sistema responde `200 OK` con la rúbrica actualizada y la suma de pesos recalculada.

#### Scenario: Eliminar un criterio

- **GIVEN** una rúbrica en `BORRADOR` con criterios
- **WHEN** el coordinador elimina uno
- **THEN** el sistema responde `200 OK` con la rúbrica sin ese criterio y la suma de pesos recalculada.

#### Scenario: Rechazo de peso fuera del rango permitido

- **GIVEN** un criterio con peso `0`, negativo, mayor que `100` o con más de 2 decimales
- **WHEN** el sistema valida la solicitud
- **THEN** responde `400 Bad Request` y no modifica la rúbrica.

#### Scenario: Rechazo de rango de puntaje inválido

- **GIVEN** un criterio con `puntajeMinimo` o `puntajeMaximo` negativo, mayor que `1000.00`, con más de 2 decimales, o con `puntajeMinimo >= puntajeMaximo`
- **WHEN** el sistema valida la solicitud
- **THEN** responde `400 Bad Request` y no modifica la rúbrica.

#### Scenario: Rechazo de nombre vacío

- **GIVEN** un criterio sin nombre o solo con espacios
- **WHEN** el sistema valida la solicitud
- **THEN** responde `400 Bad Request` y no modifica la rúbrica.

#### Scenario: Rechazo por descripción vacía o longitudes máximas excedidas

- **GIVEN** un criterio con `descripcion` vacía o solo con espacios, `descripcion` de más de 1000 caracteres, o `nombre` de más de 200 caracteres
- **WHEN** el sistema valida la solicitud
- **THEN** responde `400 Bad Request` y no modifica la rúbrica.

#### Scenario: Rúbrica o criterio inexistente

- **GIVEN** un identificador de rúbrica o de criterio que no existe
- **WHEN** el coordinador intenta consultarlos, editarlos, eliminarlos o publicar
- **THEN** el sistema responde `404 Not Found`.

### Requirement: Cálculo de la suma de pesos

El sistema `MS-POS` SHALL calcular la suma de `pesoPorcentual` de los criterios de la rúbrica con precisión decimal exacta de 2 decimales y exponerla en la consulta junto con `diferenciaPara100` (`100.00 - sumaPesos`, positiva cuando faltan puntos y negativa cuando sobran).

#### Scenario: Suma con decimales

- **GIVEN** una rúbrica con criterios de pesos `33.33`, `33.33` y `33.34`
- **WHEN** se consulta la rúbrica
- **THEN** la suma de pesos es exactamente `100.00` y la diferencia es `0.00`.

#### Scenario: Rúbrica incompleta

- **GIVEN** una rúbrica con criterios de pesos `60.00` y `30.50`
- **WHEN** se consulta la rúbrica
- **THEN** la suma es `90.50` y `diferenciaPara100` es `9.50` (positivo).

### Requirement: Publicación de la rúbrica

El sistema `MS-POS` SHALL publicar una rúbrica únicamente cuando la suma de pesos sea exactamente `100.00`. Al publicar, el estado cambia a `PUBLICADA` y se registra la fecha de publicación. La rúbrica permanece en `BORRADOR` mientras los pesos no sumen `100.00`.

#### Scenario: Publicación exitosa

- **GIVEN** una rúbrica en `BORRADOR` cuyos pesos suman `100.00`
- **WHEN** el coordinador la publica
- **THEN** el sistema responde `200 OK` con estado `PUBLICADA` y fecha de publicación, y la rúbrica queda asociada a la convocatoria del programa.

#### Scenario: Rechazo por suma menor que 100.00 (faltante)

- **GIVEN** una rúbrica en `BORRADOR` cuyos pesos suman `99.99` (`diferenciaPara100 = 0.01`, positivo)
- **WHEN** el coordinador intenta publicarla
- **THEN** el sistema responde `422 Unprocessable Entity` con `codigo = PESOS_INVALIDOS`, el detalle indica `"faltan 0.01"` y la rúbrica permanece en `BORRADOR`.

#### Scenario: Rechazo por suma mayor que 100.00 (exceso)

- **GIVEN** una rúbrica en `BORRADOR` cuyos pesos suman `100.01` (`diferenciaPara100 = -0.01`, negativo)
- **WHEN** el coordinador intenta publicarla
- **THEN** el sistema responde `422 Unprocessable Entity` con `codigo = PESOS_INVALIDOS`, el detalle indica `"excede por 0.01"` y la rúbrica permanece en `BORRADOR`.

#### Scenario: Rechazo de publicación sin criterios

- **GIVEN** una rúbrica en `BORRADOR` sin criterios (`diferenciaPara100 = 100.00`, positivo)
- **WHEN** el coordinador intenta publicarla
- **THEN** el sistema responde `422 Unprocessable Entity` con `codigo = PESOS_INVALIDOS` indicando `"faltan 100.00"`.

### Requirement: Inmutabilidad de la rúbrica publicada o bloqueada

El sistema `MS-POS` SHALL rechazar con `409 Conflict` y `codigo = RUBRICA_BLOQUEADA` cualquier intento de agregar, editar o eliminar criterios, o de publicar, una rúbrica en estado `PUBLICADA` o `BLOQUEADA`.

#### Scenario: Modificación de una rúbrica publicada

- **GIVEN** una rúbrica en estado `PUBLICADA`
- **WHEN** el coordinador intenta agregar, editar o eliminar un criterio
- **THEN** el sistema responde `409 Conflict` con `codigo = RUBRICA_BLOQUEADA` y no modifica la rúbrica.

#### Scenario: Nueva publicación de una rúbrica publicada o bloqueada

- **GIVEN** una rúbrica en estado `PUBLICADA` o `BLOQUEADA`
- **WHEN** el coordinador intenta publicarla
- **THEN** el sistema responde `409 Conflict` con `codigo = RUBRICA_BLOQUEADA`.

### Requirement: Autorización por rol sobre las rúbricas

El sistema `MS-POS` SHALL exigir la política de escritura `CoordinadorPrograma` (rol `COORDINADOR_PROGRAMA` en el claim `roles`) para crear, editar, eliminar criterios y publicar rúbricas, y una política de lectura independiente (`LecturaRubrica`, que hoy solo permite `COORDINADOR_PROGRAMA`) para consultarlas. Cualquier otro rol SHALL recibir `403 Forbidden` con `ProblemDetails` y `codigo = PERMISO_DENEGADO`, sin alterar los endpoints de Postulantes.

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
- **WHEN** intenta crear, editar, eliminar criterios, publicar o consultar una rúbrica
- **THEN** el sistema responde `403 Forbidden` con `codigo = PERMISO_DENEGADO` y no ejecuta la operación.

#### Scenario: Usuario sin autenticar

- **GIVEN** una solicitud sin token válido
- **WHEN** invoca cualquier operación de rúbricas
- **THEN** el sistema responde `401 Unauthorized`.

### Requirement: Consulta de la rúbrica

El sistema `MS-POS` SHALL devolver la rúbrica con `id`, `programaBecaId`, `convocatoriaId`, `nombre`, `estado`, `fechaPublicacion`, la lista de criterios ordenada por `orden`, la suma de pesos y la diferencia respecto de `100.00`.

#### Scenario: Consulta exitosa

- **GIVEN** una rúbrica existente
- **WHEN** el coordinador la consulta por identificador
- **THEN** el sistema responde `200 OK` con todos sus datos, criterios, suma de pesos y diferencia.
