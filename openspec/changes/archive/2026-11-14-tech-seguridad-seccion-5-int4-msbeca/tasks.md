## Tasks

- [x] 5.1 Configurar autenticación JWT Bearer contra Keycloak con validación de emisor, audiencia, firma, vigencia, `MapInboundClaims=false`, roles y `RoleClaimType=roles`; verificar con tokens de prueba válidos e inválidos.
- [x] 5.2 Implementar la política `Requiere2FA` que valida `acr` configurable o `amr` con `otp`/`totp`; verificar que los roles administrativos sin segundo factor reciben `401 SEGUNDO_FACTOR_REQUERIDO`.
- [x] 5.3 Implementar la política de alcance por programa (`programa_ids`) y la autorización de propiedad por `postulante_id`; verificar que un coordinador sin alcance recibe `403` y que un postulante no puede operar sobre una oferta ajena.
- [x] 5.4 Implementar la política de *Service Account* para `/internal/v1/...`, con validación de `client_id` permitido y scope `becas.internal.read`; verificar que una llamada sin token o con scope insuficiente recibe `401`/`403`.
- [x] 5.5 Implementar el middleware de idempotencia con hash canónico del payload, almacenamiento transaccional y TTL de 7 días; verificar los casos de reintento exitoso, payload distinto y solicitud en proceso.
- [x] 5.6 Implementar el middleware de `ProblemDetails` y el mapeo de excepciones de dominio a códigos HTTP; verificar cada mapeo con pruebas de API.
- [x] 5.7 Implementar el middleware de correlación que acepte o genere `X-Correlation-Id` y lo propague a logs, clientes HTTP y eventos; verificar que el identificador se conserva en la respuesta.
- [x] 5.8 Implementar la redacción de datos sensibles en logs (OTP, cuentas, documentos, correos); verificar con pruebas que los valores nunca aparecen en la salida.
- [x] Verificar build Release con `--warnaserror`, suite completa, formato, restore bloqueado y validación de OpenSpec.
- [x] Reparar el build de CI tras detectar archivos de la sección 5 que no habían entrado al control de versiones y la referencia JWT faltante.
- [x] Registrar este histórico de sesión sin mover ni archivar el cambio activo `tech-bootstrap-int4-msbeca`.
