## Context

La sección 8 se implementó sobre la fundación de las secciones 1–7. Su objetivo fue exponer la API HTTP pública e interna con autorización por recurso, idempotencia, errores homogéneos, contrato OpenAPI y health checks. Las secciones 9–14 continúan en el cambio activo y no forman parte de este histórico.

## Decisions

### Superficie HTTP y controladores

- `OfertasBecaController` expone cinco rutas: emisión (`POST /api/v1/becas/ofertas`), consulta (`GET /api/v1/becas/ofertas/{ofertaId}`), aceptación, rechazo y anulación.
- La emisión y la anulación exigen un rol administrativo (`COORDINADOR_PROGRAMA` o `DIRECCION`), segundo factor y alcance sobre el programa de la postulación u oferta.
- La aceptación y el rechazo exigen que el postulante autenticado sea el dueño de la oferta.
- La consulta permite al dueño o a un coordinador con alcance; si el recurso no existe responde `404` y si existe pero no está autorizado responde `403`.
- Todos los `POST` bajo `/api/v1` requieren `Idempotency-Key` de 8 a 128 caracteres; el middleware existente devuelve la respuesta original en reintentos con la misma clave y payload.
- Los errores de reglas de negocio se traducen a códigos estables: `409` para conflictos de estado, `422` para postulaciones no adjudicables y `503` para dependencias externas no disponibles.

### Consulta de becas y autorización por recurso

- `BecasController` expone `GET /api/v1/becas/{becaId}` y devuelve la vista de lectura de la beca.
- Antes de consultar el DTO completo se obtiene un `RecursoBecaDto` mínimo con `becaId`, `postulanteId` y `programaBecaId`; así la autorización no carga datos personales ni bancarios.
- El dueño accede por `postulante_id`; el coordinador accede si el programa está dentro de su alcance. Cualquier otro caso responde `403`.
- El puerto `IConsultasBeca` incorpora consultas de recursos mínimos para oferta, beca y compromiso, reutilizadas por los controladores y por las políticas de autorización.

### Carga y revisión de compromisos

- `CompromisosController` recibe el archivo por multipart/form-data y limita el tamaño en el controlador a `5 * 1024 * 1024` bytes antes de procesarlo.
- La validación se refuerza en el validador de aplicación y en el handler: MIME `application/pdf`, extensión `.pdf` y verificación del contenido real.
- La carga exige que el solicitante sea el `BECARIO` dueño de la beca.
- La revisión administrativa exige rol administrativo, segundo factor y alcance sobre el programa; aprueba o rechaza el compromiso y deja la beca habilitada o no para desembolso.
- Un archivo inválido responde `400 ARCHIVO_COMPROMISO_INVALIDO`; un recurso ajeno responde `403`; un compromiso inexistente responde `404`.

### Consulta interna de becas activas

- `InternalBecasController` expone `GET /internal/v1/becas/activas` con filtros obligatorios `postulanteId` y `periodoId`.
- La política `ServiceAccountInterno` exige un token de servicio del cliente `ms-bec-internal` y el alcance `becas.internal.read`; sin token responde `401`, con token sin alcance o cliente no permitido responde `403`.
- `BecaActivaDto` solo expone identificadores, estado, modalidad y montos de cobertura; no incluye nombre, documento, cuenta, banco, correo ni teléfono.
- La consulta se resuelve con `IQueryDispatcher` y `ConsultarBecasActivasQuery`, sin acoplarse a la persistencia.

### ProblemDetails y validación automática

- `EscritorProblemDetails.Crear` construye la respuesta homogénea con `status`, `title`, `detail`, `instance`, `code` y `correlationId`.
- `ConfiguracionErroresApi.AgregarProblemDetailsDeValidacion` sustituye la respuesta automática de `ModelState` inválido por un `ProblemDetails` con `code: SOLICITUD_INVALIDA` y el diccionario `errors`.
- Las anotaciones de validación de los `record` de request se colocan en los parámetros del constructor. Usar `[property: ...]` provoca una excepción de MVC en tiempo de ejecución porque los metadatos de validación de la propiedad se ignoran.
- Los controladores no duplican la validación de forma y tamaño que ya cubren los validadores de aplicación; solo mantienen guardas tempranas de tamaño de archivo y de parámetros de ruta.

### OpenAPI

