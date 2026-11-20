# ms-pos/postulantes Specification

## Purpose

Definir el comportamiento del registro y consulta del perfil del postulante en `MS-POS` (`RF-01`, HUs 1.1 y 1.2), incluyendo la identidad autenticada, los datos personales y académicos, el perfil socioeconómico, la composición familiar, la unicidad de la identidad y la validación contra el catálogo académico y el periodo vigente.

## Requirements

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

El sistema `MS-POS` SHALL permitir al postulante autenticado consultar exclusivamente su propio perfil, devolviendo sus datos personales, datos de contacto, datos académicos base, datos socioeconómicos, composición familiar, estado del perfil y fecha de registro.

#### Scenario: Consulta exitosa del perfil propio

- **GIVEN** un usuario autenticado con un perfil de postulante registrado
- **WHEN** solicita consultar su perfil
- **THEN** el sistema responde `200 OK` con los datos del perfil asociado a su identidad
- **AND** no expone perfiles de otros usuarios.

#### Scenario: Perfil propio aún no registrado

- **GIVEN** un usuario autenticado que todavía no posee un perfil de postulante
- **WHEN** solicita consultar su perfil
- **THEN** el sistema responde `404 Not Found` (`POSTULANTE_NO_ENCONTRADO`).

#### Scenario: Consulta de un perfil sin sección socioeconómica

- **GIVEN** un postulante registrado por la HU 1.1 que aún no completó su sección socioeconómica
- **WHEN** consulta su perfil
- **THEN** el sistema responde `200 OK` con `estadoPerfil = INCOMPLETO`, datos socioeconómicos no registrados y lista familiar vacía.

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

### Requirement: Registro del perfil socioeconómico del postulante

El sistema `MS-POS` SHALL permitir al postulante autenticado registrar o reemplazar su perfil socioeconómico con `estadoCivil`, `promedioActual`, `trabaja`, `ocupacion`, `ingresoMensualPostulante` y su composición familiar, conservando los datos personales y académicos ya registrados.

#### Scenario: Registro exitoso del perfil socioeconómico

- **GIVEN** un postulante autenticado con perfil base registrado y sin sección socioeconómica
- **WHEN** envía un perfil socioeconómico completo y válido
- **THEN** el sistema persiste los datos, calcula los totales y responde `200 OK` con el perfil actualizado.

#### Scenario: Actualización idempotente del perfil socioeconómico

- **GIVEN** un postulante autenticado con perfil socioeconómico previamente registrado
- **WHEN** envía una nueva sección socioeconómica completa
- **THEN** el sistema reemplaza la sección anterior, recalcula los totales y conserva los datos personales y académicos.

#### Scenario: Rechazo cuando el perfil del postulante no existe

- **GIVEN** un usuario autenticado que aún no posee un perfil de postulante
- **WHEN** intenta registrar su perfil socioeconómico
- **THEN** el sistema responde `404 Not Found` (`POSTULANTE_NO_ENCONTRADO`) y no persiste datos.

#### Scenario: Rechazo cuando la sección socioeconómica está incompleta

- **GIVEN** una solicitud sin estado civil, promedio, indicador laboral, ocupación o ingreso del postulante
- **WHEN** el sistema valida la solicitud
- **THEN** responde `400 Bad Request` y no modifica el perfil.

### Requirement: Catálogos cerrados de estado civil, parentesco y ocupación

El sistema `MS-POS` SHALL aceptar únicamente los catálogos cerrados: `EstadoCivil` (`SOLTERO`, `CASADO`, `DIVORCIADO`, `VIUDO`), `Parentesco` (`PADRE`, `MADRE`, `HERMANO`, `HIJO`, `PAREJA`, `OTRO`) y `Ocupacion` (`EMPLEADO`, `INDEPENDIENTE`, `ESTUDIANTE`, `LABORES_HOGAR`, `JUBILADO`, `NO_TRABAJA`, `OTRO`).

