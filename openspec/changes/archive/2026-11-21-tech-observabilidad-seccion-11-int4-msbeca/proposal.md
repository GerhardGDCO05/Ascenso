## Why

La sección 11 del cambio `tech-bootstrap-int4-msbeca` incorpora observabilidad y controles operativos transversales para `MS-BEC`: logs estructurados con correlación y redacción de información sensible, telemetría OpenTelemetry configurable y métricas de negocio. Este histórico conserva el trabajo de esa sección sin mover ni cerrar el cambio activo, que todavía contiene tareas pendientes de integración, pruebas, entorno local y documentación.

## What Changes

- Configurar Serilog con salida JSON, enriquecimiento del servicio y la correlación de solicitudes, y redacción de datos sensibles.
- Configurar OpenTelemetry para ASP.NET Core, HttpClient, Npgsql, MassTransit y Hangfire, con exportación OTLP configurable y trazas de los trabajos Hangfire.
- Agregar métricas de negocio para ofertas emitidas, aceptadas, rechazadas y expiradas, además de gauges para compromisos y mensajes Outbox pendientes.
- Proteger la información expuesta por los logs y las respuestas de salud, y agregar pruebas para la configuración y el comportamiento de observabilidad.

## Capabilities

### New Capabilities

- Ninguna. La sección implementa requisitos transversales ya descritos en los deltas del cambio activo.

### Modified Capabilities

- Ninguna en `openspec/specs/`. Los deltas de `ms-beca/fundacion-servicio` y `ms-beca/otorgamiento-beca` permanecen en `tech-bootstrap-int4-msbeca`; este histórico parcial no los sincroniza.

## Impact

- Código afectado: logging y salud en `Ascenso.Becas.Api`; puerto de métricas y handlers de otorgamiento en `Ascenso.Becas.Application`; métricas, telemetría y trabajos en `Ascenso.Becas.Infrastructure`.
- Pruebas afectadas: pruebas unitarias de métricas de handlers y pruebas de integración de Serilog, redacción, telemetría, métricas y respuestas de salud.
- Microservicio intervenido: únicamente `Apps/services/ms-becas/`.
- Fuera de alcance: archivar el cambio completo, sincronizar sus specs, cerrar tareas de la sección 12 en adelante o modificar otros microservicios.
