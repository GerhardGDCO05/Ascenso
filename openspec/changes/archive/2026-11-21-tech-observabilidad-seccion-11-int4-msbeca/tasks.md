## Tareas de la sección 11

- [x] 11.1 Configurar Serilog con salida JSON, enriquecedores de servicio, correlación y redacción; verificar que un log de prueba incluye `CorrelationId` y no incluye datos sensibles.
- [x] 11.2 Configurar OpenTelemetry para ASP.NET Core, HttpClient, Npgsql, MassTransit y Hangfire, con exportador OTLP configurable; verificar con un exportador de prueba que se generan trazas y métricas.
- [x] 11.3 Agregar métricas de negocio (`ofertas_emitidas`, `ofertas_aceptadas`, `ofertas_rechazadas`, `ofertas_expiradas`, `compromisos_pendientes`, `outbox_pendientes`); verificar que se incrementan tras las operaciones correspondientes.
- [x] 11.4 Configurar logs y health checks para no exponer cadenas de conexión, secretos ni stack traces en producción; verificar con una prueba de configuración `Production`.
- [x] Registrar este histórico de la sección 11 sin mover ni archivar el cambio activo `tech-bootstrap-int4-msbeca`.
- [x] Mantener los deltas de especificación y las tareas de las secciones restantes en el cambio activo, sin sincronizar specs principales.
