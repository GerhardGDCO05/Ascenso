## Context

`MS-BEC` parte de cero: el repositorio solo contiene la especificación maestra `docs/Explicacion_del_DDD_V7.md`. El diseño debe hacer convivir la máquina de estados cerrada del PRD, las invariantes monetarias y de no doble beneficio, y la integración con `MS-POS`, `MS-PRG`, `MS-PLT` y `MS-AUD`, sin introducir transacciones distribuidas. El alcance de este cambio es la fundación del servicio y la primera rebanada vertical de otorgamiento; los desembolsos, el seguimiento académico, la renovación y la revocación definitiva se construirán sobre esta base en cambios posteriores.

Restricciones que condicionan el diseño:

- Arquitectura hexagonal obligatoria con dependencias unidireccionales.
- PostgreSQL `ascenso_becas` como única base de escritura, sin claves foráneas hacia otros microservicios.
- `UUIDv7` generados en aplicación, `Decimal(18,2)` para dinero, `DateOnly` para fechas de negocio y `DateTimeOffset` UTC.
- Keycloak como IdP único, con 2FA para roles administrativos y *Service Tokens* para `/internal/v1/...`.
- MassTransit + RabbitMQ con *Transactional Outbox* e *Idempotent Inbox*.
- Hangfire persistente para temporizadores críticos.
- Firebase Storage solo para binarios; PostgreSQL guarda URI, hash, tamaño y MIME.

## Goals / Non-Goals

**Goals:**

- Dejar una solución .NET ejecutable, testeable y desplegable en local, con las cuatro capas y sus reglas de dependencia verificadas automáticamente.
- Implementar el agregado `Beca` y su primer ciclo de otorgamiento con transiciones, snapshots, auditoría local, idempotencia y bloqueo por estudiante.
- Definir contratos explícitos para las integraciones síncronas y eventos, de forma que los equipos de `MS-POS`, `MS-PRG` y `MS-PLT` puedan integrarse sin leer el código de `MS-BEC`.
- Demostrar el patrón Outbox/Inbox, la expiración con Hangfire y la seguridad Keycloak con pruebas automatizadas.

**Non-Goals:**

- No se implementan `CalendarioDesembolso`, `Desembolso`, `DatosPagoBecario`, `IncidenciaDesembolso` ni `SuspensionDesembolso`.
- No se implementan `ReporteAcademico`, `EvaluacionPermanencia`, `SolicitudRenovacion` ni `PausaAplazamiento`.
- No se implementa la revocación definitiva con OTP; solo se deja el puerto `IOtpClient` y su adaptador probado.
- No se implementan reportes CQRS, exportaciones ni el panel de dirección.
- No se define la interfaz visual; el API expone DTOs y OpenAPI.

## Decisions

### 1. Solución, capas y reglas de dependencia

Se crea `Ascenso.Becas.sln` con esta estructura:

```text
Apps/services/ms-becas/
  Ascenso.Becas.sln
  Ascenso.Becas.Domain/
  Ascenso.Becas.Application/
  Ascenso.Becas.Infrastructure/
  Ascenso.Becas.Api/
  tests/
    Ascenso.Becas.Domain.Tests/
    Ascenso.Becas.Application.Tests/
    Ascenso.Becas.ArchitectureTests/
    Ascenso.Becas.IntegrationTests/
```

`Domain` contiene agregados, entidades, objetos de valor, enumeraciones, eventos de dominio, excepciones y puertos de repositorio. No referencia ningún framework. `Application` contiene comandos, consultas, handlers, DTOs, validadores, puertos de servicios de aplicación y la traducción de eventos de dominio a eventos de integración. `Infrastructure` implementa persistencia, mensajería, clientes HTTP, almacenamiento, seguridad y jobs. `API` compone el contenedor, configura autenticación, middleware y controladores.

Se usa un despachador propio (`ICommandDispatcher`/`IQueryDispatcher`) en lugar de MediatR: evita dependencias con licenciamiento comercial y mantiene el flujo explícito. Las pruebas de arquitectura usan NetArchTest y se ejecutan en CI.

