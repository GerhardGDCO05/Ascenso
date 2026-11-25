# Spec Delta

## Purpose

Definir el ciclo de vida de una convocatoria de becas en `MS-PRG`: creación en borrador, configuración de criterios y documentos requeridos, publicación, cierre automático y contrato interno de lectura para `MS-POS`.

## ADDED Requirements

### Requirement: Creación de convocatoria en borrador

El sistema `MS-PRG` SHALL permitir crear una `Convocatoria` mediante `POST /api/v1/convocatorias`, normalizando el `codigo` en mayúsculas y sin espacios laterales, y MUST crearla en estado `BORRADOR` con los datos del programa, periodo, fechas, cupos y criterios recibidos.

#### Scenario: Creación exitosa de una convocatoria

- **GIVEN** un usuario autorizado y un programa de becas habilitado con un periodo académico vigente
- **WHEN** envía `POST /api/v1/convocatorias` con código, nombre, fechas, cupos y criterios válidos
- **THEN** el sistema responde `201 Created` con la convocatoria en estado `BORRADOR`
- **AND** el código queda almacenado en mayúsculas y sin espacios laterales.

#### Scenario: Rechazo por código duplicado

- **GIVEN** una convocatoria ya registrada con el mismo `codigo`
- **WHEN** se intenta crear otra convocatoria con ese código
- **THEN** el sistema responde `409 Conflict` con `codigo = CODIGO_CONVOCATORIA_DUPLICADO`
- **AND** no persiste la nueva convocatoria.

### Requirement: Validación del programa de becas y del periodo académico

El sistema `MS-PRG` SHALL exigir que el programa padre exista, esté en estado `CONFIGURADO` o `ACTIVO`, tenga presupuesto asignado mayor que cero, cupos disponibles suficientes y al menos un convenio vigente; además, el periodo académico MUST existir y estar vigente en el calendario institucional.

#### Scenario: Rechazo por programa no habilitado

- **GIVEN** un programa en estado `BORRADOR`, sin presupuesto asignado o sin convenios vigentes
- **WHEN** se intenta crear o publicar una convocatoria para ese programa
- **THEN** el sistema responde `422 Unprocessable Entity` con `codigo = PROGRAMA_NO_HABILITADO`
- **AND** la convocatoria no se crea ni cambia de estado.

#### Scenario: Rechazo por periodo académico no vigente

- **GIVEN** un periodo académico inexistente, inactivo o no vigente
- **WHEN** se intenta crear o publicar una convocatoria asociada a ese periodo
- **THEN** el sistema responde `422 Unprocessable Entity` con `codigo = PERIODO_ACADEMICO_NO_VIGENTE`
- **AND** la convocatoria no se crea ni cambia de estado.

### Requirement: Control de cupos de la convocatoria

El sistema `MS-PRG` SHALL exigir que `cuposAsignados` sea un entero mayor o igual a `1` y menor o igual a los cupos disponibles actuales del programa padre, validando esta condición tanto al guardar el borrador como al publicar.

#### Scenario: Rechazo por cupos superiores a los disponibles

- **GIVEN** un programa con cupos disponibles menores que los `cuposAsignados` solicitados
- **WHEN** se intenta guardar o publicar la convocatoria
- **THEN** el sistema responde `422 Unprocessable Entity` con `codigo = CUPOS_INSUFICIENTES`
- **AND** no modifica la convocatoria ni el programa.

#### Scenario: Revalidación de cupos al publicar

- **GIVEN** una convocatoria en `BORRADOR` cuyos cupos fueron válidos al crearse, pero el programa ya no tiene cupos suficientes
- **WHEN** se ejecuta `POST /api/v1/convocatorias/{id}/publicar`
- **THEN** el sistema responde `422 Unprocessable Entity` con `codigo = CUPOS_INSUFICIENTES`
- **AND** la convocatoria permanece en `BORRADOR`.

### Requirement: Criterios de elegibilidad de la convocatoria

El sistema `MS-PRG` SHALL exigir criterios de elegibilidad con `promedioMinimo` entre `0.00` y `20.00` (dos decimales), edades enteras entre `0` y `120` con `edadMinima <= edadMaxima`, `topeIngresoFamiliar >= 0.00` y listas no vacías de universidades y carreras permitidas que pertenezcan a los convenios vigentes del programa.

#### Scenario: Rechazo por criterios fuera de rango

- **GIVEN** una solicitud con promedio fuera de `[0.00, 20.00]`, edades invertidas o tope de ingreso negativo
- **WHEN** se intenta crear o editar la convocatoria
- **THEN** el sistema responde `400 Bad Request` con un error de validación de criterios
- **AND** no persiste cambios.

#### Scenario: Rechazo por universidad o carrera fuera del convenio

- **GIVEN** una universidad o carrera permitida que no pertenece a los convenios vigentes del programa
- **WHEN** se intenta crear o editar la convocatoria
- **THEN** el sistema responde `422 Unprocessable Entity` con `codigo = CONVENIO_VENCIDO` o `CRITERIO_UNIVERSIDAD_INVALIDA`
- **AND** no persiste cambios.

