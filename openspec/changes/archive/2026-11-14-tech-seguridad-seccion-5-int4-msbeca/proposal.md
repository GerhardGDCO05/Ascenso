## Why

La sección 5 del cambio `tech-bootstrap-int4-msbeca` cierra la base transversal de seguridad, idempotencia, errores y observabilidad de MS-BECA. Sin estas piezas, los casos de uso de otorgamiento de las secciones 6–8 no podrían exponerse de forma segura, reintentarse sin duplicar efectos ni responder con errores homogéneos.

Este histórico documenta y conserva el trabajo de la sección 5 **sin archivar el cambio activo**, que todavía tiene pendientes las secciones 6–14. Los deltas de `ms-beca/fundacion-servicio` y `ms-beca/otorgamiento-beca` permanecen en el cambio activo y no se sincronizan aquí.

## What Changes

- Configurar autenticación JWT Bearer contra Keycloak con validación de firma, emisor, audiencia, vigencia, `MapInboundClaims=false` y `RoleClaimType=roles`.
- Implementar la política `Requiere2FA` con `acr` configurable o `amr` con `otp`/`totp`, diferenciando `401 SEGUNDO_FACTOR_REQUERIDO` de `403 PERMISO_DENEGADO`.
- Implementar autorización basada en recurso para alcance por programa (`programa_ids`, bypass de `DIRECCION`) y propiedad del postulante (`postulante_id`).
- Implementar la política de *Service Account* para `/internal/v1/...` con `client_id` permitido y scope `becas.internal.read`.
- Implementar idempotencia HTTP con hash canónico, almacenamiento transaccional PostgreSQL y TTL de 7 días.
- Homogeneizar errores con `ProblemDetails` y mapear excepciones de dominio e infraestructura a códigos HTTP estables.
- Propagar `X-Correlation-Id` en respuesta, logs, contexto y clientes HTTP salientes.
- Redactar datos sensibles en logs (OTP, cuentas, documentos y correos) con Serilog.

## Capabilities

### New Capabilities

- Ninguna. El trabajo es infraestructura transversal dentro de `MS-BEC`.

### Modified Capabilities

- Ninguna en `openspec/specs/`. Los deltas de `ms-beca/fundacion-servicio` y `ms-beca/otorgamiento-beca` siguen perteneciendo al cambio activo `tech-bootstrap-int4-msbeca` y no se sincronizan en este histórico.

## Impact

- Código afectado: `Apps/services/ms-becas/Ascenso.Becas.Api/`, `Ascenso.Becas.Application/Idempotencia/`, `Ascenso.Becas.Application/Correlacion/` y `Ascenso.Becas.Infrastructure/`.
- Base de datos: sin migraciones nuevas; se reutiliza `becas.idempotency_request` creada en la sección 4.
- Configuración: nuevas secciones `Jwt` y `Autorizacion` en `appsettings.json`; HTTPS obligatorio para metadata OIDC en producción.
- Pruebas: la suite completa quedó en 359/359 correctas y el build Release con `--warnaserror` en 0 advertencias y 0 errores.
- Fuera de alcance: secciones 6–14 del cambio activo y sincronización de specs principales.