**Alternativa descartada:** un único proyecto con carpetas por capa. Se descarta porque no permite verificar la inversión de dependencias ni aislar el dominio.

### 2. Plataforma y gestión de dependencias

- .NET 10 LTS (`net10.0`), C# 14, `Nullable=enable`, `ImplicitUsings=enable`, advertencias como errores y análisis de código en build.
- `global.json` fija la banda del SDK y `Directory.Packages.props` centraliza versiones. Se usa `packages.lock.json` y `RestoreLockedMode` en CI para builds reproducibles.
- EF Core 10 con `Npgsql.EntityFrameworkCore.PostgreSQL`; MassTransit con transporte RabbitMQ y Outbox/Inbox EF Core; Hangfire con `Hangfire.PostgreSql`; FluentValidation; OpenTelemetry + Serilog; `PdfSharpCore` (MIT) para la carta; `Google.Cloud.Storage.V1` para Firebase.
- Antes de fijar versiones se verifica la licencia de MassTransit (la línea 8.x es Apache 2.0) y de PdfSharpCore (MIT); no se adopta una línea con licenciamiento comercial sin aprobación del equipo.
- Pruebas: xUnit, FluentAssertions, NSubstitute, Testcontainers para PostgreSQL/RabbitMQ, WireMock.Net para clientes HTTP y `MassTransit.Testing` para consumidores.

**Alternativa descartada:** .NET 8 LTS. Se descarta porque su soporte termina en noviembre de 2026 y el proyecto inicia en esa fecha; .NET 10 LTS ofrece `Guid.CreateVersion7()` nativo y soporte extendido.

### 3. Modelo de dominio y máquina de estados

El agregado `Beca` es la raíz de consistencia transaccional e incluye `OfertaBeca`, `CompromisoBecario` e `HistorialEstadoBeca`. `OfertaBeca` es una entidad con ciclo propio, pero sus transiciones se ejecutan siempre a través de métodos de `Beca`, porque afectan el estado de la beca y la reserva de presupuesto. `CompromisoBecario` también se modifica a través de la raíz.

Reglas de implementación:

- Ninguna propiedad `Estado*` tiene `set` público. Los estados se cambian con métodos de dominio que validan la tabla de transiciones y lanzan `TransicionEstadoInvalidaException` si la dupla no está permitida.
- Toda transición con justificación obligatoria exige `Trim().Length >= 10`. La justificación se persiste en `HistorialEstadoBeca` y en el campo de motivo correspondiente.
- Los estados terminales (`RENOVADA`, `CONCLUIDA`, `REVOCADA` para `Beca`; `ACEPTADA`, `RECHAZADA_POR_POSTULANTE`, `EXPIRADA`, `ANULADA_POR_COORDINACION` para `OfertaBeca`) no admiten transiciones posteriores.
- `Beca` nace en `OTORGADA` con `cadenaBecaId = id`, `becaAnteriorId = null`, `periodosCursados = 0`, `montoDesembolsadoPeriodoActual = 0`, `tieneRenovacionEnCurso = false` y `tipoCierre = null`.
- `habilitadaParaDesembolso` se calcula al activar: `true` si el programa no exige compromiso; `false` si lo exige. El compromiso aprobado la habilita.
- `PoliticaCobertura` y `CondicionesPermanencia` se modelan como *owned types* inmutables. `CostoMatriculaReferenciaSnapshotId` referencia la versión de arancel usada, sin clave foránea física.
- `CodigoBeca` se genera con el formato `BEC-{yyyy}-{secuencia}` usando una secuencia de PostgreSQL por año; es único.

**Alternativa descartada:** `OfertaBeca` como agregado independiente. Se descarta porque la aceptación debe cambiar `Beca` y `OfertaBeca` en la misma transacción y con el mismo bloqueo.

### 4. Persistencia, esquema y concurrencia

El esquema `becas` contiene:

| Tabla | Propósito |
| :--- | :--- |
| `beca` | Agregado raíz, cadena de becas, estados, montos y snapshot de cobertura. |
| `oferta_beca` | Oferta del periodo, snapshot de condiciones, carta, vencimiento y job de expiración. |
| `compromiso_becario` | Intentos de carga, revisión y habilitación de desembolso. |
| `historial_estado_beca` | Bitácora inmutable de transiciones. |
| `programa_beca_ref` | Proyección local de configuración de programa. |
| `costo_matricula_ref` | Proyección local de aranceles versionados. |
| `convenio_ref` | Proyección local del estado de convenio por universidad. |
| `idempotency_request` | Respuestas idempotentes por endpoint y clave. |
| `outbox_state`, `outbox_message`, `inbox_state` | Tablas de MassTransit EF Core. |

Convenciones:

- Nombres de tablas y columnas en `snake_case`; claves primarias `uuid`; `timestamptz` para `DateTimeOffset`; `date` para `DateOnly`; `numeric(18,2)` para dinero; `numeric(5,2)` para ponderaciones.
- Enumeraciones persistidas como `varchar` con `CHECK` que lista los valores permitidos. Esto facilita índices filtrados y lectura operativa.
- Toda entidad mutable hereda de `EntidadAuditable` (`fecha_creacion`, `usuario_creacion_id`, `fecha_ultima_modificacion`, `usuario_modificacion_id`, `version`). `version` es `IsConcurrencyToken` y se incrementa en cada `SaveChanges`.
- `historial_estado_beca` hereda de `EntidadInmutable`; un trigger de PostgreSQL bloquea `UPDATE` y `DELETE`, y el repositorio solo expone `Add`.
- Índices únicos parciales de respaldo:
  - `ux_beca_estudiante_cobertura_total_activa` sobre `(postulante_estudiante_id)` con `WHERE estado = 'ACTIVA' AND cobertura_modalidad = 'TOTAL'`.
  - `ux_beca_estudiante_periodo_no_acumulable_activa` sobre `(postulante_estudiante_id, periodo_vigente_actual_id)` con `WHERE estado = 'ACTIVA' AND permite_acumulacion = false`.
  - `ux_beca_cadena_periodo` sobre `(cadena_beca_id, periodo_vigente_actual_id)` para evitar dos filas de la misma cadena en un periodo.
  - `ux_oferta_beca_beca` sobre `(beca_id)`.
  - `ux_beca_codigo` sobre `(codigo_beca)`.
- La verificación de no doble beneficio se hace en aplicación con bloqueo; los índices son la última defensa y se prueban explícitamente.

**Concurrencia:**

- **Aceptar oferta:** transacción `Serializable` + `pg_advisory_xact_lock(hashtextextended(postulanteId::text, 0))` + `SELECT ... FOR UPDATE` sobre la fila de `Beca`. Así se serializan dos aceptaciones del mismo estudiante y se cumple `C3`.
- **Rechazar, anular y expirar:** `SELECT ... FOR UPDATE` sobre `Beca` para serializar contra una aceptación concurrente; el job también usa este bloqueo.
- **Emisión:** no requiere bloqueo pesimista; las restricciones únicas y la idempotencia de la reserva protegen duplicados.
- **Optimistic locking:** todas las actualizaciones usan `version`; un conflicto se traduce a `409 CONCURRENCY_CONFLICT`.

El bloqueo pesimista se implementa con SQL parametrizado sobre `becas.beca` y se encapsula en el repositorio; el dominio no conoce Npgsql.

### 5. Flujo de emisión y reserva de presupuesto

La emisión es la única operación que cruza servicios antes de escribir localmente:

