# Identidad e integraciones HTTP de MS-BEC

> **Estado:** propuesta de contrato basada en la implementación de MS-BEC.
> La confirmación de los equipos propietarios de MS-PLT, MS-POS y MS-PRG está
> pendiente; esta documentación no implica cambios en esos microservicios.

## Token de usuario y claims

La API valida access tokens JWT de Keycloak. Los nombres de claims son
configurables en `Autorizacion`; los valores predeterminados que consume MS-BEC
son:

| Claim | Tipo/uso | Responsable |
| :--- | :--- | :--- |
| `roles` | Uno o más roles de realm. Se usan `POSTULANTE`, `BECARIO`, `COORDINADOR_PROGRAMA` y `DIRECCION`. | Keycloak / MS-PLT |
| `postulante_id` | UUID de la persona propietaria; requerido para autorizar como postulante/becario. | MS-PLT |
| `programa_ids` | Lista de UUIDs de programas habilitados para el coordinador. `DIRECCION` tiene alcance global. | MS-PLT |
| `acr` | Evidencia alternativa de 2FA; por defecto debe ser `urn:ascenso:loa:2fa`. | Keycloak / MS-PLT |
| `amr` | Lista de métodos; por defecto se acepta `otp` o `totp` como alternativa a `acr`. | Keycloak / MS-PLT |

MS-BEC acepta claims multivaluados como valores repetidos, lista separada por
espacios/comas o arreglo JSON. Si falta `postulante_id` o un alcance de
programa, la autorización falla cerrada. La API responde `401
SEGUNDO_FACTOR_REQUERIDO` cuando un rol administrativo no presenta evidencia
de 2FA, y `403 PERMISO_DENEGADO` cuando el usuario no tiene propiedad o
alcance.

La configuración local del realm sirve para pruebas. MS-PLT debe confirmar
los nombres finales, el formato de `programa_ids`, los mappers del access
token, el valor de `acr`/`amr` y la política real de segundo factor.

## Service Token para llamadas internas

MS-BEC obtiene tokens mediante OAuth 2.0 Client Credentials. El cliente
permitido por defecto es `ms-bec-internal`, con el scope `becas.internal.read`
y audiencia `ms-bec`. El receptor interno valida el claim `client_id` y el
scope `scope`; las llamadas no deben usar un token de usuario ni exponer el
secreto del cliente.

## MS-POS: consultar adjudicabilidad

MS-BEC consume:

```http
GET /internal/v1/postulaciones/{postulacionId}/adjudicable
Authorization: Bearer <service-token>
X-Correlation-Id: <uuid>
```

Respuesta `200` esperada:

```json
{
  "postulacionId": "uuid",
  "adjudicable": true,
  "programaBecaId": "uuid",
  "periodoAcademicoId": "uuid",
  "periodoCodigo": "2026-1",
  "universidadAliadaId": "uuid",
  "carreraId": "uuid",
  "postulanteEstudianteId": "uuid",
  "nombreEstudiante": "<nombre>",
  "numeroDocumentoEstudiante": "<documento>",
  "convocatoriaOrigenId": "uuid",
  "motivoNoAdjudicable": null
}
```

`nombreEstudiante` y `numeroDocumentoEstudiante` se usan para generar la carta;
no deben incluirse en eventos ni logs. Respuestas y errores relevantes:

| Respuesta de MS-POS | Tratamiento en MS-BEC |
| :--- | :--- |
| `200`, `adjudicable=true` | Continúa la emisión. |
| `200`, `adjudicable=false` | `422 POSTULACION_NO_ADJUDICABLE`. |
| `404` | `422 POSTULACION_NO_ENCONTRADA`. |
| Timeout, error de red u otra respuesta no exitosa | `503 DEPENDENCIA_NO_DISPONIBLE`. |

El timeout configurado por MS-BEC es de 3 segundos. La respuesta no debe
contener información socioeconómica.

## MS-PRG: reservar cupo y presupuesto

MS-BEC consume:

```http
POST /internal/v1/programas/{programaBecaId}/reservas
Authorization: Bearer <service-token>
Idempotency-Key: oferta-{ofertaId}
X-Correlation-Id: <uuid>
Content-Type: application/json
```

Solicitud:

```json
{
  "referenciaTipo": "OFERTA_BECA",
  "referenciaId": "uuid-oferta",
  "periodoAcademicoId": "uuid-periodo",
  "monto": 2000.00,
  "cupos": 1
}
```

Respuesta `201` esperada:

```json
{
  "reservaId": "uuid-reserva",
  "estado": "PENDIENTE",
  "monto": 2000.00,
  "cupos": 1,
  "expiraEn": "2026-11-22T12:05:00Z"
}
```

La misma oferta reutiliza la misma clave de idempotencia. Errores reconocidos:

| Estado | `codigo`/`code` | Tratamiento |
| :--- | :--- | :--- |
| `409` | `SIN_CUPO_O_PRESUPUESTO` | Rechazo de reserva con `409`. |
| `422` | `CONVENIO_VENCIDO` | Rechazo de reserva con `422`. |
| Timeout, error de red u otra respuesta no exitosa | — | `503 DEPENDENCIA_NO_DISPONIBLE`. |

MS-BEC acepta los campos de error `codigo`/`mensaje` o `code`/`message`.
La reserva nace `PENDIENTE`; su confirmación/activación/liberación posterior
se coordina por los eventos documentados en [eventos](./eventos.md), no por
llamadas síncronas adicionales en este corte.

## Validación pendiente de equipos

| Equipo | Confirmación requerida | Estado |
| :--- | :--- | :--- |
| MS-PLT | Claims, formato de alcance, audiencia y evidencia 2FA del access token. | Pendiente |
| MS-POS | Ruta, campos de adjudicabilidad y errores 404/no adjudicable. | Pendiente |
| MS-PRG | Ruta de reserva, idempotencia, payload, respuesta y errores 409/422. | Pendiente |
