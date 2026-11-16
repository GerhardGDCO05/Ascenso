# Registro de Auditoría — Sección 7: clientes de integración y contratos

- **Fecha y hora de ejecución:** 15 de noviembre de 2026, 21:40 (UTC-4)
- **Rama:** `ms-beca-clientes-integracion-y-contratos`
- **Commit de la sección:** `d54681f` (`feat(ms-becas): implementar clientes de integracion, MassTransit y contratos de eventos (tareas 7.1-7.9)`); el árbol de trabajo de código quedó limpio.
- **Alcance:** tareas `7.1` a `7.9` del cambio `tech-bootstrap-int4-msbeca`.
- **Microservicio intervenido:** solo `Apps/services/ms-becas/`; `ms-pos`, `ms-plt`, `ms-prg` y los demás servicios quedaron intactos.
- **Trabajo local:** este histórico se crea después del commit y queda sin comprometer. No se archivó el cambio activo ni se sincronizaron specs principales.
- **Estado del cambio activo:** 69/114 tareas; secciones 8–14 pendientes. El cambio **no** se movió a `archive/`.

## 1. Objetivo

Cerrar la capa de integración de `MS-BEC`: clientes HTTP *fail-closed* hacia `MS-POS`, `MS-PRG` y `MS-PLT`, *Service Tokens* de Keycloak con caché, contratos de eventos sin PII y mensajería MassTransit con *Transactional Outbox* e *Idempotent Inbox*. La sección también debía conectar la publicación de eventos con los siete handlers de otorgamiento y dejar una señal operativa para la configuración faltante de `MS-PRG`.

## 2. Cambios realizados

### 2.1 `PostulacionesClient` (7.1)

- `GET /internal/v1/postulaciones/{id}/adjudicable` con el identificador de postulación validado.
- Propagación de `X-Correlation-Id` mediante `SolicitudIntegracion` y `ICorrelacionContexto`.
- Timeout total de 3 segundos configurable en `Clientes:TimeoutSegundos`; `ConnectTimeout` de 3 segundos en el handler.
- `404` → `PostulacionNoEncontradaException`; `adjudicable=false` se devuelve como resultado de negocio; respuesta no exitosa, vacía o JSON inválido → `HttpRequestException`.
- `EmitirOfertaBecaHandler` convierte `HttpRequestException`, `TimeoutException` y `TaskCanceledException` en `DependenciaNoDisponibleException("MS-POS")`, expuesta como `503 DEPENDENCIA_NO_DISPONIBLE`.

### 2.2 `ProgramasClient` (7.2)

- `POST /internal/v1/programas/{id}/reservas` con `referenciaTipo`, `referenciaId`, `periodoAcademicoId`, `monto` y `cupos`.
- `Idempotency-Key: oferta-{referenciaId}` estable por oferta; el reintento reutiliza exactamente la misma clave.
- `409 SIN_CUPO_O_PRESUPUESTO` y `422 CONVENIO_VENCIDO` se traducen a `ReservaPresupuestoRechazadaException`; otros códigos de rechazo se conservan.
- La reserva se crea en `PENDIENTE` con TTL de 5 minutos; el handler solo acepta `PENDIENTE` o `CONFIRMADA`, identificador válido, monto suficiente y al menos un cupo.

### 2.3 `OtpClient` (7.3)

- `POST /internal/v1/otp/consumir` con `usuarioId`, `operacion`, `recursoId`, `huellaPayload` y `codigo`.
- `Idempotency-Key: otp-{usuario}-{operacion}-{recurso}`.
- `204` → consumo exitoso; `2xx` con `consumido=false` → código informado o `OTP_INVALIDO`; `4xx` → `OTP_BLOQUEADO` (`403`), `OTP_EXPIRADO` (`410`), `OTP_INVALIDO` (`401`), `OTP_NO_ENCONTRADO` (`404`) u `OTP_RECHAZADO`; `5xx` → `HttpRequestException`.
- El cliente queda registrado y probado con WireMock, pero no se invoca todavía desde los casos de uso; se activará en desembolsos, revocaciones y modificaciones de datos de pago.

### 2.4 *Service Tokens* de Keycloak (7.4)