1. Validar que exista `programa_beca_ref`, `costo_matricula_ref` vigente y `convenio_ref` en estado `VIGENTE` para la universidad de la postulación. Si falta configuración, responder `422 CONFIGURACION_PROGRAMA_NO_DISPONIBLE`.
2. Consultar `GET /internal/v1/postulaciones/{id}/adjudicable` en `MS-POS` con *Service Token* y timeout de 3 s. Si no es adjudicable, `422 POSTULACION_NO_ADJUDICABLE`; si el servicio no responde, `503 DEPENDENCIA_NO_DISPONIBLE`.
3. Calcular el monto de la oferta según `PoliticaCobertura` y el arancel vigente: `TOTAL` = matrícula; `PARCIAL` = porcentaje de la matrícula; `MONTO_FIJO` = monto fijo por periodo.
4. Pre-generar `ofertaId` y `becaId` (UUIDv7) y solicitar la reserva a `MS-PRG` con `Idempotency-Key: oferta-{ofertaId}`. La reserva se crea en estado `PENDIENTE` con TTL de 5 minutos. Si `MS-PRG` rechaza por cupo, presupuesto o convenio, se propaga el código y no se escribe localmente.
5. Abrir la transacción local, persistir `Beca` + `OfertaBeca`, generar la carta PDF, subirla a Firebase, guardar URI/hash, programar el job de expiración y publicar `OfertaBecaEmitida` en el Outbox.
6. Si la transacción local falla, la reserva expira por TTL. Si confirma, `MS-PRG` consume `OfertaBecaEmitida` y transiciona la reserva `PENDIENTE → CONFIRMADA`; al aceptarse la beca, `BecaActivada` la transiciona `CONFIRMADA → ACTIVADA`; al rechazarse, anularse o expirar la oferta, el evento correspondiente la libera. No se implementa compensación síncrona en este corte para no ampliar la lista cerrada de llamadas internas.

La guarda de la Tabla 3.2 (“`ReservaPresupuestoCupo` confirmada en `MS-PRG`”) se satisface con la reserva creada y retenida en `PENDIENTE`; la transición a `CONFIRMADA` se completa por el evento `OfertaBecaEmitida` dentro de la consistencia eventual que ya exige el patrón Outbox/Inbox.

La liberación de cupo/presupuesto al rechazar, anular o expirar se comunica mediante los eventos `OfertaBecaRechazada`, `OfertaBecaAnulada` y `OfertaBecaExpirada`; `MS-PRG` los consume y libera la reserva. Esto evita una llamada síncrona de liberación no contemplada en la lista cerrada.

### 6. Contratos de integración

**Claims de Keycloak** (contrato con `MS-PLT`):

- `roles`: roles de realm/cliente.
- `postulante_id`: UUID de `Postulante`, obligatorio para `POSTULANTE` y `BECARIO`.
- `programa_ids`: UUIDs de programas donde el coordinador tiene alcance; `DIRECCION` puede operar globalmente.
- `acr`/`amr`: evidencia de segundo factor. La política `Requiere2FA` acepta un valor configurable de `acr` o la presencia de `otp`/`totp` en `amr`.
- Si un endpoint administrativo recibe un token sin 2FA, responde `401 SEGUNDO_FACTOR_REQUERIDO`; si un coordinador no tiene alcance sobre el programa, `403 PERMISO_DENEGADO`.

**`MS-POS` — `GET /internal/v1/postulaciones/{id}/adjudicable`:**

```json
{
  "postulacionId": "uuid",
  "adjudicable": true,
  "programaBecaId": "uuid",
  "periodoAcademicoId": "uuid",
  "periodoCodigo": "2026-1",
  "universidadAliadaId": "uuid",
  "carreraId": "uuid"
}
```

`200` con `adjudicable=false` se trata como `422`. `404` se trata como `422 POSTULACION_NO_ENCONTRADA`. La respuesta no incluye datos socioeconómicos.

**`MS-PRG` — `POST /internal/v1/programas/{id}/reservas`:**

```json
{
  "referenciaTipo": "OFERTA_BECA",
  "referenciaId": "uuid-oferta",
  "periodoAcademicoId": "uuid",
  "monto": 1234.56,
  "cupos": 1
}
```

Respuesta `201`: `{ "reservaId": "uuid", "estado": "PENDIENTE", "monto": 1234.56, "cupos": 1, "expiraEn": "..." }`. La confirmación y liberación se resuelven por los eventos de oferta y de activación descritos en este diseño. Errores: `409 SIN_CUPO_O_PRESUPUESTO`, `422 CONVENIO_VENCIDO`.

**`MS-PLT` — `POST /internal/v1/otp/consumir`:** se implementa el puerto y el cliente, pero no se invoca en este cambio. El contrato queda documentado para el cambio de revocación y desembolsos.

