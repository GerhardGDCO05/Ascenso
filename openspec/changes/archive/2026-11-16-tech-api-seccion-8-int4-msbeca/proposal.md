## Why

La sección 8 del cambio `tech-bootstrap-int4-msbeca` expone la superficie HTTP de `MS-BEC`: otorgamiento de ofertas, consulta de becas, carga y revisión de compromisos, consulta interna para otros servicios, errores homogéneos, contrato OpenAPI y health checks. Sin esta sección, los casos de uso de las secciones 5–7 no tenían entrada pública ni operativa para el API Gateway, y no existía una verificación de disponibilidad de PostgreSQL, RabbitMQ y migraciones.

Este histórico documenta y conserva el trabajo de la sección 8 **sin archivar el cambio activo**, que todavía tiene pendientes las secciones 9–14. Los deltas de `ms-beca/fundacion-servicio` y `ms-beca/otorgamiento-beca` permanecen en el cambio activo y no se sincronizan aquí.

## What Changes

- Implementar `OfertasBecaController` con `POST /api/v1/becas/ofertas`, `GET /api/v1/becas/ofertas/{id}`, `POST .../aceptar`, `POST .../rechazar` y `POST .../anular`, con autorización por rol, 2FA y alcance, idempotencia y códigos `201/200/204/400/401/403/404/409/422/503`.
- Implementar `BecasController` con `GET /api/v1/becas/{id}` para el dueño o un coordinador con alcance; responder `200`, `403` o `404`.
- Implementar `CompromisosController` con `POST /api/v1/becas/{id}/compromisos` multipart y `POST /api/v1/becas/compromisos/{id}/revision`; aceptar solo PDF hasta 5 MB, validar MIME/extensión/contenido y exigir dueño `BECARIO` o revisión administrativa con 2FA y alcance.
- Implementar `InternalBecasController` con `GET /internal/v1/becas/activas`; exigir *Service Token* del cliente `ms-bec-internal` y el alcance `becas.internal.read`, y devolver solo identificadores, estado y cobertura, sin PII.
- Homogeneizar `ProblemDetails` y la validación automática de DTOs con `code`, `correlationId`, `errors` y códigos estables para `400`, `401`, `403`, `409` y `422`.
- Publicar OpenAPI en `/swagger/v1/swagger.json` con seguridad Bearer global, `Idempotency-Key` en los `POST` y ejemplos de solicitud/respuesta.
- Configurar `/health/live` y `/health/ready`; readiness verifica PostgreSQL, migraciones pendientes y RabbitMQ, y responde `503` indicando la dependencia afectada.

## Capabilities

### New Capabilities

- Ninguna. La sección 8 expone y asegura capacidades ya definidas en los deltas del cambio activo.

### Modified Capabilities

- Ninguna en `openspec/specs/`. Los deltas de `ms-beca/fundacion-servicio` y `ms-beca/otorgamiento-beca` siguen perteneciendo al cambio activo `tech-bootstrap-int4-msbeca` y no se sincronizan en este histórico.

## Impact

- Código afectado: `Apps/services/ms-becas/Ascenso.Becas.Api/` (controladores, errores, OpenAPI, salud y `Program.cs`), `Ascenso.Becas.Application/Otorgamiento/Dtos/` y `Puertos/IConsultasBeca.cs`, `Ascenso.Becas.Infrastructure/Persistence/Consultas/ConsultasBeca.cs`.
- Base de datos: sin migraciones nuevas; los health checks consultan la conectividad y las migraciones existentes.
- Configuración: se reutilizan `ConnectionStrings:Becas`, `RabbitMq` y `Mensajeria:Habilitada`; no se añaden secretos nuevos.
- Pruebas: 55 pruebas nuevas de integración; la suite completa quedó en 535/535 y el build Release con `--warnaserror` en 0 errores y 0 advertencias.
- Fuera de alcance: secciones 9–14 del cambio activo, jobs de Hangfire, adaptadores reales de PDF/almacenamiento/programador de expiración, sincronización de specs principales y commit.