- `ServiceTokenProvider` usa `grant_type=client_credentials` contra `Keycloak:ServiceToken:TokenEndpoint`.
- Si el endpoint no está configurado, se deriva de `Keycloak:ServiceToken:Authority` o `Jwt:Authority` como `{authority}/protocol/openid-connect/token`.
- Caché en memoria por proceso, reutilización mientras el token no esté dentro del margen de renovación (`MargenRenovacionSegundos`, 30 por defecto) y `SemaphoreSlim` para evitar renovaciones concurrentes duplicadas.
- `Invalidar()` descarta el token ante un `401`; la siguiente llamada solicita uno nuevo.
- Faltan credenciales, endpoint inválido o rechazo de Keycloak → `HttpRequestException` *fail-closed*. El secreto se inyecta por configuración o entorno y no se registra en logs.
- `TokenServicioDelegatingHandler` agrega `Authorization: Bearer` solo cuando `UsaServiceToken=true`.

### 2.5 MassTransit, Outbox e Inbox (7.5)

- `AddEntityFrameworkOutbox<BecasDbContext>` con PostgreSQL y `UseBusOutbox()`.
- Entidades `becas.outbox_message`, `becas.outbox_state` y `becas.inbox_state` mapeadas en `BecasDbContext`.
- Intercambios de salida `Ascenso.Becas:<Evento>` y de entrada `Ascenso.Programas:<Evento>`.
- Colas estables `ms-bec.programa-beca-configurado`, `ms-bec.costo-matricula-actualizado`, `ms-bec.convenio-universidad-registrado` y `ms-bec.convenio-universidad-vencido`, con `PrefetchCount` configurable (16 por defecto).
- Reintentos exponenciales de 5 intentos entre 1 y 30 segundos, cola de error estándar de MassTransit (`_error`) e Inbox idempotente por `MessageId` + `ConsumerId`.
- `OutboxBeca` adapta `IOutboxBeca` a `IPublishEndpoint`; la publicación se escribe en `outbox_message` dentro de la transacción local y se entrega después del commit.
- `Mensajeria:Habilitada=false` registra `OutboxBecaNoDisponible`, que falla explícitamente para no perder eventos en silencio.
- La prueba con Testcontainers verifica que el mensaje se persiste en Outbox antes del commit, que llega al consumidor y que un duplicado no se vuelve a procesar.

### 2.6 Consumidores de proyecciones (7.6)

- `ProgramaBecaConfiguradoConsumidor`, `CostoMatriculaActualizadoConsumidor`, `ConvenioUniversidadRegistradoConsumidor` y `ConvenioUniversidadVencidoConsumidor`.
- Validación de `eventId`, `aggregateId` y campos obligatorios del payload; conversión de códigos a enumeraciones del dominio con rechazo de valores desconocidos.
- Upsert idempotente con `ActualizadorProyeccionesRef` y control de `ultimo_evento_id`/`occurred_at`: un evento repetido no modifica la fila y uno anterior o igual no retrocede la proyección.
- Un evento duplicado o desactualizado no altera la configuración local; la reemisión debe llevar un `occurredAt` posterior.

### 2.7 Contratos de eventos (7.7)

- Sobre `EventoIntegracion<T>` con `eventId`, `occurredAt`, `correlationId`, `producer`, `aggregateId`, `aggregateVersion` y `payload`.
- Ocho contratos de salida: `OfertaBecaEmitida`, `BecaActivada`, `OfertaBecaRechazada`, `OfertaBecaAnulada`, `OfertaBecaExpirada`, `CompromisoBecarioCargado`, `CompromisoBecarioAprobado` y `CompromisoBecarioRechazado`.
- Cuatro contratos de entrada: `ProgramaBecaConfigurado`, `CostoMatriculaActualizado`, `ConvenioUniversidadRegistrado` y `ConvenioUniversidadVencido`.
- Los payloads contienen solo identificadores, estados, fechas y códigos de cierre; no incluyen nombre, documento, correo, cuenta bancaria ni montos socioeconómicos.
- `MapeadorEventosIntegracion` es explícito y lanza `InvalidOperationException` ante un evento de dominio sin contrato, para evitar publicaciones accidentales.

### 2.8 Publicación desde los handlers (7.8)

- Los siete handlers de otorgamiento (`Emitir`, `Aceptar`, `Rechazar`, `Anular`, `Expirar`, `CargarCompromiso` y `RevisarCompromiso`) recorren `beca.EventosDominio` y llaman a `IOutboxBeca.EncolarAsync` antes de `GuardarCambiosAsync`.
- Se propaga `beca.Version` como `aggregateVersion`.
- El estado de negocio y el mensaje de Outbox se confirman en la misma transacción; un fallo de `SaveChanges` no deja eventos publicados.

### 2.9 Métrica y procedimiento operativo (7.9)