**Eventos consumidos de `MS-PRG`:**

- `ProgramaBecaConfigurado`: `programaBecaId`, `codigo`, `nombre`, `politicaCobertura` (`modalidad`, `porcentajeCobertura`, `montoFijoPeriodo`, `moneda`), `condicionesPermanencia`, `permiteAcumulacion`, `requiereCompromisoFirmado`, `duracionPeriodos`, `estado`, `occurredAt`.
- `CostoMatriculaActualizado`: `costoMatriculaReferenciaId`, `carreraId`, `periodoAcademicoId`, `montoReferenciaMatricula`, `moneda`, `numeroVersion`, `fechaVigenciaDesde`, `fechaVigenciaHasta`, `estado`.
- `ConvenioUniversidadRegistrado` y `ConvenioUniversidadVencido`: `universidadAliadaId`, `convenioId`, `estado`, `fechaFin`.

Las proyecciones se actualizan con upsert idempotente y guardan `ultimo_evento_id`; un evento con `occurredAt` anterior al último aplicado se ignora para evitar retrocesos.

**Eventos publicados:** `OfertaBecaEmitida`, `BecaActivada`, `OfertaBecaRechazada`, `OfertaBecaAnulada`, `OfertaBecaExpirada`, `CompromisoBecarioCargado`, `CompromisoBecarioAprobado`, `CompromisoBecarioRechazado`. Todos llevan el sobre `EventoIntegracion<T>` con `eventId`, `occurredAt`, `correlationId`, `producer`, `aggregateId`, `aggregateVersion` y un payload con identificadores y estados; nunca incluyen nombre, documento, correo, cuenta bancaria ni montos socioeconómicos.

### 7. Superficie HTTP

| Método | Ruta | Autorización | Propósito |
| :--- | :--- | :--- | :--- |
| `POST` | `/api/v1/becas/ofertas` | `COORDINADOR_PROGRAMA` + 2FA + alcance | Emitir oferta. |
| `GET` | `/api/v1/becas/ofertas/{ofertaId}` | Dueño o coordinador | Consultar oferta. |
| `POST` | `/api/v1/becas/ofertas/{ofertaId}/aceptar` | `POSTULANTE` dueño | Aceptar oferta. |
| `POST` | `/api/v1/becas/ofertas/{ofertaId}/rechazar` | `POSTULANTE` dueño | Rechazar oferta. |
| `POST` | `/api/v1/becas/ofertas/{ofertaId}/anular` | `COORDINADOR_PROGRAMA` + 2FA + alcance | Anular con justificación. |
| `POST` | `/api/v1/becas/{becaId}/compromisos` | `BECARIO` dueño | Cargar compromiso firmado (multipart). |
| `POST` | `/api/v1/becas/compromisos/{compromisoId}/revision` | `COORDINADOR_PROGRAMA` + 2FA + alcance | Aprobar o rechazar. |
| `GET` | `/api/v1/becas/{becaId}` | Dueño o coordinador | Consultar beca. |
| `GET` | `/internal/v1/becas/activas` | *Service Account* + scope `becas.internal.read` | Consulta de `MS-POS`. |
| `GET` | `/health/live`, `/health/ready` | Anónimo | Salud. |

Todos los `POST` exigen `Idempotency-Key` (8-128 caracteres). Las respuestas de error usan `ProblemDetails` con `code`, `message`, `correlationId` y `errors`. El mapeo principal es: `TransicionEstadoInvalidaException` → `409 TRANSICION_INVALIDA`; `OfertaVencidaException` → `409 OFERTA_VENCIDA`; `DobleBeneficioException` → `409 DOBLE_BENEFICIO`; `JustificacionRequeridaException` → `400 JUSTIFICACION_REQUERIDA`; `PostulacionNoAdjudicableException` → `422 POSTULACION_NO_ADJUDICABLE`; `DependenciaNoDisponibleException` → `503 DEPENDENCIA_NO_DISPONIBLE`; `IdempotencyConflictException` → `409 IDEMPOTENCY_KEY_REUTILIZADA`; `CompromisoNoFirmadoException` → `412 COMPROMISO_NO_FIRMADO`.

