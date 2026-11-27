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
| Confirmar cuota | `POST /api/v1/becas/calendarios-desembolso/{calendarioId}/cuotas/{numeroCuota}/confirmar` | `200` con movimiento confirmado; `503` mientras falte la integración idempotente de MS-PRG (16.10) |

El cambio de datos de pago recibe `codigoOtp` en el cuerpo y
`X-Autorizacion-OTP-Id` con el UUID de la autorización en el encabezado. La
confirmación acepta la misma cabecera y un cuerpo con `codigoOtp`. El caso de
uso comprueba primero la disponibilidad del movimiento contable; el adaptador
productivo actual falla cerrado antes de consumir el OTP o modificar datos
locales. Una autorización ausente o inválida produce `400`. Mientras MS-PRG
no publique el contrato idempotente, la ruta devuelve
`503 DEPENDENCIA_NO_DISPONIBLE`.

Al registrar una ejecución destinada al becario, MS-BEC toma el número de la
cuenta activa cifrada que tiene persistida. Para `INSTITUCION`, exige el
snapshot cifrado de una fuente institucional autorizada: no confía en el valor
enviado por la petición HTTP. Si esa fuente no está disponible, falla cerrado
sin registrar la ejecución. La aceptación de una oferta también exige un plan
oficial de cuotas, frecuencia, destinatario y fechas antes de activar la beca;
el calendario se crea en la misma transacción local y nunca se inventan cuotas.
Un job recurrente busca becas activas sin calendario y reintenta crearlo de
forma idempotente desde el mismo plan oficial cuando la configuración llegue.

Suspensión, reanudación y revocación usan un puerto de movimiento contable con
claves de idempotencia estables. El adaptador productivo actual falla cerrado
antes de cambiar el estado o consumir el OTP. Falta acordar con MS-PRG el
contrato de movimientos y la recuperación duradera entre la respuesta remota
y el commit local.

## Ciclo de vida administrativo

| Operación | Ruta | Respuesta correcta |
| :--- | :--- | :--- |
| Suspender | `POST /api/v1/becas/{becaId}/suspender` | `200` con ID de suspensión |
| Reanudar | `POST /api/v1/becas/{becaId}/reanudar` | `200` con ID de suspensión levantada |
| Revocar | `POST /api/v1/becas/{becaId}/revocar` | `200` |

Las tres rutas requieren `COORDINADOR_PROGRAMA` o `DIRECCION`, 2FA de Keycloak
y alcance sobre el programa. La revocación requiere además
`X-Autorizacion-OTP-Id` y `codigoOtp` en el cuerpo. Sus casos de uso exigen
movimiento contable confirmado; el adaptador actual devuelve `503` sin
transición local hasta acordar MS-PRG. Las rutas y la transmisión del OTP
pueden verificarse con dobles del caso de uso.

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
