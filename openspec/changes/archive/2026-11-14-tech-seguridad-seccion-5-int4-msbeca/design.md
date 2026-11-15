## Context

La sección 5 se implementó sobre la fundación de dominio y persistencia de las secciones 1–4. El objetivo fue dejar listas las preocupaciones transversales que consumirán los casos de uso y los controladores de las secciones 6–8, sin introducir dependencias circulares ni acoplar `Domain` a ASP.NET Core, Keycloak o Serilog.

## Decisions

### Autenticación JWT

- Se usa `Microsoft.AspNetCore.Authentication.JwtBearer` con autoridad OIDC de Keycloak.
- `MapInboundClaims = false` conserva los nombres originales de los claims (`roles`, `postulante_id`, `programa_ids`, `acr`, `amr`, `client_id`, `scope`).
- `RoleClaimType = roles` y `NameClaimType = preferred_username`.
- Se validan firma, emisor, audiencia, vigencia y `RequireExpirationTime`; el desfase de reloj se limita a 30 segundos.
- En desarrollo/pruebas se admite una clave simétrica (`Jwt:SigningKey`) para no depender de un Keycloak real; en producción se valida contra el metadata OIDC y `RequireHttpsMetadata = true`.

### Segundo factor

- La política `Requiere2FA` acepta el valor configurable de `acr` (`urn:ascenso:loa:2fa` por defecto) o la presencia de `otp`/`totp` en `amr`.
- Solo los roles configurados como administrativos (`COORDINADOR_PROGRAMA`, `DIRECCION` por defecto) están obligados a acreditar segundo factor.
- Los roles no administrativos satisfacen el requisito para que un rechazo posterior por rol se resuelva como `403 PERMISO_DENEGADO` y no como `401 SEGUNDO_FACTOR_REQUERIDO`.
- El manejador de resultados de autorización traduce el fallo a `ProblemDetails` con el código correspondiente.

### Autorización basada en recurso

- Se define `IRecursoAutorizable` con `ProgramaId` y `PostulanteId`. Los controladores deben pasar el recurso a `IAuthorizationService.AuthorizeAsync`.
- `AlcancePrograma` autoriza si el programa del recurso está en `programa_ids`; `DIRECCION` opera globalmente.
- `PropiedadPostulante` autoriza si `postulante_id` coincide con el dueño del recurso.
- Si el recurso no trae el identificador necesario, la decisión es fallar cerrado (no autorizar).

### Service Account

- La política `ServiceAccountInterno` exige un `client_id` dentro de la lista permitida (`ms-bec-internal` por defecto) y el scope `becas.internal.read`.
- El scope se interpreta de forma tolerante: valor único, lista separada por espacios o arreglo JSON.
- Sin token se responde `401 NO_AUTENTICADO`; con token pero sin cliente/scope suficiente, `403 PERMISO_DENEGADO`.

### Idempotencia

- Aplica a POST, PUT, PATCH y DELETE bajo `/api/v1`.
- Exige `Idempotency-Key` de 8 a 128 caracteres; su ausencia o longitud inválida responde `400`.
- El hash es SHA-256 del payload canónico: JSON con propiedades ordenadas y sin espacios; para multipart se normaliza el boundary; para otros contenidos se usan los bytes originales.
- La clave se registra con `INSERT ... ON CONFLICT DO NOTHING` y una transacción compartida con el caso de uso.
- TTL de 7 días; un registro expirado se elimina y la solicitud se ejecuta de nuevo.
- Reintento con el mismo payload: se replica la respuesta y se agrega `Idempotent-Replayed: true`.
- Misma clave con payload distinto: `409 IDEMPOTENCY_KEY_REUTILIZADA`.
- Solicitud aún en proceso: `409 IDEMPOTENCIA_EN_PROCESO` con `Retry-After: 1`.
- Si el caso de uso falla o responde 5xx, se revierte el registro para no bloquear reintentos legítimos.

### ProblemDetails

- `EscritorProblemDetails` produce `application/problem+json` con `status`, `title`, `detail`, `instance`, `code` y `correlationId`.
- Las excepciones de dominio conservan su estatus y código (`DomainException`).
- `DbUpdateConcurrencyException` → `409 CONCURRENCY_CONFLICT`; `DbUpdateException` → `409 CONFLICTO_PERSISTENCIA`.
- `BadHttpRequestException` → `400 SOLICITUD_INVALIDA`; `OperationCanceledException` → `499 OPERACION_CANCELADA`.
- Los errores no controlados → `500 ERROR_INTERNO` sin filtrar detalles internos.

### Correlación

- `X-Correlation-Id` se acepta si tiene hasta 128 caracteres y no contiene caracteres de control; si no, se genera un GUID `N`.
- El identificador se publica en `HttpContext.TraceIdentifier`, en `HttpContext.Items`, en la respuesta y en el ámbito de logs como `CorrelationId`.
- `CorrelacionDelegatingHandler` propaga el identificador en las llamadas HTTP salientes.
- `ICorrelacionContexto` abstrae el valor para trabajos en segundo plano y eventos; fuera de una petición usa `Activity.Current.TraceId` o genera uno nuevo.

### Redacción de logs

- Se usa Serilog con un enricher para propiedades escalares y una destructuring policy para objetos.
- Las claves se normalizan sin mayúsculas, espacios, guiones ni acentos.
- Se redactan OTP, cuentas/IBAN/CBU, documentos/cédula/DNI/pasaporte y correos.
- El reemplazo es `[REDACTADO]`; los valores no sensibles y las propiedades no sensibles de un objeto se conservan.
- La política también se aplica a objetos anidados.

## Risks / Trade-offs

- La detección por nombre de propiedad puede producir falsos positivos (por ejemplo, una propiedad que contenga “documento” pero no sea un dato personal). Se prioriza no filtrar datos sobre la precisión.
- La autorización basada en recurso exige que los controladores pasen el recurso correcto; si se omite, la decisión es denegar.
- La clave simétrica JWT es exclusiva de desarrollo/pruebas; en producción debe quedar vacía.
- La transacción de idempotencia debe abarcar el caso de uso completo; los repositorios posteriores deben unirse a ella.

## Migration Plan

No hay migraciones nuevas. La tabla `becas.idempotency_request` ya existe desde la sección 4. El despliegue solo requiere configurar `Jwt`, `Autorizacion` y las cadenas de conexión existentes.

## Open Questions

- Ninguna para la sección 5. Las secciones 6–14 continúan en el cambio activo.