**Alternativa descartada:** versionado por header. Se usa prefijo de ruta `/api/v1` y `/internal/v1` porque es el contrato ya definido para las rutas internas y simplifica el gateway.

### 8. Idempotencia

La tabla `idempotency_request` guarda `endpoint`, `clave`, `hash_payload`, `estado`, `status_code`, `response_body`, `recurso_id`, `creado_en` y `expira_en`. El flujo es:

1. El middleware exige la clave en `POST` y calcula el hash canónico del payload.
2. Dentro de la misma transacción del caso de uso, inserta la fila con `ON CONFLICT DO NOTHING`.
3. Si la fila ya existe y el hash coincide, devuelve la respuesta almacenada sin ejecutar el handler. Si el hash difiere, `409 IDEMPOTENCY_KEY_REUTILIZADA`. Si la fila existe pero aún no tiene respuesta, `409 IDEMPOTENCIA_EN_PROCESO` con `Retry-After: 1`.
4. Al confirmar, guarda el status y el cuerpo. Los errores `5xx` hacen rollback de la fila para permitir reintentos; los `4xx` deterministas se almacenan.
5. Un job diario elimina registros con `expira_en < now()`. TTL: 7 días.

**Alternativa descartada:** idempotencia solo en memoria o en Redis. Se descarta porque no sobrevive reinicios y no garantiza atomicidad con el cambio de estado.

### 9. Mensajería, Outbox e Inbox

MassTransit se configura con `AddEntityFrameworkOutbox<BecasDbContext>` usando PostgreSQL, `UseBusOutbox()` y las entidades de Outbox/Inbox en el esquema `becas`. Los handlers publican eventos de integración antes de `SaveChanges`; MassTransit los escribe en `outbox_message` dentro de la misma transacción. El dispatcher de Outbox los entrega a RabbitMQ después del commit.

Los consumidores de `MS-PRG` usan *Idempotent Inbox* por `eventId` y un `DbContext` scoped. Los eventos se declaran como contratos inmutables en `Application/IntegrationEvents` (records), no como clases de dominio. Los exchanges/queues se nombran por convención: exchange `Ascenso.Becas:<Evento>`, colas `ms-bec.<evento>`. Los consumidores de este cambio son las proyecciones de programa, costo y convenio.

**Alternativa descartada:** publicar directo a RabbitMQ dentro del handler. Se descarta porque un fallo del broker dejaría estados persistidos sin evento.

### 10. Hangfire y expiración

Hangfire usa PostgreSQL con esquema `hangfire`, cola `critical` para expiraciones y `default` para limpieza. `ExpirarOfertaBecaJob` se programa al emitir con `BackgroundJob.Schedule(..., TimeSpan.FromDays(15))` y su id se guarda en `OfertaBeca.referenciaJobExpiracionHangfireId`.

Para cubrir la ventana entre el commit y la programación del job, un job recurrente `ReconciliarOfertasPendientesJob` (cada 5 minutos) busca ofertas `PENDIENTE_RESPUESTA` con `fecha_vencimiento_aceptacion <= now()` y ejecuta la misma lógica. Ambos caminos son idempotentes y usan `SELECT ... FOR UPDATE`; si la oferta ya no está pendiente, no hacen nada.

El dashboard de Hangfire solo se habilita en desarrollo o con política administrativa; en producción se protege con el mismo esquema de Keycloak y no se expone por el gateway.

**Alternativa descartada:** un `Timer` en memoria. Se descarta porque no sobrevive reinicios ni despliegues.

### 11. Archivos y carta de otorgamiento

El puerto `IAlmacenamientoArchivos` recibe un `Stream`, content type y ruta lógica, calcula SHA-256, sube el objeto y devuelve `{ url, hashSha256, tamanioBytes }`. En producción se implementa con Google Cloud Storage/Firebase; en desarrollo y pruebas se usa un adaptador local o un fake. Las rutas son inmutables:

