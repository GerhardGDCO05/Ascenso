# API de desembolsos de MS-BEC

Todos los comandos exigen `Idempotency-Key`, un token con rol
`ADMIN_FINANCIERO` o `COORDINADOR_PROGRAMA`, segundo factor de Keycloak y alcance
sobre el programa de la beca. Las consultas exigen las mismas autorizaciones.
Los errores usan `application/problem+json` con `code` y `correlationId`.

| Operación | Ruta | Respuesta correcta |
| :--- | :--- | :--- |
| Programar calendario | `POST /api/v1/becas/{becaId}/calendarios-desembolso` | `201` |
| Consultar calendarios | `GET /api/v1/becas/{becaId}/calendarios-desembolso` | `200` |
| Actualizar datos de pago | `POST /api/v1/becas/{becaId}/datos-pago` | `201` |
| Consultar cuotas | `GET /api/v1/becas/calendarios-desembolso/{calendarioId}/cuotas` | `200` |
| Registrar ejecución | `POST /api/v1/becas/calendarios-desembolso/{calendarioId}/cuotas/{numeroCuota}/ejecucion` | `200` |
| Registrar incidencia | `POST /api/v1/becas/calendarios-desembolso/{calendarioId}/cuotas/{numeroCuota}/incidencias` | `201` |
| Resolver incidencia | `POST /api/v1/becas/calendarios-desembolso/{calendarioId}/cuotas/{numeroCuota}/incidencias/{incidenciaId}/resolver` | `200` |
| Reintentar cuota | `POST /api/v1/becas/calendarios-desembolso/{calendarioId}/cuotas/{numeroCuota}/reintentar` | `200` |
| Confirmar cuota | `POST /api/v1/becas/calendarios-desembolso/{calendarioId}/cuotas/{numeroCuota}/confirmar` | `503` hasta integrar el movimiento idempotente en MS-PRG (16.10) |

El cambio de datos de pago recibe `codigoOtp` en el cuerpo y
`X-Autorizacion-OTP-Id` con el UUID de la autorización en el encabezado. La
confirmación acepta la misma cabecera y un cuerpo con `codigoOtp`, pero no
ejecuta ni consume el OTP hasta completar 16.10. Una autorización ausente o
inválida produce `400`. La ruta de confirmación devuelve
`503 DEPENDENCIA_NO_DISPONIBLE` sin modificar datos locales.

Los siguientes códigos de enumeración se usan en los DTO de MS-BEC. Salvo
`EstadoDesembolso`, son decisiones locales: su compatibilidad con los
consumidores aún requiere revisión.

| Enumeración | Códigos |
| :--- | :--- |
| `FrecuenciaDesembolso` | `MENSUAL`, `SEMESTRAL` |
| `DestinatarioPago` | `BECARIO`, `INSTITUCION` |
| `MetodoPago` | `TRANSFERENCIA_BANCARIA` |
| `EstadoPlanDesembolso` | `PROGRAMADO`, `SUSPENDIDO`, `COMPLETADO`, `CANCELADO` |
| `EstadoDatosPago` | `ACTIVO`, `REEMPLAZADO` |
| `EstadoDesembolso` | `PROGRAMADO`, `EN_PROCESO`, `PAGADO_CONFIRMADO`, `FALLIDO`, `DEVUELTO`, `SUSPENDIDO`, `CANCELADO` |
| `TipoIncidenciaDesembolso` | `FALLO_BANCARIO`, `DEVOLUCION_BANCARIA` |
| `EstadoIncidencia` | `ABIERTA`, `RESUELTA` |
