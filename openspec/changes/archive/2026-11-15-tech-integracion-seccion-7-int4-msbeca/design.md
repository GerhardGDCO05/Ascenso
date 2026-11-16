## Context

La sección 7 se implementó sobre la fundación de las secciones 1–6. Su objetivo fue conectar la capa de aplicación con los servicios externos y con el bus de eventos sin filtrar detalles de infraestructura al dominio: clientes HTTP *fail-closed*, *Service Tokens* de Keycloak, contratos de integración inmutables y mensajería transaccional con Outbox/Inbox. Las secciones 8–14 continúan en el cambio activo y no forman parte de este histórico.

## Decisions

### Clientes HTTP internos

- `RegistroClientes.AgregarClientes()` registra `HttpClient` keyed de larga vida para `postulaciones`, `programas` y `plataforma`, con un único `SocketsHttpHandler`.
- El pool conserva conexiones durante 5 minutos, las conexiones inactivas 1 minuto y aplica un `ConnectTimeout` de 3 segundos. El timeout total se lee de `Clientes:TimeoutSegundos` y por defecto es de 3 segundos.
- Las URLs base se leen de `Clientes:Postulaciones:BaseUrl`, `Clientes:Programas:BaseUrl` y `Clientes:Plataforma:BaseUrl`. Una URL inválida falla al arrancar.
- `SolicitudIntegracion` centraliza la creación de peticiones, la serialización JSON con `JsonSerializerDefaults.Web`, la lectura del cuerpo y la propagación de `X-Correlation-Id` desde `ICorrelacionContexto`.
- `TokenServicioDelegatingHandler` agrega `Authorization: Bearer` solo cuando `UsaServiceToken` es verdadero; el token lo obtiene `ServiceTokenProvider`.

### `PostulacionesClient`

- Consulta `GET /internal/v1/postulaciones/{id}/adjudicable` con el identificador validado.
- Un `404` se traduce a `PostulacionNoEncontradaException`; un `false` se devuelve como resultado de negocio, no como error técnico.
- Una respuesta no exitosa, vacía o con JSON inválido lanza `HttpRequestException`.
- `EmitirOfertaBecaHandler` captura `HttpRequestException`, `TimeoutException` y `TaskCanceledException` y las convierte en `DependenciaNoDisponibleException("MS-POS")`, que la API expone como `503 DEPENDENCIA_NO_DISPONIBLE`.
- El timeout de 3 segundos lo aplica el `HttpClient`; no se duplica con un `CancellationTokenSource` interno.

### `ProgramasClient`

- Envía `POST /internal/v1/programas/{id}/reservas` con `referenciaTipo`, `referenciaId`, `periodoAcademicoId`, `monto` y `cupos`.
- La clave de idempotencia es estable por referencia: `Idempotency-Key: oferta-{referenciaId}`. Un reintento de la misma oferta reutiliza la clave y no duplica la reserva.
- Los rechazos `409` y `422` se leen del cuerpo y se traducen a `ReservaPresupuestoRechazadaException` con los códigos `SIN_CUPO_O_PRESUPUESTO` y `CONVENIO_VENCIDO`; otros códigos se conservan.
- La respuesta exitosa contiene `reservaId`, `estado`, `monto`, `cupos` y `expiraEn`. El handler valida que el estado sea `PENDIENTE` o `CONFIRMADA`, que el identificador sea válido, que el monto sea suficiente y que exista al menos un cupo.
- La reserva nace en `PENDIENTE` con TTL de 5 minutos; la confirmación y la liberación se completan por eventos de otorgamiento.

### `OtpClient`

- Consume `POST /internal/v1/otp/consumir` con `usuarioId`, `operacion`, `recursoId`, `huellaPayload` y `codigo`.
- Usa una `Idempotency-Key` derivada de usuario, operación y recurso: `otp-{usuario}-{operacion}-{recurso}`.
- Un `204` se interpreta como consumo exitoso. Un `2xx` con `consumido=false` devuelve el `codigoError` informado o `OTP_INVALIDO`.
- Los `4xx` se mapean a códigos estables: `403` → `OTP_BLOQUEADO`, `410` → `OTP_EXPIRADO`, `401` → `OTP_INVALIDO`, `404` → `OTP_NO_ENCONTRADO` y el resto → `OTP_RECHAZADO`. Los `5xx` lanzan `HttpRequestException`.
- El cliente queda registrado, pero no se invoca desde los casos de uso de este cambio; se activará en desembolsos, revocaciones y modificaciones de datos de pago.

