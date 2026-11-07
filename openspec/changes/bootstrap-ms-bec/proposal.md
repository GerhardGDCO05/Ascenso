## Why

`MS-BEC` es el microservicio Core de `BC-05`, `BC-06` (parcial) y `BC-07`, y hoy el repositorio solo contiene la especificación de dominio: no existe solución, base de datos ni código ejecutable. Sin una fundación técnica y una primera rebanada vertical de otorgamiento, `MS-POS` no puede completar el ciclo de adjudicación, `MS-PRG` no puede confirmar reservas de cupo/presupuesto y el equipo no tiene una base sobre la cual construir desembolsos y seguimiento académico.

## What Changes

- Crear la solución .NET `Ascenso.Becas` con arquitectura hexagonal estricta: `Domain`, `Application`, `Infrastructure` y `API`, más proyectos de pruebas.
- Implementar el *shared kernel* de dominio: `EntidadAuditable`, `EntidadInmutable`, identificadores `UUIDv7`, objetos de valor, excepciones y puertos de repositorio.
- Modelar el primer corte del agregado `Beca`: `Beca`, `OfertaBeca`, `CompromisoBecario` e `HistorialEstadoBeca`, con la máquina de estados de la Tabla 3.2 y las invariantes `RN-BEC-01` y `RN-BEC-02`.
- Implementar los casos de uso de otorgamiento: emitir oferta, aceptar, rechazar, anular, expirar a los 15 días y consultar becas activas.
- Exponer la API pública protegida por Keycloak y los endpoints internos `/internal/v1/...` con *Service Tokens* e `Idempotency-Key`.
- Integrar llamadas síncronas *fail-closed* hacia `MS-POS` (postulación adjudicable) y `MS-PRG` (reserva de cupo/presupuesto), y publicar los eventos de otorgamiento vía MassTransit + RabbitMQ con *Transactional Outbox* e *Idempotent Inbox*.
- Mantener proyecciones locales de configuración de programa, costo de matrícula y convenio a partir de los eventos publicados por `MS-PRG`, para tomar las fotografías inmutables de la oferta sin consultas síncronas adicionales.
- Persistir en PostgreSQL `ascenso_becas` con migraciones EF Core, concurrencia optimista, bloqueo pesimista por fila e índices únicos parciales de no doble beneficio.
- Configurar Hangfire (`critical`) para `ExpirarOfertaBecaJob`, observabilidad, health checks y un entorno local con Docker Compose.
- Incorporar pruebas unitarias de dominio, pruebas de integración con Testcontainers y pipeline de CI.

**Fuera de alcance de este cambio (se entregará en cambios posteriores):** `CalendarioDesembolso`, `Desembolso`, `DatosPagoBecario`, `IncidenciaDesembolso`, `SuspensionDesembolso`, revocación definitiva, `ReporteAcademico`, `EvaluacionPermanencia`, `SolicitudRenovacion`, `PausaAplazamiento`, reportería y despliegue productivo.

## Capabilities

### New Capabilities

- `ms-bec/fundacion-servicio`: arranque del microservicio con arquitectura hexagonal, persistencia PostgreSQL, seguridad Keycloak, mensajería Outbox/Inbox, trabajos Hangfire, observabilidad, entorno local y CI.
- `ms-bec/otorgamiento-beca`: ciclo de otorgamiento de la beca (emisión, aceptación, rechazo, anulación y expiración de `OfertaBeca`), compuerta de `CompromisoBecario`, consulta de becas activas y eventos de integración asociados.

### Modified Capabilities

- Ninguna. La especificación maestra `openspec/specs/domain-spec.md` se mantiene como fuente de verdad del dominio.

## Impact

- **Código nuevo:** solución `Ascenso.Becas.sln` en `Apps/services/ms-becas/` y proyectos de pruebas en `Apps/services/ms-becas/tests/`; no se modifica código existente porque el repositorio no contiene implementación.
- **Base de datos:** nueva base `ascenso_becas` con esquema `becas`, tablas de MassTransit Outbox/Inbox y Hangfire.
- **Infraestructura:** RabbitMQ (exchange/colas de `MS-BEC`), PostgreSQL, Keycloak (cliente `ms-bec`, roles y *service accounts*), Firebase Storage para cartas y compromisos, SMTP no requerido en este corte.
- **Integraciones salientes:** `GET /internal/v1/postulaciones/{id}/adjudicable` (`MS-POS`), `POST /internal/v1/programas/{id}/reservas` (`MS-PRG`), `POST /internal/v1/otp/consumir` (`MS-PLT`, solo se deja el puerto y el adaptador listos; se activa en el cambio de revocación).
- **Integraciones entrantes:** `MS-POS` consume `GET /internal/v1/becas/activas`; `MS-POS`, `MS-PLT` y `MS-AUD` consumen los eventos `OfertaBecaEmitida`, `OfertaBecaRechazada`, `OfertaBecaExpirada`, `OfertaBecaAnulada`, `BecaActivada`, `CompromisoBecarioCargado/Aprobado/Rechazado`.
- **Eventos consumidos:** `ProgramaBecaConfigurado`, `CostoMatriculaActualizado`, `ConvenioUniversidadRegistrado` y `ConvenioUniversidadVencido` de `MS-PRG`, para alimentar las proyecciones locales de configuración.
- **Contrato de identidad:** el JWT de Keycloak debe incluir los claims `roles`, `postulante_id`, `programa_ids` y los claims de segundo factor (`acr`/`amr`); `MS-PLT` es responsable de emitirlos.
- **Operación:** workers de Hangfire para expiración de ofertas y jobs de reconciliación de Outbox; trazas, métricas y logs correlacionados por `correlationId`.
