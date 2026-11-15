# Spec Delta: Registro y Perfil Base del Postulante (`ms-pos/postulantes`)

## Purpose

Definir el comportamiento del registro y consulta del perfil base del postulante en `MS-POS` (`RF-01`, HU 1.1), incluyendo la identidad autenticada, los datos personales y académicos mínimos, la unicidad de la identidad y la validación contra el catálogo académico y el periodo vigente.

## ADDED Requirements

### Requirement: Registro del perfil base del postulante

El sistema `MS-POS` SHALL permitir crear el perfil de un postulante con identificación, nombres, apellidos, datos de contacto, universidad aliada, carrera, periodo académico, condición académica (`ADMITIDO` o `MATRICULADO`) y aceptación del tratamiento de datos, naciendo en estado `INCOMPLETO`.

#### Scenario: Registro exitoso del perfil base

- **GIVEN** un usuario autenticado sin perfil de postulante y con datos personales, de contacto y académicos completos y válidos
- **WHEN** el sistema procesa el registro del postulante
- **THEN** crea el perfil asociado a dicho usuario con `estadoPerfil = INCOMPLETO`
- **AND** registra la fecha de registro y responde `201 Created` con el perfil creado.

#### Scenario: Rechazo por datos personales incompletos o tratamiento de datos no aceptado

- **GIVEN** una solicitud de registro con identificación, nombres, apellidos o contacto inválidos, o sin aceptación del tratamiento de datos
- **WHEN** el sistema valida la solicitud
- **THEN** rechaza la operación con `400 Bad Request` por campos inválidos o `422 Unprocessable Entity` por tratamiento de datos no aceptado
- **AND** no crea ningún perfil.

### Requirement: Unicidad de la identidad y del perfil por cuenta

El sistema `MS-POS` SHALL garantizar que un `numeroDocumentoIdentidad` se registre una sola vez y que cada usuario autenticado posea como máximo un perfil de postulante, rechazando duplicados con `409 Conflict` y un código estable.

#### Scenario: Rechazo por identificación ya registrada

- **GIVEN** un `numeroDocumentoIdentidad` que ya pertenece a otro perfil de postulante
- **WHEN** se intenta registrar un nuevo perfil con esa identificación
- **THEN** el sistema rechaza la operación con `409 Conflict` (`IDENTIDAD_DUPLICADA`)
- **AND** no persiste el nuevo perfil.

#### Scenario: Rechazo por segundo perfil para el mismo usuario

- **GIVEN** un usuario autenticado que ya posee un perfil de postulante
- **WHEN** intenta registrar otro perfil
- **THEN** el sistema rechaza la operación con `409 Conflict` (`PERFIL_YA_REGISTRADO`)
- **AND** conserva intacto el perfil existente.

### Requirement: Validación de la universidad y carrera aliada

El sistema `MS-POS` SHALL validar que la universidad informada exista, esté activa y tenga convenio vigente, y que la carrera pertenezca a esa universidad y esté activa, antes de crear el perfil; si el catálogo académico no está disponible, MUST fallar cerrado.

#### Scenario: Rechazo por universidad o carrera no elegible

- **GIVEN** una universidad inexistente, inactiva o sin convenio vigente, o una carrera inactiva o ajena a la universidad informada
- **WHEN** el sistema valida los datos académicos del registro
- **THEN** rechaza la operación con `422 Unprocessable Entity` (`UNIVERSIDAD_NO_ELEGIBLE` o `CARRERA_NO_ELEGIBLE`)
- **AND** no crea el perfil.

#### Scenario: Falla cerrada cuando el catálogo académico no responde

- **GIVEN** que el servicio de catálogo académico no responde o devuelve un error
- **WHEN** el sistema intenta validar la universidad y la carrera
- **THEN** rechaza la operación con `503 Service Unavailable` (`DEPENDENCIA_NO_DISPONIBLE`)
- **AND** no crea el perfil ni asume que los datos académicos son válidos.

### Requirement: Validación del periodo académico vigente

El sistema `MS-POS` SHALL verificar que el periodo académico informado corresponda al periodo vigente antes de crear el perfil, y MUST fallar cerrado si la dependencia de periodos académicos no está disponible.

#### Scenario: Rechazo por periodo académico no vigente

- **GIVEN** un periodo académico informado que no corresponde al periodo vigente del calendario institucional
- **WHEN** el sistema valida el periodo durante el registro
- **THEN** rechaza la operación con `422 Unprocessable Entity` (`PERIODO_ACADEMICO_INVALIDO`)
- **AND** no crea el perfil.

#### Scenario: Falla cerrada cuando el calendario académico no responde

- **GIVEN** que el servicio de periodos académicos no responde o devuelve un error
- **WHEN** el sistema intenta validar el periodo académico
- **THEN** rechaza la operación con `503 Service Unavailable` (`DEPENDENCIA_NO_DISPONIBLE`)
- **AND** no crea el perfil.

### Requirement: Consulta del perfil propio

El sistema `MS-POS` SHALL permitir al postulante autenticado consultar exclusivamente su propio perfil, devolviendo sus datos personales, datos de contacto, datos académicos base, estado del perfil y fecha de registro.

#### Scenario: Consulta exitosa del perfil propio

- **GIVEN** un usuario autenticado con un perfil de postulante registrado
- **WHEN** solicita consultar su perfil
- **THEN** el sistema responde `200 OK` con los datos del perfil asociado a su identidad
- **AND** no expone perfiles de otros usuarios.

#### Scenario: Perfil propio aún no registrado

- **GIVEN** un usuario autenticado que todavía no posee un perfil de postulante
- **WHEN** solicita consultar su perfil
- **THEN** el sistema responde `404 Not Found` (`POSTULANTE_NO_ENCONTRADO`).

### Requirement: Identidad autenticada y trazabilidad del registro

El sistema `MS-POS` SHALL asociar cada perfil al sujeto autenticado del token de Keycloak y MUST rechazar con `401 Unauthorized` toda operación sin un token válido, sin aceptar identificadores de usuario provistos por el cliente.

#### Scenario: Rechazo de operación sin token válido

- **GIVEN** una solicitud de registro o consulta sin token de acceso válido
- **WHEN** el sistema procesa la solicitud
- **THEN** responde `401 Unauthorized` (`NO_AUTENTICADO`)
- **AND** no crea ni expone ningún perfil.

#### Scenario: El perfil pertenece al sujeto autenticado

- **GIVEN** un token válido cuyo sujeto no coincide con ningún identificador de usuario enviado por el cliente
- **WHEN** el sistema registra el perfil
- **THEN** asocia el perfil al sujeto del token
- **AND** ignora cualquier identificador de usuario incluido en la solicitud.
