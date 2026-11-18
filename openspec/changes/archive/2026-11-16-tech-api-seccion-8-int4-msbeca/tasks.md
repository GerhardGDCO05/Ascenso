## Tasks

- [x] 8.1 Implementar `OfertasBecaController` con las rutas `POST /api/v1/becas/ofertas`, `GET /api/v1/becas/ofertas/{id}`, `POST .../aceptar`, `POST .../rechazar` y `POST .../anular`; verificar que OpenAPI las expone con los códigos de respuesta correctos.
- [x] 8.2 Implementar `BecasController` con `GET /api/v1/becas/{id}` aplicando autorización de dueño o coordinador; verificar `200`, `403` y `404`.
- [x] 8.3 Implementar `CompromisosController` con carga multipart y revisión; verificar que acepta solo PDF hasta 5 MB y rechaza otros tipos con `400`.
- [x] 8.4 Implementar `InternalBecasController` con `GET /internal/v1/becas/activas`, validando *Service Token* y devolviendo solo los campos permitidos; verificar que no expone PII.
- [x] 8.5 Configurar `ProblemDetails`, validación automática de DTOs y respuestas consistentes en todos los controladores; verificar con pruebas de API para `400`, `401`, `403`, `409` y `422`.
- [x] 8.6 Configurar OpenAPI/Swagger con esquemas de seguridad, ejemplos de request/response y documentación de `Idempotency-Key`; verificar que `/swagger/v1/swagger.json` es válido y contiene todas las rutas.
- [x] 8.7 Configurar los health checks `/health/live` y `/health/ready` con PostgreSQL, RabbitMQ y migraciones; verificar `200` con dependencias sanas y `503` con PostgreSQL detenido.
- [x] Verificar restore con `--locked-mode`, build Release con `--warnaserror`, `dotnet format --verify-no-changes`, suite completa y `openspec validate --all --strict`.
- [x] Registrar este histórico de sesión sin mover ni archivar el cambio activo `tech-bootstrap-int4-msbeca`.
