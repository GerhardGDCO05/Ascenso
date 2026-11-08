## Purpose

Establece una base ejecutable, segura y observable para el microservicio `MS-BEC`, de modo que los equipos puedan desplegarlo localmente, integrarlo con los demás microservicios y construir sobre él los flujos de otorgamiento, desembolsos y seguimiento académico sin duplicar infraestructura transversal.

## ADDED Requirements

### Requirement: Arranque y verificación de salud del servicio

El microservicio SHALL iniciar como una API .NET contenedorizada y exponer endpoints de salud separados para *liveness* y *readiness*. La verificación de *readiness* SHALL comprobar la conectividad con PostgreSQL y RabbitMQ, y SHALL fallar mientras una dependencia obligatoria no esté disponible.

#### Scenario: Servicio sano

- **WHEN** la API arranca con PostgreSQL y RabbitMQ disponibles y las migraciones aplicadas
- **THEN** el endpoint de *liveness* responde `200 Healthy`
- **AND** el endpoint de *readiness* responde `200 Healthy`

#### Scenario: Dependencia obligatoria caída

- **WHEN** PostgreSQL o RabbitMQ no están disponibles
- **THEN** el endpoint de *readiness* responde `503 Unhealthy` indicando la dependencia afectada
- **AND** el servicio no recibe tráfico del API Gateway

### Requirement: Aislamiento de la arquitectura hexagonal

La solución SHALL mantener cuatro capas con dependencias unidireccionales: `Domain` no SHALL referenciar Entity Framework Core, ASP.NET Core, MassTransit, Firebase ni Keycloak; `Application` SHALL depender solo de `Domain`; `Infrastructure` SHALL implementar los puertos definidos en `Domain` y `Application`; y `API` SHALL ser el único punto de composición y exposición HTTP.

#### Scenario: Verificación automática de dependencias

- **WHEN** se ejecutan las pruebas de arquitectura en el pipeline
- **THEN** cualquier referencia de `Domain` hacia un framework de infraestructura hace fallar la compilación o la prueba
- **AND** cualquier dependencia de `Application` hacia `Infrastructure` o `API` hace fallar la prueba

### Requirement: Persistencia aislada y migraciones reproducibles

El servicio SHALL persistir sus datos exclusivamente en la base PostgreSQL `ascenso_becas`, sin claves foráneas físicas hacia bases de otros microservicios. Toda entidad mutable SHALL registrar identificadores `UUIDv7`, marcas de tiempo UTC con `DateTimeOffset`, auditoría de creación/modificación y un token de concurrencia optimista. Las migraciones SHALL poder aplicarse desde cero y de forma idempotente en un entorno limpio.

#### Scenario: Base de datos vacía

- **WHEN** se ejecuta la migración inicial sobre una base `ascenso_becas` vacía
- **THEN** se crean las tablas del esquema `becas`, los índices y las tablas de Outbox/Inbox y Hangfire
- **AND** una segunda ejecución no produce errores ni duplica estructuras

#### Scenario: Conflicto de concurrencia optimista

- **WHEN** dos operaciones intentan modificar la misma fila con la misma versión
- **THEN** la segunda operación recibe `409 CONCURRENCY_CONFLICT`
- **AND** no se sobrescriben los cambios de la primera

### Requirement: Seguridad perimetral con Keycloak

Los endpoints públicos SHALL validar el JWT emitido por Keycloak, incluyendo la vigencia, el emisor, la audiencia y el segundo factor cuando el rol lo exija. Los endpoints internos bajo `/internal/v1/...` SHALL exigir un *Service Token* de Keycloak, SHALL estar fuera del API Gateway público y SHALL rechazar peticiones sin autorización o con alcance insuficiente.

#### Scenario: Token válido con 2FA completado

- **WHEN** un coordinador con rol `COORDINADOR_PROGRAMA` presenta un JWT válido con el segundo factor completado
- **THEN** la API autoriza la operación conforme a su rol y alcance de programa

#### Scenario: Token sin segundo factor

