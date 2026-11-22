## Contexto

La observabilidad es transversal al ciclo de otorgamiento y debe permitir correlacionar solicitudes y trabajos asíncronos, detectar fallos y medir el estado del proceso sin registrar datos sensibles. Esta sección implementa en `MS-BEC` la tarea 11 del cambio activo `tech-bootstrap-int4-msbeca`, vinculada a RF-18/RF-35.

## Decisiones

### Logs estructurados y redacción (11.1)

- Serilog emite JSON compacto e incorpora nombre del servicio, entorno, `CorrelationId` y contexto de traza cuando existe.
- La redacción aplica a propiedades sensibles de eventos y a datos estructurados; las pruebas cubren correo, cadenas de conexión, contraseñas y secretos.
- La configuración se centraliza en `ConfiguracionSerilog` y se conecta desde `Program.cs`.

### Trazas y métricas técnicas (11.2)

- `RegistroTelemetria` configura recursos de servicio, instrumentación de ASP.NET Core y HttpClient, y las fuentes/medidores de Npgsql y MassTransit.
- El exportador OTLP es opcional y configurable por endpoint, protocolo e intervalo.
- `TelemetriaHangfire` crea actividades para los trabajos, y el pipeline filtra las rutas de salud de la instrumentación de solicitudes.
- Las opciones se leen desde `Observabilidad:Telemetria`; los valores inválidos del endpoint se reportan explícitamente.

### Métricas de negocio (11.3)

- `IMetricasOtorgamiento` separa la aplicación del proveedor de métricas.
- `MetricasOtorgamiento` publica contadores para ofertas emitidas, aceptadas, rechazadas y expiradas.
- Los gauges de compromisos pendientes y mensajes Outbox pendientes se actualizan desde un publicador periódico que consulta `IContadorPendientesOtorgamiento`.
- Las métricas se registran desde los casos de uso y los trabajos correspondientes.

### Protección de información operativa (11.4)

- La configuración de logs redacta cadenas de conexión, credenciales, tokens y otros valores sensibles.
- Las respuestas de salud aplican el comportamiento previsto para producción sin exponer detalles internos.
- Se agregaron pruebas de la configuración Serilog, redacción y respuestas de salud.

## Verificación y límites

- En el cambio activo, las tareas 11.1–11.4 están marcadas como completadas.
- Hay pruebas de integración/unidad asociadas a logging, salud, telemetría y métricas.
- No se volvieron a ejecutar build, formato ni pruebas al crear este histórico; este documento no certifica resultados nuevos.
- Las tareas de integración E2E, mensajería, seguridad y operación de las secciones posteriores siguen perteneciendo al cambio activo.

## Sincronización

No se sincronizan los deltas de `ms-beca/fundacion-servicio` ni `ms-beca/otorgamiento-beca`: abarcan el cambio completo y permanecen bajo `openspec/changes/tech-bootstrap-int4-msbeca/`. Este histórico no modifica las especificaciones principales.