- `MetricaConfiguracionFaltante` expone el medidor `Ascenso.Becas` y el contador `becas.configuracion_faltante` con la etiqueta `recurso`.
- Valores normalizados: `programa`, `costo_matricula` y `convenio`.
- La emisión bloquea con `422 CONFIGURACION_PROGRAMA_NO_DISPONIBLE`, registra la métrica y no crea beca, no reserva presupuesto ni publica eventos.
- `docs/operacion/configuracion-prg.md` documenta la señal, la alerta `BecasConfiguracionPrgFaltante`, el diagnóstico SQL y el procedimiento para que `MS-PRG` reemita el evento con `eventId` y `occurredAt` nuevos.

### 2.10 Ajustes de validación y herramienta local

- La prueba de Outbox/Inbox se ajustó para arrancar los servicios hospedados de MassTransit mediante `IHost`; un proveedor de servicios plano no despacha el Outbox.
- Se alineó la nomenclatura de los campos estáticos privados con `.editorconfig` (`_camelCase`) y se dejó `dotnet format --verify-no-changes` limpio.
- Se creó `Apps/services/ms-becas/Ascenso.Becas.UnitTests.slnf` con Domain, Application y Architecture para ejecutar las pruebas unitarias locales sin la advertencia de VSTest que aparecía al aplicar `FullyQualifiedName!~IntegrationTests` al ensamblado de integración.

## 3. Archivos principales

- `Apps/services/ms-becas/Ascenso.Becas.Application/IntegracionEventos/`: `EventoIntegracion.cs`, `EventosEntrada.cs`, `EventosSalida.cs` y `MapeadorEventosIntegracion.cs`.
- `Apps/services/ms-becas/Ascenso.Becas.Application/Excepciones/PostulacionNoEncontradaException.cs`.
- `Apps/services/ms-becas/Ascenso.Becas.Application/Otorgamiento/Puertos/`: `IOutboxBeca.cs` e `IMetricaConfiguracionFaltante.cs`.
- `Apps/services/ms-becas/Ascenso.Becas.Application/Otorgamiento/Comandos/`: los siete handlers de otorgamiento.
- `Apps/services/ms-becas/Ascenso.Becas.Infrastructure/Clientes/`: `ClientesOptions.cs`, `ServiceTokenOptions.cs`, `ServiceTokenProvider.cs`, `TokenServicioDelegatingHandler.cs`, `SolicitudIntegracion.cs`, `PostulacionesClient.cs`, `ProgramasClient.cs`, `OtpClient.cs` y `RegistroClientes.cs`.
- `Apps/services/ms-becas/Ascenso.Becas.Infrastructure/Mensajeria/`: `RabbitMqOptions.cs`, `OutboxBeca.cs` y `Consumidores/`.
- `Apps/services/ms-becas/Ascenso.Becas.Infrastructure/Metricas/MetricaConfiguracionFaltante.cs`.
- `Apps/services/ms-becas/Ascenso.Becas.Infrastructure/Dependencias/`: `RegistroMensajeria.cs`, `RegistroMetricas.cs` y `RegistroPersistencia.cs`.
- `Apps/services/ms-becas/Ascenso.Becas.Api/Program.cs`, `appsettings.json` y `Ascenso.Becas.UnitTests.slnf`.
- `Apps/services/ms-becas/docs/operacion/configuracion-prg.md`.
- `openspec/changes/tech-bootstrap-int4-msbeca/tasks.md`: tareas `7.1` a `7.9` marcadas.

## 4. Pruebas incorporadas

- `IntegrationTests/Clientes/ClientesInternosTests.cs`: 12 pruebas con WireMock para adjudicabilidad, `404`, `false`, timeout, reserva, clave de idempotencia, errores `409`/`422` y consumo de OTP.
- `IntegrationTests/Clientes/ServiceTokenProviderTests.cs`: 6 pruebas para reutilización, renovación anticipada, concurrencia, encabezado `Authorization`, rechazo de credenciales e invalidación ante `401`.
- `IntegrationTests/Mensajeria/MensajeriaRabbitMqTests.cs`: 2 pruebas con PostgreSQL y RabbitMQ/Testcontainers para Outbox/Inbox y proyección.
- `IntegrationTests/Metricas/MetricaConfiguracionFaltanteTests.cs`: 1 prueba de la métrica y su etiqueta `recurso`.
- `Application.Tests/IntegracionEventos/EventosIntegracionTests.cs`: 18 pruebas de envoltura, serialización sin PII, evento desconocido y conservación de metadatos.
- Se ajustaron `Application.Tests/Otorgamiento/EmitirOfertaBecaHandlerTests.cs` y `EscenarioOtorgamiento.cs` para el nuevo puerto de métrica y Outbox.

