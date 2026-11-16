## Tasks

- [x] 7.1 Implementar `PostulacionesClient` para `GET /internal/v1/postulaciones/{id}/adjudicable` con *Service Token*, timeout de 3 s, propagación de correlación y mapeo de `404`/`false`/timeout; verificar con WireMock.
- [x] 7.2 Implementar `ProgramasClient` para `POST /internal/v1/programas/{id}/reservas` con `Idempotency-Key: oferta-{id}`, timeout de 3 s y mapeo de `409 SIN_CUPO_O_PRESUPUESTO` y `422 CONVENIO_VENCIDO`; verificar con WireMock que la reserva nace en `PENDIENTE`, que el reintento usa la misma clave y que los errores se mapean.
- [x] 7.3 Implementar `OtpClient` para `POST /internal/v1/otp/consumir` con el contrato documentado, sin invocarlo aún desde los casos de uso; verificar con WireMock los códigos de éxito, expiración y bloqueo.
- [x] 7.4 Implementar el proveedor de *Service Tokens* con *client credentials* de Keycloak, caché en memoria y renovación antes de expirar; verificar con un servidor OIDC simulado que no solicita un token nuevo en cada llamada.
- [x] 7.5 Configurar MassTransit con RabbitMQ, exchange/queue naming, reintentos, cola de error, *Transactional Outbox* e *Idempotent Inbox*; verificar con Testcontainers que un mensaje publicado se persiste en Outbox y llega al consumidor.
- [x] 7.6 Implementar los consumidores de `ProgramaBecaConfigurado`, `CostoMatriculaActualizado`, `ConvenioUniversidadRegistrado` y `ConvenioUniversidadVencido` con upsert idempotente y control de `occurredAt`/`ultimo_evento_id`; verificar que un evento duplicado o desactualizado no retrocede la proyección.
- [x] 7.7 Definir los contratos de eventos de salida (`OfertaBecaEmitida`, `BecaActivada`, `OfertaBecaRechazada`, `OfertaBecaAnulada`, `OfertaBecaExpirada`, `CompromisoBecarioCargado`, `CompromisoBecarioAprobado`, `CompromisoBecarioRechazado`) con el sobre `EventoIntegracion<T>`; verificar con pruebas de serialización que no incluyen PII.
- [x] 7.8 Implementar la publicación de los eventos de salida desde los handlers y verificar que cada transición publica exactamente el evento correspondiente.
- [x] 7.9 Implementar una métrica y alerta de configuración faltante (programa, costo o convenio) y documentar el procedimiento para que `MS-PRG` reemita el evento; verificar que la emisión se bloquea con `422 CONFIGURACION_PROGRAMA_NO_DISPONIBLE`.
- [x] Verificar build Release con `--warnaserror`, suite completa, formato, restore bloqueado, migraciones sin cambios pendientes y validación de OpenSpec.
- [x] Ajustar la prueba de Outbox/Inbox para arrancar los servicios hospedados de MassTransit mediante `IHost`.
- [x] Alinear la nomenclatura de los campos estáticos privados con `.editorconfig` y dejar `dotnet format --verify-no-changes` limpio.
- [x] Crear `Ascenso.Becas.UnitTests.slnf` para ejecutar las pruebas unitarias locales sin la advertencia de VSTest.
- [x] Registrar este histórico de sesión sin mover ni archivar el cambio activo `tech-bootstrap-int4-msbeca`.
