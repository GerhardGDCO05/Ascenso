# Registro de Auditoría — Sección 5: seguridad, idempotencia y errores transversales

- **Fecha y hora de ejecución:** 14 de noviembre de 2026, 12:21 (UTC-4)
- **Rama:** `Gerhard`
- **Commit de referencia:** `ead1903` (merge de `origin/develop`)
- **Alcance:** tareas `5.1` a `5.8` del cambio `tech-bootstrap-int4-msbeca`.
- **Microservicio intervenido:** solo `Apps/services/ms-becas/`; `ms-pos` y `ms-plt` intactos.
- **Trabajo local:** sin `git push` ni modificación de ramas remotas.
- **Estado del cambio activo:** 49/114 tareas; secciones 6–14 pendientes. El cambio **no** se movió a `archive/`.

## 1. Objetivo

Cerrar la base transversal de MS-BECA: autenticación y autorización contra Keycloak, idempotencia de comandos, respuestas de error homogéneas, correlación de solicitudes y redacción de datos sensibles en logs. Estas piezas son prerequisito de los casos de uso y controladores de las secciones 6–8.

## 2. Cambios realizados

### 2.1 Autenticación JWT Bearer (5.1)

- `Seguridad/AutenticacionJwt.cs`: configuración del esquema Bearer con autoridad OIDC de Keycloak.
- `Seguridad/JwtOptions.cs`: opciones `Authority`, `MetadataAddress`, `Issuer`, `Audience`, `RequireHttpsMetadata`, `SigningKey`, `RoleClaimType` y `NameClaimType`.
- Validación de firma, emisor, audiencia, vigencia y `RequireExpirationTime`; desfase de reloj de 30 segundos.
- `MapInboundClaims = false` y `RoleClaimType = roles` para conservar el contrato de claims de Keycloak.
- Clave simétrica solo para desarrollo/pruebas; en producción se usa el metadata OIDC con HTTPS obligatorio.

### 2.2 Política `Requiere2FA` (5.2)

- `Seguridad/Requiere2FAHandler.cs`: acepta el `acr` configurado (`urn:ascenso:loa:2fa`) o `otp`/`totp` en `amr`.
- Solo exige segundo factor a roles administrativos (`COORDINADOR_PROGRAMA`, `DIRECCION` por defecto).
- Los roles no administrativos satisfacen el requisito para que un rechazo por rol produzca `403 PERMISO_DENEGADO` y no `401 SEGUNDO_FACTOR_REQUERIDO`.
- `Seguridad/AutorizacionResultadoHandler.cs`: traduce el fallo a `ProblemDetails` con `401 SEGUNDO_FACTOR_REQUERIDO` o `403 PERMISO_DENEGADO`.
- `Seguridad/AutorizacionApi.cs`, `AutorizacionOptions.cs`, `RequisitosAutorizacion.cs` y `PoliticasSeguridad.cs`: registro y configuración de políticas.

### 2.3 Alcance por programa y propiedad del postulante (5.3)

- `Seguridad/IRecursoAutorizable.cs`: contrato de recurso con `ProgramaId` y `PostulanteId`.
- `Seguridad/AlcanceProgramaHandler.cs`: autoriza si el programa está en `programa_ids`; `DIRECCION` tiene bypass global.
- `Seguridad/PropiedadPostulanteHandler.cs`: autoriza si `postulante_id` coincide con el dueño del recurso.
- Si el recurso no trae el identificador necesario, la decisión es fallar cerrado.
- `Seguridad/LectorClaims.cs`: lectura tolerante de claims como valor único, lista separada por espacios o arreglo JSON.

### 2.4 Service Account interno (5.4)

- `Seguridad/CuentaServicioHandler.cs`: valida `client_id` dentro de la lista permitida (`ms-bec-internal`) y el scope `becas.internal.read`.
- Sin token: `401 NO_AUTENTICADO`. Con token pero sin cliente/scope suficiente: `403 PERMISO_DENEGADO`.
- El alcance se interpreta como valor único, lista separada por espacios o arreglo JSON.

### 2.5 Idempotencia de comandos (5.5)

- `Api/Idempotencia/IdempotenciaMiddleware.cs`: aplica a POST/PUT/PATCH/DELETE bajo `/api/v1`.
- `Idempotency-Key` obligatoria de 8 a 128 caracteres; ausencia o longitud inválida responde `400`.
- `Api/Idempotencia/HashPayload.cs`: SHA-256 del payload canónico. JSON con propiedades ordenadas y sin espacios; multipart con boundary normalizado; otros contenidos con bytes originales.
- `Application/Idempotencia/IAlmacenIdempotencia.cs` y `RegistroIdempotencia.cs`: puerto y contrato del almacén.
- `Infrastructure/Persistence/Repositorios/AlmacenIdempotencia.cs`: `INSERT ... ON CONFLICT DO NOTHING`, transacción compartida, TTL de 7 días y actualización de la respuesta.
- `Infrastructure/Dependencias/RegistroPersistencia.cs`: registro del `BecasDbContext` y del almacén.
- Reintento con el mismo payload: respuesta original + `Idempotent-Replayed: true`.
- Misma clave con payload distinto: `409 IDEMPOTENCY_KEY_REUTILIZADA`.
- Solicitud en proceso: `409 IDEMPOTENCIA_EN_PROCESO` con `Retry-After: 1`.
- Fallo o respuesta 5xx: revierte el registro para permitir reintentos.

