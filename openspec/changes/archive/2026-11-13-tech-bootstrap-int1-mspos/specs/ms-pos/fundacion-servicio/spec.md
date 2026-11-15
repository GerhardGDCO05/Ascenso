# Spec Delta: MS-POS Fundación del Servicio y Reglas Base del Dominio

## Purpose

Establecer la fundación arquitectónica hexagonal, el *Shared Kernel* táctico de dominio, el contrato estandarizado de errores y la base de persistencia del microservicio `MS-POS` (`Ascenso.Postulaciones`), habilitando el desarrollo de las historias de admisión y registro de postulantes.

## ADDED Requirements

### Requirement: Aislamiento de la arquitectura hexagonal en MS-POS

La solución `Ascenso.Postulaciones.sln` SHALL estructurarse en cuatro capas con dependencias estrictamente hacia adentro: `Domain` no SHALL referenciar frameworks externos (`EntityFrameworkCore`, `AspNetCore`, `MassTransit`, `Npgsql`, `Firebase`, `Keycloak`), `Application` SHALL depender únicamente de `Domain`, `Infrastructure` SHALL implementar los puertos de `Domain` y `Application`, y `Api` SHALL actuar como único punto de composición.

#### Scenario: Verificación de independencia del dominio y dirección de dependencias

- **GIVEN** los ensamblados compilados de `Ascenso.Postulaciones.Domain`, `Application`, `Infrastructure` y `Api`
- **WHEN** se ejecuta la suite de pruebas de arquitectura
- **THEN** se comprueba que `Domain` no referencia ningún paquete de infraestructura ni capas externas
- **AND** se comprueba que `Application` no referencia `Infrastructure` ni `Api`, y que `Infrastructure` no referencia `Api`

### Requirement: Encapsulamiento estricto y Shared Kernel táctico del dominio

Toda entidad mutable de `MS-POS` SHALL heredar de una abstracción auditable con identificador `UUIDv7` y token de concurrencia optimista (`Version`), todo registro histórico SHALL heredar de una abstracción inmutable *append-only*, y ninguna propiedad en `Ascenso.Postulaciones.Domain` SHALL exponer un `set` público.

#### Scenario: Auditoría automática y bloqueo de setters públicos en entidades

- **GIVEN** las clases de entidad y raíces de agregado definidas en `Ascenso.Postulaciones.Domain`
- **WHEN** se inspeccionan sus propiedades mediante las pruebas de arquitectura y del *Shared Kernel*
- **THEN** ninguna propiedad mutable posee un método `set` público
- **AND** al actualizar la auditoría de una entidad auditable se incrementa `Version` y se registra la marca de tiempo en UTC

#### Scenario: Identificadores generados con UUIDv7 en el dominio

- **GIVEN** una entidad auditable creada a través de su fábrica de dominio
- **WHEN** se inspecciona su identificador
- **THEN** el identificador es un `Guid` con versión 7 generado en el dominio y no por la base de datos

### Requirement: Contrato estandarizado de errores de la API

La API de `MS-POS` SHALL responder errores como `ProblemDetails` en formato JSON: las reglas de negocio SHALL producir el código HTTP declarado por la excepción de dominio junto con la extensión `codigo`, los errores de validación de entrada SHALL producir `400` con el detalle por campo, y las excepciones no controladas SHALL producir `500` sin exponer detalles internos.

#### Scenario: Error de regla de negocio con código estable

- **GIVEN** una solicitud que viola una regla de negocio del dominio
- **WHEN** la API procesa la solicitud
- **THEN** responde con el estado HTTP declarado por la regla y un cuerpo `ProblemDetails` que incluye la extensión `codigo`

#### Scenario: Error de validación de campos de entrada

- **GIVEN** una solicitud con campos que incumplen las validaciones de formato u obligatoriedad
- **WHEN** la API procesa la solicitud
- **THEN** responde `400 Bad Request` con los mensajes agrupados por campo

### Requirement: Unidad de trabajo transaccional y traducción de conflictos de persistencia

La capa de persistencia de `MS-POS` SHALL exponer una unidad de trabajo que confirme los cambios de forma atómica con `CancellationToken`, SHALL traducir los conflictos de concurrencia optimista y las violaciones de unicidad en excepciones de dominio que la API mapea a `409 Conflict`, y no SHALL filtrar excepciones del proveedor de base de datos.

#### Scenario: Conflicto de concurrencia optimista

- **GIVEN** una entidad modificada por dos transacciones concurrentes sobre la misma versión
- **WHEN** la unidad de trabajo intenta confirmar la segunda transacción
- **THEN** se lanza una excepción de dominio de conflicto que la API responde como `409 Conflict`

#### Scenario: Violación de restricción de unicidad

- **GIVEN** un registro que duplica un valor sujeto a una restricción de unicidad
- **WHEN** la unidad de trabajo intenta confirmar los cambios
- **THEN** se lanza una excepción de dominio que la API responde como `409 Conflict` con su `codigo`

### Requirement: Persistencia aislada por esquema y migraciones versionadas

El microservicio `MS-POS` SHALL persistir exclusivamente en el esquema PostgreSQL `postulaciones` con su propio historial de migraciones, SHALL aplicar las configuraciones de mapeo por ensamblado y no SHALL generar identificadores en la base de datos para las entidades cuyo `UUIDv7` se crea en el dominio.

#### Scenario: Aislamiento del esquema y del historial de migraciones

- **GIVEN** el contexto de persistencia de `MS-POS` conectado a PostgreSQL
- **WHEN** se inspecciona el modelo relacional y la tabla de historial de migraciones
- **THEN** todas las tablas del modelo pertenecen al esquema `postulaciones`
- **AND** el historial de migraciones se registra en `__ef_migrations_history` dentro de ese mismo esquema

### Requirement: Verificación automática de reglas arquitectónicas

El repositorio SHALL incluir pruebas de arquitectura ejecutables en CI que fallen ante cualquier violación de independencia del dominio, dirección de dependencias entre capas o exposición de setters públicos, de modo que la compilación y las pruebas del microservicio se validen en cada cambio.

#### Scenario: Detección de una violación de dependencias

- **GIVEN** una referencia de `Ascenso.Postulaciones.Domain` hacia un paquete de infraestructura
- **WHEN** se ejecutan las pruebas de arquitectura
- **THEN** la suite falla indicando la regla de dependencia incumplida