#### Scenario: Aceptación de valores pertenecientes a los catálogos

- **GIVEN** una solicitud con estado civil, parentescos y ocupaciones incluidos en los catálogos
- **WHEN** el sistema valida la solicitud
- **THEN** acepta los valores y los persiste con sus códigos en mayúsculas.

#### Scenario: Rechazo de un valor fuera de catálogo

- **GIVEN** una solicitud con un estado civil, parentesco u ocupación no reconocido
- **WHEN** el sistema valida la solicitud
- **THEN** responde `422 Unprocessable Entity` con un código estable de catálogo inválido y no modifica el perfil.

#### Scenario: Rechazo de ocupación ausente

- **GIVEN** una solicitud del postulante o de un familiar sin ocupación
- **WHEN** el sistema valida la solicitud
- **THEN** responde `400 Bad Request` y no modifica el perfil.

### Requirement: Registro de la composición familiar

El sistema `MS-POS` SHALL permitir declarar cero o más `MiembroFamiliar` dentro del perfil del postulante, cada uno con `parentesco`, `edad`, `ocupacion` e `ingresoMensual` obligatorios; la lista vacía representa a un postulante que vive solo o es foráneo.

#### Scenario: Registro con lista de familiares vacía

- **GIVEN** un postulante autenticado que vive solo o es foráneo
- **WHEN** envía su perfil socioeconómico con `miembrosFamiliares` vacío
- **THEN** el sistema acepta la declaración, registra cero familiares y completa el perfil.

#### Scenario: Registro con uno o más familiares

- **GIVEN** un postulante autenticado con familiares en su hogar
- **WHEN** envía su perfil socioeconómico con uno o más miembros válidos
- **THEN** el sistema persiste cada familiar asociado al postulante y devuelve la composición registrada.

#### Scenario: Rechazo de un familiar con edad o ingreso inválido

- **GIVEN** un familiar con edad fuera de `0–120` o ingreso negativo
- **WHEN** el sistema valida la solicitud
- **THEN** responde `400 Bad Request` o `422 Unprocessable Entity` y no modifica el perfil.

#### Scenario: Rechazo de un familiar sin ocupación

- **GIVEN** un familiar sin `ocupacion`
- **WHEN** el sistema valida la solicitud
- **THEN** responde `400 Bad Request` y no modifica el perfil.

### Requirement: Cálculo automático de integrantes e ingreso familiar mensual total

El sistema `MS-POS` SHALL calcular `numeroIntegrantesFamilia` como la cantidad de miembros familiares más uno, e `ingresoFamiliarMensualTotal` como la suma del ingreso del postulante y los ingresos de sus familiares, sin aceptar estos valores del cliente.

#### Scenario: Cálculo con lista familiar vacía

- **GIVEN** un postulante sin familiares registrados
- **WHEN** el sistema procesa su perfil socioeconómico
- **THEN** registra `numeroIntegrantesFamilia = 1` e `ingresoFamiliarMensualTotal` igual al ingreso del postulante.

#### Scenario: Cálculo con familiares registrados

- **GIVEN** un postulante con dos familiares registrados
- **WHEN** el sistema procesa su perfil socioeconómico
- **THEN** registra `numeroIntegrantesFamilia = 3` e `ingresoFamiliarMensualTotal` igual a la suma del ingreso del postulante y de ambos familiares.

#### Scenario: Recalculo al actualizar el perfil

- **GIVEN** un postulante con una sección socioeconómica previamente calculada
- **WHEN** actualiza sus ingresos o su composición familiar
- **THEN** el sistema recalcula ambos valores y persiste el nuevo resultado.

#### Scenario: Ignorar totales enviados por el cliente

- **GIVEN** una solicitud que incluye valores de cantidad de integrantes o ingreso total
- **WHEN** el sistema procesa la solicitud
- **THEN** ignora esos valores y usa únicamente los cálculos realizados en el servidor.