- `becas/{becaId}/ofertas/{ofertaId}/carta-otorgamiento.pdf`
- `becas/{becaId}/compromisos/{compromisoId}/intento-{n}.pdf`

La carta se genera con `PdfSharpCore` a partir de una plantilla simple (datos de la fundación, estudiante, programa, periodo, cobertura, monto y vigencia). El PDF se sube antes de confirmar la transacción local; si la subida falla, la emisión falla y no se persiste la oferta. El hash se calcula sobre los bytes del PDF y se guarda en `hash_carta_sha256`.

Para el compromiso se valida que el archivo sea PDF, que no supere 5 MB y que el hash no coincida con un intento anterior del mismo compromiso; un intento repetido se registra como nuevo `numeroIntentoCarga` solo si el anterior fue rechazado.

**Alternativa descartada:** almacenar binarios en PostgreSQL. Se descarta por la restricción explícita de usar Firebase Storage.

### 12. Observabilidad, errores y protección de datos

- Serilog con salida JSON a consola y enriquecimiento con `CorrelationId`, `TraceId`, `SpanId` y `Service=ms-bec`.
- OpenTelemetry para trazas y métricas; exportador OTLP configurable y consola en desarrollo. Se instrumentan ASP.NET Core, HttpClient, Npgsql, MassTransit y Hangfire.
- Middleware de correlación: acepta `X-Correlation-Id` o genera uno; lo devuelve en la respuesta y lo propaga a clientes HTTP y eventos.
- Los logs enmascaran OTP, números de cuenta, documentos y correos. Los DTOs de eventos solo llevan identificadores.
- Health checks: `live` sin dependencias; `ready` con PostgreSQL, RabbitMQ y migraciones aplicadas. Si una dependencia falla, `ready` responde `503`.
- ProblemDetails estándar; no se filtran stack traces en producción.

### 13. Pruebas

- **Dominio:** tabla de transiciones completa, estados terminales, justificación mínima, no doble beneficio, snapshots, compuerta de compromiso y cálculo de montos por modalidad.
- **Aplicación:** handlers con repositorios y puertos falsos; verificación de eventos, idempotencia y mapeo de errores.
- **Arquitectura:** NetArchTest para las reglas de dependencia.
- **Integración:** Testcontainers con PostgreSQL y RabbitMQ; migraciones desde cero, índices únicos, trigger de historial, bloqueo por estudiante, Outbox/Inbox, expiración de oferta, API con autenticación de prueba y WireMock para `MS-POS`/`MS-PRG`.
- **Contratos:** pruebas de serialización de eventos y DTOs internos; validación de OpenAPI.
- **Seguridad:** tokens sin 2FA, sin `postulante_id`, sin alcance de programa y llamadas internas sin service token.
- **Cobertura:** mínimo 80% en `Domain` + `Application`; el pipeline falla si baja.

Se usa un `WebApplicationFactory` con un esquema de autenticación de prueba para no depender de Keycloak en cada test; la configuración real de Keycloak se valida en un test de configuración y en el entorno local.

### 14. Entorno local y CI

- `compose.yaml` con PostgreSQL 17, RabbitMQ 4 (management), Keycloak 26, un perfil opcional para la API y un servicio `dev-stubs` (WireMock o API mínima) que simula `MS-POS`/`MS-PRG` con datos semilla. Healthchecks y volúmenes persistentes.
- `deploy/keycloak/ascenso-realm.json` con realm `ascenso`, clientes `ms-bec` y `ms-bec-internal`, roles base y usuarios de prueba. El client interno usa *client credentials* y scopes `becas.internal.read`.
- En desarrollo, el almacenamiento local es el modo por defecto (`Storage:Provider=Local`) y no requiere credenciales de Firebase. Firebase se usa en integración/producción con bucket y service account fuera del repositorio.
- `Makefile` con `up`, `down`, `migrate`, `run`, `test`, `format`; `README.md` con requisitos y pasos.
- `Dockerfile` multi-stage, usuario no root y `HEALTHCHECK`.
- CI en GitHub Actions: restore bloqueado, build `-warnaserror`, `dotnet format --verify-no-changes`, pruebas unitarias/arquitectura, pruebas de integración con Docker, cobertura y build de imagen. Se publica el artefacto de cobertura.