### 2.6 `ProblemDetails` y mapeo de excepciones (5.6)

- `Api/Errores/EscritorProblemDetails.cs`: respuestas `application/problem+json` con `status`, `title`, `detail`, `instance`, `code` y `correlationId`.
- `Api/Errores/MapeadorExcepcionesDominio.cs`: excepciones de dominio con su estatus y código; `DbUpdateConcurrencyException` → `409 CONCURRENCY_CONFLICT`; `DbUpdateException` → `409 CONFLICTO_PERSISTENCIA`; `BadHttpRequestException` → `400 SOLICITUD_INVALIDA`; `OperationCanceledException` → `499 OPERACION_CANCELADA`; no controladas → `500 ERROR_INTERNO` sin detalle interno.
- `Api/Errores/ManejadorExcepcionesApi.cs`: `IExceptionHandler` registrado en el pipeline, con logs de correlación y sin filtrar stack traces.

### 2.7 Correlación de solicitudes (5.7)

- `Api/Correlacion/CorrelacionMiddleware.cs`: acepta o genera `X-Correlation-Id`, lo devuelve en la respuesta, lo publica en `TraceIdentifier`, en `HttpContext.Items` y en el ámbito de logs como `CorrelationId`.
- `Api/Correlacion/CorrelacionHttp.cs`: valida el encabezado (máximo 128 caracteres, sin caracteres de control) y aplica fallback a `TraceIdentifier` o GUID.
- `Api/Correlacion/CorrelacionContexto.cs` y `Application/Correlacion/ICorrelacionContexto.cs`: contexto para trabajos en segundo plano y eventos.
- `Application/Correlacion/CorrelacionConstantes.cs`: nombre del encabezado, clave de `Items` y longitud máxima.
- `Infrastructure/Clientes/CorrelacionDelegatingHandler.cs`: propaga el identificador en llamadas HTTP salientes.

### 2.8 Redacción de datos sensibles en logs (5.8)

- `Api/Logging/RedaccionDatosSensibles.cs`: detección normalizada (sin mayúsculas, espacios, guiones ni acentos) de OTP, cuentas/IBAN/CBU, documentos/cédula/DNI/pasaporte y correos.
- `Api/Logging/RedaccionLogEventEnricher.cs`: redacta propiedades escalares sensibles con `[REDACTADO]`.
- `Api/Logging/RedaccionDestructuringPolicy.cs`: redacta propiedades sensibles de objetos, incluidas las anidadas, y conserva las no sensibles.
- `Api/Program.cs`: Serilog con enricher, destructuring policy y consola con cultura invariante.

## 3. Archivos principales

- `Apps/services/ms-becas/Ascenso.Becas.Api/Seguridad/`: `AutenticacionJwt.cs`, `JwtOptions.cs`, `AutorizacionApi.cs`, `AutorizacionOptions.cs`, `AutorizacionResultadoHandler.cs`, `Requiere2FAHandler.cs`, `AlcanceProgramaHandler.cs`, `PropiedadPostulanteHandler.cs`, `CuentaServicioHandler.cs`, `IRecursoAutorizable.cs`, `LectorClaims.cs`, `PoliticasSeguridad.cs`, `RequisitosAutorizacion.cs`.
- `Apps/services/ms-becas/Ascenso.Becas.Api/Idempotencia/`: `IdempotenciaMiddleware.cs`, `HashPayload.cs`, `IdempotenciaConstantes.cs`.
- `Apps/services/ms-becas/Ascenso.Becas.Api/Errores/`: `EscritorProblemDetails.cs`, `MapeadorExcepcionesDominio.cs`, `ManejadorExcepcionesApi.cs`.
- `Apps/services/ms-becas/Ascenso.Becas.Api/Correlacion/`: `CorrelacionMiddleware.cs`, `CorrelacionHttp.cs`, `CorrelacionContexto.cs`.
- `Apps/services/ms-becas/Ascenso.Becas.Api/Logging/`: `RedaccionDatosSensibles.cs`, `RedaccionLogEventEnricher.cs`, `RedaccionDestructuringPolicy.cs`.
- `Apps/services/ms-becas/Ascenso.Becas.Application/Idempotencia/` y `Application/Correlacion/`: contratos transversales.
- `Apps/services/ms-becas/Ascenso.Becas.Infrastructure/Persistence/Repositorios/AlmacenIdempotencia.cs`, `Infrastructure/Dependencias/RegistroPersistencia.cs` y `Infrastructure/Clientes/CorrelacionDelegatingHandler.cs`.
- `Apps/services/ms-becas/Ascenso.Becas.Api/Program.cs`, `appsettings.json`, `appsettings.Production.json` y `Ascenso.Becas.Api.csproj`.
- `Apps/services/ms-becas/Directory.Packages.props`: referencia central de `Microsoft.AspNetCore.Authentication.JwtBearer` 10.0.15.