### Requirement: Coherencia entre la condición laboral y la ocupación

El sistema `MS-POS` SHALL exigir que `ocupacion` sea coherente con `trabaja`: si `trabaja = true`, solo `EMPLEADO`, `INDEPENDIENTE` u `OTRO`; si `trabaja = false`, solo `ESTUDIANTE`, `LABORES_HOGAR`, `JUBILADO`, `NO_TRABAJA` u `OTRO`. El ingreso no se fuerza a cero.

#### Scenario: Aceptación de una combinación coherente

- **GIVEN** un postulante con `trabaja = true` y ocupación `EMPLEADO`
- **WHEN** el sistema valida la solicitud
- **THEN** acepta la combinación y conserva su ingreso declarado.

#### Scenario: Rechazo de una combinación incoherente

- **GIVEN** un postulante con `trabaja = false` y ocupación `EMPLEADO`
- **WHEN** el sistema valida la solicitud
- **THEN** responde `422 Unprocessable Entity` y no modifica el perfil.

#### Scenario: Aceptación de una persona jubilada con pensión

- **GIVEN** un postulante con `trabaja = false`, ocupación `JUBILADO` e ingreso mayor que cero
- **WHEN** el sistema procesa su perfil socioeconómico
- **THEN** acepta el ingreso declarado y lo suma al total familiar.

### Requirement: Transición del perfil a COMPLETO

El sistema `MS-POS` SHALL transicionar `estadoPerfil` a `COMPLETO` cuando el postulante tenga datos académicos válidos y registre una sección socioeconómica válida, aunque su lista de familiares sea vacía.

#### Scenario: Perfil completado sin familiares

- **GIVEN** un postulante con datos académicos válidos y sin familiares
- **WHEN** registra una sección socioeconómica válida
- **THEN** el sistema cambia `estadoPerfil` a `COMPLETO` y responde con el nuevo estado.

#### Scenario: Perfil completado con familiares

- **GIVEN** un postulante con datos académicos válidos y al menos un familiar
- **WHEN** registra una sección socioeconómica válida
- **THEN** el sistema cambia `estadoPerfil` a `COMPLETO` y conserva la composición familiar.

#### Scenario: Perfil que permanece incompleto ante un error

- **GIVEN** un postulante con perfil `INCOMPLETO`
- **WHEN** intenta registrar una sección socioeconómica inválida
- **THEN** el sistema rechaza la operación y conserva `estadoPerfil = INCOMPLETO`.

#### Scenario: Actualización de un perfil ya completo

- **GIVEN** un postulante con `estadoPerfil = COMPLETO`
- **WHEN** actualiza su sección socioeconómica con datos válidos
- **THEN** el sistema conserva `estadoPerfil = COMPLETO` y actualiza los totales.

### Requirement: Confidencialidad del perfil socioeconómico

El sistema `MS-POS` SHALL exponer los datos socioeconómicos y familiares únicamente al postulante propietario del perfil, sin almacenar nombres de familiares y sin incluirlos en eventos de integración.

#### Scenario: Acceso del propietario a su información

- **GIVEN** un postulante autenticado con perfil socioeconómico registrado
- **WHEN** consulta su propio perfil
- **THEN** el sistema le devuelve sus datos socioeconómicos y familiares sin exponer los de otros postulantes.

#### Scenario: Aislamiento entre postulantes

- **GIVEN** dos postulantes autenticados con perfiles distintos
- **WHEN** uno consulta o actualiza su perfil
- **THEN** el sistema opera exclusivamente sobre el perfil asociado a su identidad.

#### Scenario: Datos familiares sin nombres

- **GIVEN** una composición familiar registrada
- **WHEN** el sistema devuelve o persiste la información
- **THEN** solo incluye parentesco, edad, ocupación e ingreso, sin nombres de familiares.