- **WHEN** un usuario con rol que exige 2FA presenta un JWT sin el claim de segundo factor
- **THEN** la API responde `401 SEGUNDO_FACTOR_REQUERIDO`

#### Scenario: Llamada interna sin token de servicio

- **WHEN** un cliente intenta invocar `/internal/v1/...` sin un *Service Token* válido
- **THEN** la API responde `401` y no ejecuta la operación

### Requirement: Idempotencia de comandos y llamadas internas

Todo comando HTTP que cree o modifique estado SHALL aceptar el encabezado `Idempotency-Key`. Si la misma clave se repite con el mismo payload, el servicio SHALL devolver el resultado original sin duplicar efectos; si se repite con un payload distinto, SHALL responder `409 IDEMPOTENCY_KEY_REUTILIZADA`. Las llamadas síncronas salientes a otros microservicios SHALL tener un tiempo máximo de 3 segundos y SHALL aplicar política *fail-closed*.

#### Scenario: Reintento con la misma clave

- **WHEN** un cliente reenvía una solicitud con la misma `Idempotency-Key` y el mismo payload
- **THEN** el servicio devuelve la misma respuesta que la primera ejecución
- **AND** no crea un segundo agregado ni publica un segundo evento

#### Scenario: Timeout de un servicio dependiente

- **WHEN** `MS-POS` o `MS-PRG` no responde dentro de 3 segundos durante una validación bloqueante
- **THEN** la operación se rechaza con `503 DEPENDENCIA_NO_DISPONIBLE`
- **AND** no se persiste un estado parcial

### Requirement: Mensajería confiable con Outbox e Inbox

Los eventos de integración SHALL registrarse en la misma transacción local que el cambio de estado y publicarse de forma asíncrona con garantía *at-least-once*. Todo consumidor SHALL aplicar idempotencia por `eventId` mediante *Idempotent Inbox*. El sobre del evento SHALL incluir `eventId`, `occurredAt`, `correlationId`, `producer`, `aggregateId` y `aggregateVersion`, y no SHALL exponer datos personales sensibles.

#### Scenario: Caída de RabbitMQ tras confirmar la transacción

- **WHEN** la transacción local confirma un cambio de estado pero RabbitMQ no está disponible
- **THEN** el evento permanece en Outbox y se publica cuando el broker se recupera
- **AND** el cambio de estado no se revierte

#### Scenario: Entrega duplicada de un evento

- **WHEN** el mismo `eventId` se recibe dos veces
- **THEN** el consumidor procesa el evento una sola vez y registra el `eventId` en Inbox
- **AND** la segunda entrega se confirma sin repetir efectos de negocio

### Requirement: Trabajos en segundo plano persistentes

Los temporizadores críticos SHALL ejecutarse en Hangfire con almacenamiento en PostgreSQL y cola `critical`, de modo que sobrevivan a reinicios. Antes de ejecutar una acción, cada job SHALL releer el estado actual de la base de datos y SHALL ser idempotente.

#### Scenario: Reinicio del contenedor durante un temporizador

- **WHEN** el servicio se reinicia mientras existe una oferta pendiente de expiración
- **THEN** Hangfire conserva el job programado y lo ejecuta después del reinicio
- **AND** el job no expira una oferta que ya fue aceptada, rechazada o anulada

### Requirement: Observabilidad y protección de datos en trazas

Cada solicitud SHALL propagar un `correlationId` y registrarlo en logs estructurados, trazas y eventos de integración. Los logs y trazas NO SHALL contener números de cuenta, códigos OTP, documentos de identidad ni datos socioeconómicos. El servicio SHALL exponer métricas de latencia, errores, jobs y estado del Outbox.

#### Scenario: Petición con correlationId entrante

- **WHEN** una petición llega con el encabezado `X-Correlation-Id`
- **THEN** todos los logs y eventos generados durante la petición conservan ese identificador

#### Scenario: Intento de registrar datos sensibles

- **WHEN** una operación incluye un código OTP o un número de cuenta
- **THEN** los logs y trazas los enmascaran o los omiten por completo