**Alternativa descartada:** aplicar migraciones automáticamente al arrancar en todos los entornos. Se descarta porque varios pods podrían competir; en producción se ejecuta un paso de migración previo y `Database:AutoMigrate` queda en `false`.

## Risks / Trade-offs

| Riesgo | Mitigación |
| :--- | :--- |
| Los contratos de `ProgramaBecaConfigurado`, `CostoMatriculaActualizado`, `adjudicable` y `reservas` no están definidos al detalle en la especificación maestra. | Se documentan en este diseño, se implementan detrás de puertos/adaptadores y se cubren con pruebas de contrato y WireMock; cualquier ajuste queda localizado en `Infrastructure`. |
| Los claims `postulante_id` y `programa_ids` requieren cambios en `MS-PLT`/Keycloak. | Se definen como contrato de identidad y se valida en pruebas; si faltan, el servicio falla cerrado (`401`/`403`), nunca autoriza de más. |
| La emisión hace una llamada síncrona a `MS-PRG` antes de escribir; si el broker o la red fallan, la reserva puede quedar creada sin oferta. | La reserva nace `PENDIENTE` con TTL de 5 minutos y la clave de idempotencia evita duplicados; `OfertaBecaEmitida` la confirma y los eventos de oferta terminal la liberan. Se monitorea el Outbox y las reservas huérfanas. |
| El índice único parcial no cubre todos los casos de acumulación (una beca acumulable y otra no). | La regla se valida en el dominio con aislamiento `Serializable` y bloqueo por estudiante; el índice es una defensa adicional, no la única. |
| La generación de PDF y Firebase agrega latencia y dependencia externa a la emisión. | Se sube el archivo antes del commit y se falla cerrado; en desarrollo se usa almacenamiento local y en pruebas un fake. |
| Hangfire y MassTransit comparten PostgreSQL y pueden competir por conexiones. | Se usan pools separados, límites de workers y healthchecks; el esquema `hangfire` se mantiene aislado. |
| El bloqueo `FOR UPDATE` puede aumentar la contención en becas muy activas. | Las operaciones son por beca/estudiante y de corta duración; se monitorea la latencia y se mantiene el timeout de 3 s para dependencias. |
| La proyección local de configuración puede quedar desactualizada si se pierde un evento. | Inbox idempotente, control de `occurredAt`/`ultimo_evento_id`, métrica de configuración faltante que bloquea la emisión y procedimiento documentado para que `MS-PRG` reemita el evento. |

## Migration Plan

1. Crear la base `ascenso_becas`, el usuario de aplicación y los esquemas `becas` y `hangfire` en el entorno destino.
2. Registrar el cliente `ms-bec` y el cliente de servicio `ms-bec-internal` en Keycloak; cargar roles, scopes y mappers de claims.
3. Crear el bucket de Firebase y el service account; cargar las credenciales como secreto.
4. Declarar exchanges/colas de `MS-BEC` y suscribir los consumidores de `MS-PRG`.
5. Ejecutar `dotnet ef database update` como paso previo al despliegue de la API.
6. Desplegar la API y verificar `/health/ready`, el dashboard de Hangfire y la publicación de un evento de prueba en Outbox.
7. Rollback: desplegar la imagen anterior; las migraciones son forward-only en producción. Si se requiere revertir el esquema en un entorno no productivo, se restaura el snapshot previo. No se elimina el esquema en producción durante un rollback.

## Open Questions

- Confirmar con `MS-PRG` el payload exacto de `ProgramaBecaConfigurado` y `CostoMatriculaActualizado`; el diseño asume que incluyen la política de cobertura y las condiciones de permanencia completas.
- Confirmar con `MS-POS` que `adjudicable` devuelve `carreraId` y `universidadAliadaId`, necesarios para seleccionar el arancel y validar el convenio.
- Confirmar con `MS-PLT` el nombre final de los claims `postulante_id` y `programa_ids` y el valor de `acr` para 2FA. Se mantienen configurables para no bloquear el desarrollo.