### Requirement: Cronología y zona horaria de las fechas

El sistema `MS-PRG` SHALL interpretar las fechas en `America/Caracas` (UTC-4), iniciar la apertura a las `00:00:00`, normalizar el cierre y el límite de subsanación a las `23:59:59` y almacenar los instantes en UTC; además, MUST rechazar una cronología inválida.

#### Scenario: Normalización y almacenamiento en UTC

- **GIVEN** una convocatoria con `fechaApertura = 2026-12-01` y `fechaCierre = 2026-12-20`
- **WHEN** se guarda la convocatoria
- **THEN** la apertura se registra a las `00:00:00` de Caracas y el cierre a las `23:59:59` de Caracas
- **AND** ambos instantes se almacenan en UTC.

#### Scenario: Rechazo por cronología inválida

- **GIVEN** una solicitud donde el cierre no es posterior a la apertura, el límite de subsanación es anterior al cierre o las fechas de evaluación no respetan el orden configurado
- **WHEN** se intenta crear o editar la convocatoria
- **THEN** el sistema responde `400 Bad Request` con `codigo = FECHAS_CONVOCATORIA_INVALIDAS`
- **AND** no persiste cambios.

### Requirement: Revisión documental previa y fechas de evaluación

El sistema `MS-PRG` SHALL exigir el indicador booleano `requiereRevisionDocumentalPrevia`, con valor por defecto `true`; cuando sea `true`, el inicio de evaluación MUST ser posterior o igual al cierre; cuando sea `false`, MAY iniciar desde la apertura, y en todos los casos el cierre de evaluación MUST ser posterior al límite de subsanación y al inicio de evaluación.

#### Scenario: Evaluación posterior a la revisión previa

- **GIVEN** una convocatoria con `requiereRevisionDocumentalPrevia = true` e inicio de evaluación anterior al cierre
- **WHEN** se intenta guardar la convocatoria
- **THEN** el sistema responde `400 Bad Request` con `codigo = FECHAS_CONVOCATORIA_INVALIDAS`
- **AND** no persiste cambios.

#### Scenario: Evaluación en paralelo desde la apertura

- **GIVEN** una convocatoria con `requiereRevisionDocumentalPrevia = false`, inicio de evaluación igual o posterior a la apertura y cierre de evaluación posterior al límite de subsanación
- **WHEN** se guarda la convocatoria
- **THEN** el sistema responde `201 Created` o `200 OK` según la operación
- **AND** conserva el indicador en `false`.

### Requirement: Edición restringida al estado borrador

El sistema `MS-PRG` SHALL permitir editar parámetros, criterios y documentos requeridos únicamente mientras la convocatoria esté en `BORRADOR`, y MUST rechazar con `409 Conflict` cualquier edición sobre una convocatoria `PUBLICADA`, `CERRADA` o `CANCELADA`.

#### Scenario: Edición exitosa de un borrador

- **GIVEN** una convocatoria en estado `BORRADOR`
- **WHEN** se envía `PUT /api/v1/convocatorias/{id}` con datos válidos
- **THEN** el sistema responde `200 OK` con los datos actualizados
- **AND** la convocatoria permanece en `BORRADOR`.

#### Scenario: Rechazo de edición de una convocatoria publicada

- **GIVEN** una convocatoria en estado `PUBLICADA`
- **WHEN** se intenta editar sus parámetros o criterios
- **THEN** el sistema responde `409 Conflict` con `codigo = CONVOCATORIA_NO_EDITABLE`
- **AND** no modifica la convocatoria.

### Requirement: Documentos requeridos antes de publicar

El sistema `MS-PRG` SHALL permitir asociar y quitar `DocumentoRequerido` mientras la convocatoria esté en `BORRADOR`, y MUST exigir al menos un documento requerido para ejecutar la publicación.

#### Scenario: Asociación de documento requerido en borrador

- **GIVEN** una convocatoria en estado `BORRADOR`
- **WHEN** se agrega un documento requerido con tipo, nombre y obligatoriedad válidos
- **THEN** el sistema responde `201 Created`
- **AND** el documento queda asociado a la convocatoria.

#### Scenario: Rechazo de publicación sin documentos requeridos

- **GIVEN** una convocatoria en `BORRADOR` sin documentos requeridos
- **WHEN** se ejecuta `POST /api/v1/convocatorias/{id}/publicar`
- **THEN** el sistema responde `422 Unprocessable Entity` con `codigo = CONVOCATORIA_SIN_DOCUMENTOS`
- **AND** la convocatoria permanece en `BORRADOR`.

### Requirement: Publicación de la convocatoria

El sistema `MS-PRG` SHALL permitir publicar una convocatoria solo si está en `BORRADOR`, tiene al menos un documento requerido y su fecha de cierre normalizada es estrictamente posterior al instante actual; al publicar MUST cambiar el estado a `PUBLICADA` y registrar la fecha de publicación.

#### Scenario: Publicación exitosa