**Suite completa:** 480/480 correctas (Domain 136, Application 95, Integration 241, Architecture 8). La sección 6 cerró con 441 pruebas; la sección 7 agregó 39.

**Subconjunto unitario local:** 239/239 correctas (Domain 136, Application 95, Architecture 8) mediante `Ascenso.Becas.UnitTests.slnf`, sin la advertencia de VSTest.

## 5. Verificación ejecutada

- `dotnet build Ascenso.Becas.sln -c Release --warnaserror --no-restore`: 0 errores y 0 advertencias.
- `dotnet test Ascenso.Becas.sln -c Release --no-build`: 480/480 correctas.
- `dotnet test Ascenso.Becas.UnitTests.slnf -c Release --no-build`: 239/239 correctas, sin advertencias.
- `dotnet format Ascenso.Becas.sln --verify-no-changes --no-restore`: limpio.
- `dotnet restore Ascenso.Becas.sln --locked-mode -p:NuGetAudit=false`: correcto.
- `dotnet ef migrations has-pending-model-changes --project Ascenso.Becas.Infrastructure --startup-project Ascenso.Becas.Infrastructure --context BecasDbContext`: sin cambios de modelo. Se usó Infrastructure como proyecto de arranque porque Api no referencia `Microsoft.EntityFrameworkCore.Design`.
- `git diff --check`: limpio.
- `openspec validate --all --strict`: 5/5 elementos correctos.

## 6. Análisis de HUs del roadmap

No se marcó ningún RF de `MS-BEC` en `openspec/roadmaps/por_microservicio.md` ni en `openspec/roadmaps/por_integrante.md`:

- **RF-15 (emisión de carta/oferta):** la integración síncrona con `MS-POS`/`MS-PRG`, el Outbox y la métrica están listos, pero faltan los controladores HTTP, el job de expiración de Hangfire, el almacenamiento/PDF real y la prueba de extremo a extremo de las secciones 8–10.
- **RF-16 (aceptación o rechazo formal):** la máquina de estados, los handlers y la publicación de eventos existen, pero faltan la API HTTP, la autorización de dueño/coordinador y la aceptación de extremo a extremo.
- **RF-17 (firma de compromiso):** la carga, revisión y eventos están implementados a nivel de aplicación, pero faltan la API multipart, el almacenamiento real y la compuerta completa de desembolsos.

Las tareas `7.1` a `7.9` quedaron marcadas porque su implementación y verificación con WireMock/Testcontainers están completas dentro del alcance de la sección 7. Los RF permanecen pendientes hasta cerrar las secciones 8–10.

## 7. Decisiones y riesgos

- No se sincronizaron los deltas de `ms-beca/fundacion-servicio` ni `ms-beca/otorgamiento-beca`: el cambio activo sigue incompleto y este histórico no altera sus specs.
- La reserva en `MS-PRG` ocurre antes de la escritura local. Si la persistencia falla después de una reserva exitosa, la reserva puede quedar huérfana; el TTL de 5 minutos y los eventos de cierre la liberan, y la reconciliación posterior debe cubrir el residuo.
- El despacho del Outbox depende de los servicios hospedados de MassTransit. En pruebas se usa `IHost`; en producción el worker debe estar activo para entregar los mensajes.
- El control de orden por `occurredAt` ignora eventos anteriores o iguales. Una reemisión debe llevar una marca temporal posterior; de lo contrario, la proyección no retrocede.
- La caché de *Service Tokens* es local al proceso; cada réplica solicita su propio token. Es aceptable para este corte y evita una dependencia distribuida prematura.
- `OtpClient` no se invoca todavía desde los casos de uso; su contrato se verificó con WireMock.
- Si la mensajería se deshabilita, los handlers fallan explícitamente al encolar; se prefiere el fallo visible a perder eventos.
- Un payload de entrada inválido agota los reintentos y termina en la cola de error; requiere intervención operativa.
- El secreto de Keycloak se inyecta por configuración o variable de entorno y nunca se registra en logs.

## 8. Estado y próximos pasos

- El cambio activo `tech-bootstrap-int4-msbeca` permanece en `openspec/changes/` con 69/114 tareas.
- Las secciones 8–14 siguen pendientes; la siguiente es la sección 8 (API HTTP).
- Este histórico no mueve ni archiva el cambio activo, no sincroniza specs y no marca RF-15/16/17.
- El código de la sección 7 está comprometido en `d54681f`; este registro histórico se crea después y queda sin commit.