### *Service Tokens*

- `ServiceTokenProvider` solicita el token con `grant_type=client_credentials` al endpoint configurado en `Keycloak:ServiceToken:TokenEndpoint`.
- Si no se configura el endpoint explícito, se deriva de `Keycloak:ServiceToken:Authority` o de `Jwt:Authority` como `{authority}/protocol/openid-connect/token`.
- La caché es en memoria por proceso. El token se reutiliza mientras no esté dentro del margen de renovación (`MargenRenovacionSegundos`, 30 por defecto).
- Un `SemaphoreSlim` serializa la renovación para que varias llamadas concurrentes no soliciten tokens duplicados.
- `Invalidar()` descarta el token cuando el proveedor responde `401`; la siguiente llamada solicita uno nuevo.
- Si faltan credenciales, el endpoint es inválido o Keycloak rechaza la solicitud, el proveedor falla de forma cerrada con `HttpRequestException`. El secreto se inyecta por configuración o variable de entorno y no se registra en logs.

### MassTransit, Outbox e Inbox

- `AgregarMensajeria()` configura `AddEntityFrameworkOutbox<BecasDbContext>` con PostgreSQL, `UseBusOutbox()` y las entidades `outbox_message`, `outbox_state` e `inbox_state` del esquema `becas`.
- Los intercambios de salida se nombran `Ascenso.Becas:<Evento>` y los de entrada `Ascenso.Programas:<Evento>`.
- Las colas de entrada son estables y en *kebab-case*: `ms-bec.programa-beca-configurado`, `ms-bec.costo-matricula-actualizado`, `ms-bec.convenio-universidad-registrado` y `ms-bec.convenio-universidad-vencido`.
- Cada endpoint usa `PrefetchCount` configurable (16 por defecto), reintentos exponenciales de 5 intentos entre 1 y 30 segundos, Outbox de EF Core y la cola de error estándar de MassTransit (`_error`).
- `Mensajeria:Habilitada=false` registra `OutboxBecaNoDisponible`, que falla de forma explícita para no perder eventos silenciosamente.
- `OutboxBeca` adapta `IOutboxBeca` a `IPublishEndpoint`. Los handlers encolan antes de `SaveChanges`; MassTransit escribe el mensaje en `outbox_message` dentro de la misma transacción y lo entrega a RabbitMQ después del commit.

### Consumidores y proyecciones

- Los cuatro consumidores validan `eventId`, `aggregateId` y los campos obligatorios del payload antes de escribir.
- `CodigosIntegracion` convierte los códigos en mayúsculas de los contratos a las enumeraciones del dominio y rechaza valores desconocidos.
- `ActualizadorProyeccionesRef` aplica un upsert idempotente por `ultimo_evento_id` y `occurred_at`: un evento repetido no modifica la fila y un evento anterior o igual al último aplicado se ignora para evitar retrocesos.
- El Inbox de MassTransit registra `MessageId` + `ConsumerId`; un mensaje duplicado no se vuelve a procesar.

### Contratos de eventos

- El sobre `EventoIntegracion<T>` contiene `eventId`, `occurredAt`, `correlationId`, `producer`, `aggregateId`, `aggregateVersion` y `payload`.
- Los ocho contratos de salida son `OfertaBecaEmitida`, `BecaActivada`, `OfertaBecaRechazada`, `OfertaBecaAnulada`, `OfertaBecaExpirada`, `CompromisoBecarioCargado`, `CompromisoBecarioAprobado` y `CompromisoBecarioRechazado`.
- Los cuatro contratos de entrada son `ProgramaBecaConfigurado`, `CostoMatriculaActualizado`, `ConvenioUniversidadRegistrado` y `ConvenioUniversidadVencido`.
- Los payloads solo llevan identificadores, estados, fechas y códigos de cierre; no incluyen nombre, documento, correo, cuenta bancaria ni montos socioeconómicos.
- `MapeadorEventosIntegracion` es explícito: un evento de dominio sin contrato lanza `InvalidOperationException` para impedir publicaciones accidentales con un contrato incompleto.