- **GIVEN** una convocatoria en `BORRADOR` con criterios, documentos requeridos y fecha de cierre futura
- **WHEN** se ejecuta `POST /api/v1/convocatorias/{id}/publicar`
- **THEN** el sistema responde `200 OK` con estado `PUBLICADA`
- **AND** registra la fecha de publicación y deja programado el cierre automático.

#### Scenario: Rechazo de publicación con fecha de cierre vencida

- **GIVEN** una convocatoria en `BORRADOR` cuya fecha de cierre ya pasó
- **WHEN** se ejecuta `POST /api/v1/convocatorias/{id}/publicar`
- **THEN** el sistema responde `422 Unprocessable Entity` con `codigo = CONVOCATORIA_FECHA_CIERRE_VENCIDA`
- **AND** la convocatoria permanece en `BORRADOR`.

### Requirement: Cierre automático de convocatorias publicadas

El sistema `MS-PRG` SHALL cerrar automáticamente toda convocatoria `PUBLICADA` cuando se alcance la hora de cierre normalizada, cambiando su estado a `CERRADA` sin intervención manual y sin afectar convocatorias ya cerradas o canceladas.

#### Scenario: Cierre automático al vencer la fecha

- **GIVEN** una convocatoria `PUBLICADA` cuya fecha de cierre ya fue alcanzada
- **WHEN** se ejecuta el proceso automático de cierre
- **THEN** la convocatoria pasa a estado `CERRADA`
- **AND** se registra el evento de cierre para los consumidores.

#### Scenario: Convocatoria aún vigente no se cierra

- **GIVEN** una convocatoria `PUBLICADA` cuya fecha de cierre es futura
- **WHEN** se ejecuta el proceso automático de cierre
- **THEN** la convocatoria permanece en `PUBLICADA`.

### Requirement: Consulta de convocatorias y contrato interno

El sistema `MS-PRG` SHALL exponer consultas paginadas y el detalle de una convocatoria para usuarios autorizados, y MUST exponer un contrato interno de solo lectura para que `MS-POS` consulte una convocatoria por identificador y las convocatorias abiertas con sus fechas, cupos, criterios y documentos requeridos.

#### Scenario: Consulta paginada de convocatorias

- **GIVEN** convocatorias registradas con distintos estados y programas
- **WHEN** se consulta `GET /api/v1/convocatorias` con filtros opcionales
- **THEN** el sistema responde `200 OK` con la página solicitada.

#### Scenario: Consulta interna para MS-POS

- **GIVEN** una convocatoria publicada y vigente
- **WHEN** `MS-POS` consulta `GET /internal/v1/convocatorias/{id}` o `GET /internal/v1/convocatorias/abiertas`
- **THEN** el sistema responde `200 OK` con el contrato de lectura acordado
- **AND** no expone datos de otros programas fuera del alcance solicitado.

### Requirement: Autorización basada en permisos para convocatorias

El sistema `MS-PRG` SHALL autorizar cada operación de convocatorias contra el claim `permissions` del JWT, exigiendo `convocatorias:crear`, `convocatorias:leer`, `convocatorias:editar` o `convocatorias:publicar` según la acción, y MUST responder `401 Unauthorized` sin token válido y `403 Forbidden` con `codigo = PERMISO_DENEGADO` cuando falte el permiso.

#### Scenario: Acceso permitido con el permiso requerido

- **GIVEN** un token válido cuyo claim `permissions` contiene `convocatorias:publicar`
- **WHEN** el usuario ejecuta `POST /api/v1/convocatorias/{id}/publicar`
- **THEN** el sistema procesa la publicación.

#### Scenario: Acceso denegado por permiso ausente

- **GIVEN** un token válido sin el permiso `convocatorias:publicar`
- **WHEN** el usuario intenta publicar una convocatoria
- **THEN** el sistema responde `403 Forbidden` con `codigo = PERMISO_DENEGADO`
- **AND** no modifica la convocatoria.

### Requirement: Integridad de la convocatoria en base de datos

El sistema `MS-PRG` SHALL garantizar en PostgreSQL que `codigo` sea obligatorio y único, que `cupos_asignados >= 1`, que `edad_minima <= edad_maxima`, que `promedio_minimo` esté entre `0` y `20` y que `fecha_cierre > fecha_apertura`, independientemente de las validaciones de aplicación.

#### Scenario: Rechazo de código duplicado a nivel de persistencia

- **GIVEN** una convocatoria existente con un código determinado
- **WHEN** una escritura intenta persistir el mismo código
- **THEN** PostgreSQL rechaza la operación por el índice único
- **AND** el sistema responde `409 Conflict` con `codigo = CODIGO_CONVOCATORIA_DUPLICADO`.

#### Scenario: Rechazo de datos que violan una restricción

- **GIVEN** una escritura con cupos menores que `1`, edades invertidas, promedio fuera de rango o cierre anterior a la apertura
- **WHEN** se intenta persistir directamente en la base de datos
- **THEN** PostgreSQL rechaza la escritura por la restricción correspondiente.
