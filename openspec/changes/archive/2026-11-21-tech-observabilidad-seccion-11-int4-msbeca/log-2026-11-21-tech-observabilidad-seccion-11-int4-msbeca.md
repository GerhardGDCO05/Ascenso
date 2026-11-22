# Registro de Auditoría — Sección 11: Observabilidad y operación

- **Fecha y hora de ejecución:** 21 de noviembre de 2026, 22:19 (UTC-4)
- **Rama:** `Gerhard`
- **Commit de la sección:** ninguno; los cambios de implementación de la sección 11 permanecen locales y sin commit.
- **Alcance:** tareas `11.1` a `11.4` del cambio `tech-bootstrap-int4-msbeca`.
- **Microservicio intervenido:** solo `Apps/services/ms-becas/`; no se modificaron los demás microservicios.
- **Trabajo local:** este histórico se crea dentro de `openspec/changes/archive/` y queda sin commit. No se movió ni archivó el cambio activo, ni se sincronizaron sus deltas con las specs principales.
- **Estado del cambio activo:** 97/184 tareas completadas; 87 pendientes. La sección 11 está marcada completa y las secciones siguientes permanecen en el cambio activo.

## 1. Objetivo

Implementar la observabilidad y los controles operativos transversales de `MS-BEC`: logs JSON enriquecidos y correlacionados con redacción de información sensible, telemetría configurable, métricas de negocio y protección de la información expuesta por las respuestas de salud.

## 2. Cambios realizados

### 2.1 Logging y correlación (11.1)

- Se agregó `ConfiguracionSerilog` con salida JSON compacta y propiedades de servicio y entorno.
- `EnriquecedorCorrelacion` incorpora el identificador de correlación HTTP y los identificadores de traza/span cuando están disponibles.
- Se agregaron enriquecedores y políticas de redacción para ocultar propiedades sensibles, incluyendo correo, tokens, secretos y cadenas de conexión.
- Se actualizó `Program.cs` para usar la configuración común de Serilog.
- Se agregaron pruebas de propiedades enriquecidas, formato JSON y redacción de datos sensibles.

### 2.2 Telemetría técnica (11.2)

- `OpcionesTelemetria` lee la configuración de telemetría y exportación OTLP, incluyendo endpoint, protocolo e intervalos.
- `RegistroTelemetria` registra recursos del servicio, instrumentación de ASP.NET Core y HttpClient, y fuentes/medidores de Npgsql y MassTransit.
- `TelemetriaHangfire` instrumenta la ejecución de trabajos.
- Se agregó un exportador de prueba para verificar la generación de trazas y métricas sin depender de un colector externo.

### 2.3 Métricas de negocio (11.3)

- Se agregó el puerto `IMetricasOtorgamiento` y la implementación `MetricasOtorgamiento`.
- Los handlers y trabajos actualizan contadores de ofertas emitidas, aceptadas, rechazadas y expiradas.
- `ContadorPendientesOtorgamiento` consulta compromisos y mensajes Outbox pendientes; `PublicadorMetricasPendientes` actualiza los gauges periódicamente.
- Se agregaron pruebas de métricas de handlers, integración y registro del publicador.

### 2.4 Protección operativa (11.4)

- Se extendió la lista de claves sensibles que deben redactarse.
- Se ajustaron las respuestas de salud para evitar exponer información interna en producción.
- Se agregaron pruebas para validar las respuestas y la configuración de logging.

## 3. Archivos principales

- `Apps/services/ms-becas/Ascenso.Becas.Api/Logging/`: configuración Serilog y enriquecedores.
- `Apps/services/ms-becas/Ascenso.Becas.Api/Observabilidad/`: opciones, registro de telemetría y publicador de métricas pendientes.
- `Apps/services/ms-becas/Ascenso.Becas.Api/Salud/`: respuestas de salud y pruebas relacionadas.
- `Apps/services/ms-becas/Ascenso.Becas.Application/Otorgamiento/Puertos/IMetricasOtorgamiento.cs` y handlers de otorgamiento.
- `Apps/services/ms-becas/Ascenso.Becas.Infrastructure/Metricas/` y `Observabilidad/`: métricas de negocio, contadores y telemetría Hangfire.
- `Apps/services/ms-becas/tests/`: pruebas de logging, salud, telemetría y métricas.
- `openspec/changes/tech-bootstrap-int4-msbeca/tasks.md`: tareas 11.1–11.4 marcadas como completadas.

## 4. Verificación

- El cambio activo marca 11.1–11.4 como completadas y contiene pruebas asociadas a logging, redacción, salud, telemetría y métricas.
- En esta operación no se ejecutaron `dotnet build`, `dotnet format` ni pruebas. Las marcas de tareas reflejan el estado documental previo; este registro no afirma una ejecución nueva.
- El estado de la sección 12 continúa parcialmente pendiente, incluidas las pruebas E2E, RabbitMQ y seguridad.

## 5. Alcance y continuidad

- Este histórico no sincroniza `specs/ms-beca/fundacion-servicio` ni `specs/ms-beca/otorgamiento-beca`; ambos deltas permanecen en el cambio activo.
- No archiva el cambio completo `tech-bootstrap-int4-msbeca`, que conserva 87 tareas pendientes.
- Las secciones 12–22, el entorno local/CI y la publicación de contratos continúan planificados en el cambio activo.
- El histórico y los cambios de implementación quedan locales y sin commit.
