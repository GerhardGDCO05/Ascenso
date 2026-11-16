## Why

La sección 7 del cambio `tech-bootstrap-int4-msbeca` implementa los clientes de integración con `MS-POS`, `MS-PRG` y `MS-PLT`, y la mensajería Outbox/Inbox que conecta a `MS-BEC` con el resto del ecosistema. Sin esta sección, la emisión de ofertas no podía consultar adjudicabilidad ni reservar cupo/presupuesto con garantías de idempotencia, los eventos de otorgamiento no se publicaban de forma transaccional y las proyecciones de configuración de `MS-PRG` no se actualizaban.

Este histórico documenta y conserva el trabajo de la sección 7 **sin archivar el cambio activo**, que todavía tiene pendientes las secciones 8–14. Los deltas de `ms-beca/fundacion-servicio` y `ms-beca/otorgamiento-beca` permanecen en el cambio activo y no se sincronizan aquí.

## What Changes

- Implementar `PostulacionesClient` para consultar `GET /internal/v1/postulaciones/{id}/adjudicable` con *Service Token*, timeout de 3 s, propagación de `X-Correlation-Id` y mapeo de `404`, `false` y timeout.
- Implementar `ProgramasClient` para `POST /internal/v1/programas/{id}/reservas` con `Idempotency-Key: oferta-{id}`, timeout de 3 s y mapeo de `409 SIN_CUPO_O_PRESUPUESTO` y `422 CONVENIO_VENCIDO`.
- Implementar `OtpClient` para `POST /internal/v1/otp/consumir` con el contrato documentado, sin invocarlo todavía desde los casos de uso.
- Implementar el proveedor de *Service Tokens* con *client credentials* de Keycloak, caché en memoria, renovación anticipada e invalidación ante `401`.
- Configurar MassTransit con RabbitMQ, *Transactional Outbox*, *Idempotent Inbox*, reintentos exponenciales, cola de error y nombres estables de intercambios y colas.
- Implementar los consumidores de `ProgramaBecaConfigurado`, `CostoMatriculaActualizado`, `ConvenioUniversidadRegistrado` y `ConvenioUniversidadVencido` con upsert idempotente y control de `occurredAt`/`ultimo_evento_id`.
- Definir el sobre `EventoIntegracion<T>`, ocho contratos de salida y cuatro de entrada, sin datos personales.
- Publicar los eventos de salida desde los siete handlers de otorgamiento antes de `SaveChanges`, dentro de la transacción local.
- Implementar la métrica `becas.configuracion_faltante{recurso}` y documentar el procedimiento de reemisión desde `MS-PRG`.
- Incorporar pruebas con WireMock, Testcontainers de PostgreSQL/RabbitMQ y pruebas de serialización de eventos.

## Capabilities

### New Capabilities

- Ninguna. El trabajo es integración y mensajería dentro de `MS-BEC`.

### Modified Capabilities

- Ninguna en `openspec/specs/`. Los deltas de `ms-beca/fundacion-servicio` y `ms-beca/otorgamiento-beca` siguen perteneciendo al cambio activo `tech-bootstrap-int4-msbeca` y no se sincronizan en este histórico.

## Impact

- Código afectado: `Apps/services/ms-becas/Ascenso.Becas.Application/IntegracionEventos/`, `Otorgamiento/Puertos/` y los handlers de otorgamiento; `Ascenso.Becas.Infrastructure/Clientes/`, `Mensajeria/`, `Metricas/` y `Dependencias/`; `Ascenso.Becas.Api/Program.cs` y `appsettings.json`.
- Base de datos: sin migraciones nuevas; se reutilizan `becas.outbox_message`, `becas.outbox_state` y `becas.inbox_state` creadas en la migración inicial.
- Configuración: nuevas secciones `Keycloak:ServiceToken`, `Clientes`, `RabbitMq` y `Mensajeria` en `appsettings.json`.
- Pruebas: la suite completa quedó en 480/480 correctas y el build Release con `--warnaserror` en 0 errores y 0 advertencias.
- Fuera de alcance: secciones 8–14 del cambio activo, controladores HTTP, jobs de Hangfire, almacenamiento real, uso del OTP desde casos de uso y sincronización de specs principales.