### Publicación desde los handlers

- Los siete handlers de otorgamiento recorren `beca.EventosDominio` y llaman a `IOutboxBeca.EncolarAsync` antes de `GuardarCambiosAsync`.
- Se pasa `beca.Version` como `aggregateVersion` para conservar el orden lógico del agregado.
- La escritura del estado de negocio y la del Outbox comparten la transacción local; un fallo de `SaveChanges` no deja eventos confirmados.

### Métrica de configuración faltante

- `MetricaConfiguracionFaltante` expone el medidor `Ascenso.Becas` y el contador `becas.configuracion_faltante` con la etiqueta `recurso`.
- Los valores normalizados son `programa`, `costo_matricula` y `convenio`.
- La emisión registra la métrica y responde `422 CONFIGURACION_PROGRAMA_NO_DISPONIBLE` cuando falta la proyección o no está vigente; no crea beca, no reserva presupuesto y no publica eventos.
- `docs/operacion/configuracion-prg.md` documenta la señal, la alerta de Prometheus, el diagnóstico SQL y el procedimiento para que `MS-PRG` reemita el evento con un `eventId` y un `occurredAt` nuevos.

### Pruebas y herramienta local

- WireMock.Net simula `MS-POS`, `MS-PRG`, `MS-PLT` y Keycloak para verificar rutas, encabezados, códigos y claves de idempotencia.
- Testcontainers levanta PostgreSQL 17 y RabbitMQ 3.13 para verificar Outbox, Inbox, consumidores y proyecciones contra infraestructura real.
- La prueba de mensajería usa un `IHost` para arrancar los servicios hospedados de MassTransit; un proveedor de servicios plano no despacha el Outbox.
- `Ascenso.Becas.UnitTests.slnf` agrupa Domain, Application y Architecture para ejecutar pruebas unitarias locales sin la advertencia de VSTest que aparece al aplicar un filtro a un ensamblado sin coincidencias.

## Risks / Trade-offs

- La reserva en `MS-PRG` ocurre antes de la escritura local. Si la persistencia falla después de una reserva exitosa, la reserva puede quedar huérfana; el TTL de 5 minutos y los eventos de cierre la liberan, y la reconciliación posterior debe cubrir el residuo.
- El despacho del Outbox depende de los servicios hospedados de MassTransit. En pruebas se debe usar `IHost`; en producción el worker debe estar activo para que los mensajes salgan de `outbox_message`.
- El control de orden por `occurredAt` ignora eventos anteriores o iguales. Una reemisión debe llevar una marca temporal posterior; de lo contrario, la proyección no retrocede y el incidente continúa.
- La caché de *Service Tokens* es local al proceso. Cada réplica solicita su propio token; es aceptable para este corte y evita introducir una dependencia distribuida prematura.
- `OtpClient` no se invoca todavía desde los casos de uso; su contrato se verificó con WireMock para no activarlo antes de las secciones de desembolsos y revocaciones.
- Si la mensajería se deshabilita, los handlers fallan explícitamente al encolar. Se prefiere el fallo visible a perder eventos.
- Un payload de entrada inválido agota los reintentos y termina en la cola de error; requiere intervención operativa y no se descarta en silencio.

## Migration Plan

No hay migraciones nuevas. La migración inicial ya crea `becas.outbox_message`, `becas.outbox_state` y `becas.inbox_state`; la sección 7 solo las utiliza. La configuración nueva se agrega a `appsettings.json` y el secreto de Keycloak se inyecta por entorno o gestor de secretos.

## Open Questions

- Ninguna para la sección 7. Las secciones 8–14 continúan en el cambio activo `tech-bootstrap-int4-msbeca`.