## 4. Pruebas incorporadas

- `Seguridad/AutenticacionJwtTests.cs`: 8 pruebas.
- `Seguridad/Autorizacion2FATests.cs`: 8 pruebas.
- `Seguridad/AutorizacionRecursosTests.cs`: 11 pruebas.
- `Seguridad/AutorizacionCuentaServicioTests.cs`: 9 pruebas.
- `Idempotencia/IdempotenciaMiddlewareTests.cs`: 10 pruebas.
- `Persistencia/AlmacenIdempotenciaTests.cs`: 3 pruebas con PostgreSQL/Testcontainers.
- `Errores/ManejoErroresTests.cs`: 12 pruebas.
- `Correlacion/CorrelacionTests.cs`: 5 pruebas.
- `Logging/RedaccionLogsTests.cs`: 30 pruebas.

**Suite completa:** 359/359 correctas (Application 1, Architecture 8, Domain 136, Integration 214).

## 5. Verificación ejecutada

- `dotnet build Apps/services/ms-becas/Ascenso.Becas.sln --configuration Release --warnaserror`: 0 advertencias, 0 errores.
- `dotnet test Apps/services/ms-becas/Ascenso.Becas.sln --configuration Release`: 359/359 correctas.
- `dotnet restore --locked-mode --force-evaluate`: correcto.
- `dotnet format --verify-no-changes`: limpio.
- `git diff --check`: limpio.
- `openspec validate --all --strict`: correcto.
- `dotnet ef migrations has-pending-model-changes`: sin cambios de modelo.

## 6. Incidente de CI y corrección

El commit inicial `68dc5f9` no incluyó varios archivos nuevos de la sección 5 ni la referencia JWT. El build local en Debug pasaba por artefactos incrementales obsoletos, pero Release en GitHub Actions compilaba desde cero y fallaba con `CS0234`/`CS0246`.

Se reconstruyeron y verificaron los archivos faltantes:

- `Application/Correlacion/ICorrelacionContexto.cs` y `CorrelacionConstantes.cs`.
- `Api/Correlacion/CorrelacionHttp.cs`.
- `Api/Errores/EscritorProblemDetails.cs`.
- `Api/Seguridad/JwtOptions.cs`, `LectorClaims.cs`, `PoliticasSeguridad.cs` y `RequisitosAutorizacion.cs`.

Además se agregó `Microsoft.AspNetCore.Authentication.JwtBearer` 10.0.15 a `Directory.Packages.props` y al `.csproj` de la API, y se actualizaron los `packages.lock.json`. Con ello, el build Release con `--warnaserror` y la suite completa quedaron verdes. Los cambios se incorporaron a la rama mediante los merges `c053f65` y `ead1903`.

## 7. Decisiones y riesgos

- No se sincronizaron los deltas de `ms-beca/fundacion-servicio` ni `ms-beca/otorgamiento-beca`: el cambio activo sigue incompleto y `fundacion-servicio` incluye requisitos aún no implementados.
- La autorización basada en recurso exige que los controladores pasen `IRecursoAutorizable`; si lo omiten, la decisión es denegar.
- La detección de datos sensibles por nombre de propiedad puede generar falsos positivos; se prioriza no filtrar información.
- La clave simétrica JWT es exclusiva de desarrollo/pruebas.
- La transacción de idempotencia debe abarcar el caso de uso completo para que el registro y los efectos sean atómicos.

## 8. Estado y próximos pasos

- El cambio activo `tech-bootstrap-int4-msbeca` permanece en `openspec/changes/` con 49/114 tareas.
- Secciones pendientes: 6 (aplicación y casos de uso), 7 (clientes de integración), 8 (API HTTP), 9 (Hangfire), 10 (archivos y compromiso), 11 (observabilidad), 12 (pruebas), 13 (entorno y CI) y 14 (documentación y cierre).
- El archivado definitivo del cambio requiere las 114 tareas en `[x]`, su bitácora y la sincronización de los deltas.
- Próximo paso: implementar la sección 6, previa autorización.

## 9. Referencias

- Cambio activo: `openspec/changes/tech-bootstrap-int4-msbeca/`.
- Deltas: `specs/ms-beca/fundacion-servicio/spec.md` y `specs/ms-beca/otorgamiento-beca/spec.md`.
- Histórico previo de la sección 4: `openspec/changes/archive/2026-11-12-tech-persistencia-seccion-4-2-4-8-int4-msbeca/`.
