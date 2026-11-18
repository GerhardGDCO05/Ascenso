# Registro de Auditoría — Sección 8: API HTTP

- **Fecha y hora de ejecución:** 16 de noviembre de 2026, 23:13 (UTC-4)
- **Rama:** `Gerhard`
- **Commit de la sección:** ninguno; los cambios de la sección 8 están sin commit. El último commit de la rama es `a687d7b` (merge del PR #25).
- **Alcance:** tareas `8.1` a `8.7` del cambio `tech-bootstrap-int4-msbeca`.
- **Microservicio intervenido:** solo `Apps/services/ms-becas/`; los demás servicios quedaron intactos.
- **Trabajo local:** este histórico se crea dentro de `openspec/changes/archive/` y queda sin commit. No se archivó el cambio activo ni se sincronizaron specs principales.
- **Estado del cambio activo:** 76/114 tareas; secciones 1–8 completas y secciones 9–14 pendientes (38 tareas). El cambio **no** se movió a `archive/`.

## 1. Objetivo

Cerrar la superficie HTTP de `MS-BEC`: controladores de otorgamiento, consulta de becas, carga y revisión de compromisos y consulta interna; autorización por recurso con 2FA y alcance; idempotencia; errores homogéneos; contrato OpenAPI con seguridad y ejemplos; y health checks separados de liveness y readiness con PostgreSQL, RabbitMQ y migraciones.

## 2. Cambios realizados

### 2.1 `OfertasBecaController` (8.1)

- Cinco rutas: `POST /api/v1/becas/ofertas`, `GET /api/v1/becas/ofertas/{ofertaId}`, `POST .../aceptar`, `POST .../rechazar` y `POST .../anular`.
- Emisión y anulación con rol administrativo (`COORDINADOR_PROGRAMA` o `DIRECCION`), segundo factor y alcance sobre el programa.
- Aceptación y rechazo reservados al postulante dueño de la oferta; consulta permitida al dueño o a un coordinador con alcance.
- `Idempotency-Key` obligatoria en los `POST` (8 a 128 caracteres); los reintentos con la misma clave y payload devuelven la respuesta original.
- Códigos verificados: `201` al emitir, `200` al consultar/aceptar, `204` al rechazar/anular y `400/401/403/404/409/422/503` para validación, autenticación, permisos, recurso inexistente, conflictos, reglas de negocio y dependencias no disponibles.
- Los recursos se autorizan con consultas mínimas de identificadores (`RecursoOfertaDto`); si el recurso no existe se responde `404` y si existe pero no está autorizado, `403`.

### 2.2 `BecasController` (8.2)

- `GET /api/v1/becas/{becaId}` devuelve la vista de lectura de la beca.
- La autorización usa `RecursoBecaDto` con `becaId`, `postulanteId` y `programaBecaId`; no se cargan datos personales ni bancarios antes de autorizar.
- El dueño accede por `postulante_id`; el coordinador accede si el programa está dentro de su alcance.
- Se verificaron `200`, `403` y `404`.

### 2.3 `CompromisosController` (8.3)

- `POST /api/v1/becas/{becaId}/compromisos` recibe multipart/form-data con el archivo del compromiso.
- El controlador aplica una guarda temprana de tamaño de `5 * 1024 * 1024` bytes; el validador y el handler refuerzan MIME `application/pdf`, extensión `.pdf` y contenido real.
- La carga exige que el solicitante sea el `BECARIO` dueño de la beca.
- `POST /api/v1/becas/compromisos/{compromisoId}/revision` permite aprobar o rechazar con rol administrativo, 2FA y alcance sobre el programa.
- Un archivo inválido responde `400 ARCHIVO_COMPROMISO_INVALIDO`; un recurso ajeno, `403`; un compromiso inexistente, `404`.

### 2.4 `InternalBecasController` (8.4)

- `GET /internal/v1/becas/activas` exige los filtros obligatorios `postulanteId` y `periodoId`.
- La política `ServiceAccountInterno` valida un *Service Token* del cliente `ms-bec-internal` y el alcance `becas.internal.read`.
- Sin token responde `401`; con token sin alcance o cliente no permitido, `403`; sin un filtro obligatorio, `400 SOLICITUD_INVALIDA`.
- `BecaActivaDto` solo expone `becaId`, `programaBecaId`, `periodoAcademicoId`, `estado`, `modalidadCobertura`, `porcentajeCobertura`, `montoFijoPeriodo`, `moneda` y `permiteAcumulacion`.
- Las pruebas verifican que el JSON no contiene nombre, documento, cuenta, banco, correo ni teléfono.
- La consulta se resuelve con `IQueryDispatcher` y `ConsultarBecasActivasQuery`.

### 2.5 `ProblemDetails` y validación automática (8.5)

- `EscritorProblemDetails.Crear` construye la respuesta homogénea con `status`, `title`, `detail`, `instance`, `code` y `correlationId`.
- `ConfiguracionErroresApi.AgregarProblemDetailsDeValidacion` reemplaza la respuesta automática de `ModelState` inválido por un `ProblemDetails` con `code: SOLICITUD_INVALIDA`, `correlationId` y el diccionario `errors`.
- Las anotaciones de validación de los `record` de request se colocan en los parámetros del constructor. `[property: ...]` provoca `InvalidOperationException: Record type ... has validation metadata defined on property ... that will be ignored`.
- Se verificaron respuestas homogéneas para `400`, `401`, `403`, `409` y `422`.

### 2.6 OpenAPI (8.6)

- Se usa el OpenAPI nativo de .NET 10 con `AddOpenApi` y transformadores de documento, operación y esquema.
- El documento se publica en `/swagger/v1/swagger.json` y se mantiene `/openapi/v1.json`; solo se expone en `Development`.
- El documento declara el esquema de seguridad Bearer de Keycloak y lo aplica globalmente.
- Todos los `POST` documentan `Idempotency-Key` con longitud mínima 8 y máxima 128.
- Emisión, anulación, revisión y consulta interna incluyen ejemplos de solicitud y respuesta.
- Los ejemplos de esquema usan `OpenApiSchema.Examples`; `OpenApiSchema.Example` está obsoleto en Microsoft.OpenApi 2.13 y el build con `--warnaserror` lo rechaza.
- La ruta correcta es `MapOpenApi("/swagger/{documentName}/swagger.json")`; `"/swagger/{documentName}.json"` produce `/swagger/v1.json` y no el contrato esperado.
- Las pruebas parsean el documento con `OpenApiDocument.Parse` y verifican rutas, seguridad, idempotencia y ejemplos.

### 2.7 Health checks (8.7)

- `PostgresHealthCheck` crea un alcance de DI, resuelve `BecasDbContext` y ejecuta `CanConnectAsync` y `GetPendingMigrationsAsync` con un timeout interno de 3 segundos.
- Si PostgreSQL no responde o hay migraciones pendientes, readiness queda `Unhealthy`; los mensajes no incluyen cadena de conexión, usuario ni contraseña.
- `RabbitMqHealthCheck` abre una conexión TCP al host y puerto configurados cuando `Mensajeria:Habilitada` es `true`. Si la mensajería está deshabilitada, queda sano porque RabbitMQ no es una dependencia obligatoria en ese despliegue.
- `/health/live` se mapea con predicado `_ => false` para no ejecutar dependencias; `/health/ready` ejecuta los chequeos etiquetados `ready`.
- `EscritorRespuestaSalud` escribe JSON con estado, duración total y lista de chequeos (nombre, estado, descripción y duración), sin excepciones ni secretos.
- Los endpoints son anónimos y no deben quedar detrás de una política de autorización futura.
- La prueba de `503` con PostgreSQL detenido reemplaza `DbContextOptions<BecasDbContext>` en `ConfigureTestServices` y apunta a `127.0.0.1:1`; no depende de una base real en la suite.

## 3. Archivos principales

- `Apps/services/ms-becas/Ascenso.Becas.Api/Controllers/v1/`: `OfertasBecaController.cs`, `BecasController.cs`, `CompromisosController.cs` e `InternalBecasController.cs`.
- `Apps/services/ms-becas/Ascenso.Becas.Api/Errores/`: `ConfiguracionErroresApi.cs` y `EscritorProblemDetails.cs` (modificado).
- `Apps/services/ms-becas/Ascenso.Becas.Api/OpenApi/`: `DocumentoOpenApiTransformer.cs`, `OperacionOpenApiTransformer.cs` y `EjemplosEsquemasOpenApiTransformer.cs`.
- `Apps/services/ms-becas/Ascenso.Becas.Api/Salud/`: `PostgresHealthCheck.cs`, `RabbitMqHealthCheck.cs`, `EscritorRespuestaSalud.cs` y `RegistroSalud.cs`.
- `Apps/services/ms-becas/Ascenso.Becas.Api/Program.cs` y `Seguridad/PoliticasSeguridad.cs` (modificados).
- `Apps/services/ms-becas/Ascenso.Becas.Application/Otorgamiento/Dtos/`: `RecursoOfertaDto.cs`, `RecursoBecaDto.cs` y `RecursoCompromisoDto.cs`.
- `Apps/services/ms-becas/Ascenso.Becas.Application/Otorgamiento/Puertos/IConsultasBeca.cs` (modificado).
- `Apps/services/ms-becas/Ascenso.Becas.Infrastructure/Persistence/Consultas/ConsultasBeca.cs` (modificado).
- `Apps/services/ms-becas/tests/Ascenso.Becas.IntegrationTests/Controllers/`: `OfertasBecaControllerTests.cs`, `BecasControllerTests.cs`, `CompromisosControllerTests.cs`, `InternalBecasControllerTests.cs` y `RespuestasProblemDetailsTests.cs`.
- `Apps/services/ms-becas/tests/Ascenso.Becas.IntegrationTests/OpenApi/OpenApiDocumentoTests.cs`.
- `Apps/services/ms-becas/tests/Ascenso.Becas.IntegrationTests/Salud/SaludTests.cs`.
- `Apps/services/ms-becas/tests/Ascenso.Becas.Application.Tests/Dobles/ConsultasBecaFalsas.cs` (modificado).
- `openspec/changes/tech-bootstrap-int4-msbeca/tasks.md`: tareas `8.1` a `8.7` marcadas.

## 4. Pruebas incorporadas

| Archivo | Pruebas |
|---|---:|
| `Controllers/OfertasBecaControllerTests.cs` | 13 |
| `Controllers/BecasControllerTests.cs` | 7 |
| `Controllers/CompromisosControllerTests.cs` | 11 |
| `Controllers/InternalBecasControllerTests.cs` | 7 |
| `Controllers/RespuestasProblemDetailsTests.cs` | 5 |
| `OpenApi/OpenApiDocumentoTests.cs` | 4 |
| `Salud/SaludTests.cs` | 8 |
| **Total sección 8** | **55** |

**Suite completa:** 535/535 correctas (Architecture 8, Domain 136, Application 95, Integration 296). La sección 7 cerró con 480 pruebas; la sección 8 agregó 55.

Las pruebas de salud cubren: liveness `200` sin ejecutar dependencias, readiness `200` con dependencias sanas, readiness `503` con PostgreSQL no saludable, readiness `503` con PostgreSQL real inalcanzable, RabbitMQ deshabilitado, RabbitMQ aceptando conexiones, RabbitMQ sin respuesta y PostgreSQL directo sin respuesta.

## 5. Verificación ejecutada

- `dotnet restore Apps/services/ms-becas/Ascenso.Becas.sln --locked-mode -p:NuGetAudit=false`: correcto.
- `dotnet build Apps/services/ms-becas/Ascenso.Becas.sln -c Release --no-restore --warnaserror`: 0 errores y 0 advertencias.
- `dotnet format Apps/services/ms-becas/Ascenso.Becas.sln --verify-no-changes --no-restore`: limpio.
- `dotnet test Apps/services/ms-becas/Ascenso.Becas.sln -c Release --no-build`: 535/535 correctas.
- `openspec validate --all --strict`: 7/7 elementos correctos.
- `tasks.md`: 76/114; sección 8 completa (7/7) y 38 tareas pendientes en las secciones 9–14.

## 6. Análisis de HUs del roadmap

No se marcó ningún RF de `MS-BEC` en `openspec/roadmaps/por_microservicio.md` ni en `openspec/roadmaps/por_integrante.md`:

- **RF-15 (emisión de carta/oferta):** la API de emisión, la consulta y los health checks están listos, pero faltan los adaptadores reales de PDF/almacenamiento, los jobs de expiración de Hangfire y las pruebas de extremo a extremo de las secciones 9–12.
- **RF-16 (aceptación o rechazo formal):** la API de aceptación/rechazo y la autorización de dueño están implementadas, pero faltan las pruebas de extremo a extremo y los adaptadores de las secciones 9–12.
- **RF-17 (firma de compromiso):** la carga multipart y la revisión administrativa están implementadas, pero faltan el almacenamiento real, la carta PDF y la compuerta completa de desembolsos.

Las tareas `8.1` a `8.7` quedaron marcadas porque su implementación y verificación HTTP están completas dentro del alcance de la sección 8. Los RF permanecen pendientes hasta cerrar las secciones 9–12.

## 7. Decisiones y riesgos

- No se sincronizaron los deltas de `ms-beca/fundacion-servicio` ni `ms-beca/otorgamiento-beca`: el cambio activo sigue incompleto y este histórico no altera sus specs.
- La autorización por recurso usa consultas mínimas de identificadores; se devuelve `404` si el recurso no existe y `403` si existe pero no está autorizado.
- La carga de compromisos aplica la guarda de 5 MB en el controlador y refuerza MIME, extensión y contenido en validador y handler.
- El endpoint interno no expone PII; el DTO solo contiene identificadores, estado y cobertura.
- En `record` con DataAnnotations, las anotaciones deben ir en el parámetro del constructor; `[property: ...]` lanza una excepción de MVC en tiempo de ejecución.
- `OpenApiSchema.Example` está obsoleto en Microsoft.OpenApi 2.13; se usa `OpenApiSchema.Examples`. La ruta publicada es `/swagger/{documentName}/swagger.json`.
- La configuración de pruebas añadida con `ConfigureAppConfiguration` no afecta a los servicios que `Program.cs` registra leyendo `builder.Configuration` durante el arranque. Para pruebas que necesitan otra conexión se deben reemplazar los servicios; la prueba de `503` reemplaza `DbContextOptions<BecasDbContext>`.
- El chequeo de RabbitMQ valida disponibilidad TCP, no credenciales ni virtual host. Un fallo de autenticación se detecta al arrancar MassTransit y en los logs, no en el health check.
- Readiness falla mientras haya migraciones pendientes; el despliegue debe aplicar migraciones antes de enviar tráfico.
- La API todavía no puede arrancar en `Development` sin sustituir `ICartaOfertaPdfGenerator`, `IAlmacenamientoArchivos` e `IProgramadorExpiracionOfertas`; es una brecha previa y las pruebas los sustituyen.
- Los cambios de la sección 8 no se comprometieron en git; el histórico también queda sin commit.

## 8. Estado y próximos pasos

- El cambio activo `tech-bootstrap-int4-msbeca` permanece en `openspec/changes/` con 76/114 tareas.
- Las secciones 1–8 están completas; la siguiente es la sección 9 (Hangfire y trabajos en segundo plano).
- Las secciones 9–14 siguen pendientes con 38 tareas.
- Este histórico no mueve ni archiva el cambio activo, no sincroniza specs y no marca RF-15/16/17.
- El código de la sección 8 y este registro histórico quedan sin commit.