- Se usa el OpenAPI nativo de .NET 10 con `AddOpenApi` y transformadores de documento, operación y esquema.
- El documento se publica en `/swagger/v1/swagger.json` y se mantiene `/openapi/v1.json`; solo se expone en `Development`.
- El transformador de documento declara el esquema Bearer de Keycloak y lo aplica globalmente.
- El transformador de operación agrega `Idempotency-Key` a todos los `POST` y ejemplos de solicitud/respuesta en emisión, anulación, revisión y consulta interna.
- Los ejemplos de esquema usan `OpenApiSchema.Examples` porque `OpenApiSchema.Example` está obsoleto en Microsoft.OpenApi 2.13 y el build con `--warnaserror` lo rechaza.
- La ruta correcta es `MapOpenApi("/swagger/{documentName}/swagger.json")`; `"/swagger/{documentName}.json"` produce `/swagger/v1.json` y no el contrato esperado.

### Health checks

- `PostgresHealthCheck` crea un alcance de DI, resuelve `BecasDbContext` y ejecuta `CanConnectAsync` y `GetPendingMigrationsAsync` con un timeout interno de 3 segundos.
- Si PostgreSQL no responde o hay migraciones pendientes, readiness queda `Unhealthy`; el mensaje no incluye cadena de conexión ni credenciales.
- `RabbitMqHealthCheck` abre una conexión TCP al host y puerto configurados cuando `Mensajeria:Habilitada` es `true`. Si la mensajería está deshabilitada, el chequeo queda sano porque RabbitMQ no es una dependencia obligatoria en ese despliegue.
- `/health/live` se mapea con predicado `_ => false` para no ejecutar dependencias; `/health/ready` ejecuta los chequeos etiquetados `ready`.
- `EscritorRespuestaSalud` escribe un JSON con estado, duración y lista de chequeos (nombre, estado, descripción y duración), sin excepciones ni secretos.
- Los endpoints son anónimos y no deben quedar detrás de una política de autorización futura.

### Pruebas de API

- `OfertasBecaApiFactory` usa el esquema de autenticación `TestJwt`, un almacén de idempotencia simulado, MassTransit deshabilitado por configuración y sin servidores de Hangfire.
- El manejador de autenticación de prueba acepta roles, postulante, programas, `acr`, cliente y scopes por encabezados `X-Test-*`.
- Se sustituyen `ICartaOfertaPdfGenerator`, `IAlmacenamientoArchivos` e `IProgramadorExpiracionOfertas` porque todavía no tienen implementación de producción; es una brecha previa, fuera del alcance de la sección 8.

## Risks / Trade-offs

- La configuración de pruebas añadida con `ConfigureAppConfiguration` no afecta a los servicios que `Program.cs` registra leyendo `builder.Configuration` durante el arranque. Para pruebas que necesitan una conexión distinta se deben reemplazar los servicios en `ConfigureTestServices`; la prueba de `503` de PostgreSQL reemplaza `DbContextOptions<BecasDbContext>`.
- Readiness falla mientras haya migraciones pendientes. El despliegue debe aplicar migraciones antes de enviar tráfico; en producción no se migra automáticamente al arrancar.
- El chequeo de RabbitMQ valida disponibilidad TCP, no credenciales ni virtual host. Un fallo de autenticación se detecta al arrancar MassTransit y en los logs, no en el health check.
- Los health checks crean un alcance de base de datos por sonda. El timeout interno es de 3 segundos y el registro de health checks usa 5 segundos; las sondas deben configurarse con márgenes compatibles.
- La consulta de recurso devuelve `404` si no existe y `403` si existe pero no está autorizado; se acepta la revelación mínima de existencia para mantener códigos claros.
- OpenAPI solo se publica en `Development`; en producción el contrato debe distribuirse como artefacto de build o documentación, no desde el proceso.
- La API todavía no puede arrancar en `Development` sin sustituir los adaptadores de PDF, almacenamiento y programador de expiración; las pruebas lo hacen, pero la implementación real queda para las secciones 10–12.
- La sección 8 no se comprometió en git; el árbol de trabajo conserva los cambios y el histórico se crea después, también sin commit.

## Migration Plan

No hay migraciones nuevas. Los health checks reutilizan la base `ascenso_becas` y verifican que las migraciones existentes estén aplicadas. El orden de despliegue es: aplicar migraciones, desplegar la API y habilitar tráfico cuando `/health/ready` responda `200`. El rollback es forward-only; no se elimina el esquema en producción durante una reversión.

## Open Questions

- Ninguna para la sección 8. Las secciones 9–14 continúan en el cambio activo `tech-bootstrap-int4-msbeca`.
