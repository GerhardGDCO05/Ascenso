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
| `roles` | Uno o más roles de realm. Se usan `POSTULANTE`, `BECARIO`, `ENLACE_UNIVERSIDAD`, `COORDINADOR_PROGRAMA` y `DIRECCION`. | Keycloak / MS-PLT |
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

La carga académica del rol `ENLACE_UNIVERSIDAD` permanece cerrada con `403`:
MS-BEC todavía no dispone de un claim universitario acordado ni de un contrato
para verificar que esa universidad puede reportar sobre la beca concreta.
El identificador enviado en el formulario no prueba esa relación.

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

## MS-POS: marcar la postulación como otorgada

Antes de confirmar localmente una oferta, MS-BEC consume:

```http
POST /internal/v1/postulaciones/{postulacionId}/otorgar
Authorization: Bearer <service-token>
Idempotency-Key: otorgar-{ofertaId}
X-Correlation-Id: <uuid>
```

El endpoint no requiere cuerpo. Una respuesta `2xx` confirma la transición
`EN_EVALUACION → OTORGADA`; `409` y `422` se conservan como rechazos de
negocio y timeout, error de red u otra respuesta no exitosa se traducen a
`503 DEPENDENCIA_NO_DISPONIBLE`. Antes de la llamada remota, MS-BEC persiste
una intención con el mismo `ofertaId` y los datos necesarios para reconstruir
la oferta. La intención se escribe en una conexión independiente de la
transacción HTTP y sobrevive si el commit local falla. Una vez preparada,
MS-BEC conserva los documentos y el job mientras la intención está pendiente.

El finalizador confirma primero la reserva de MS-PRG con la misma identidad de
oferta; solo entonces repite `otorgar-{ofertaId}`, guarda beca y Outbox y
elimina la intención en una transacción local. Un job crítico reintenta las
intenciones por páginas. Si la reserva venció o MS-PRG no confirma, conserva
la intención y no crea la beca. La respuesta idempotente de MS-POS y la
resolución de reservas vencidas requieren validación con los equipos dueños.

## MS-PRG: consultar condiciones vigentes

Para emitir, MS-BEC consume:

```http
GET /internal/v1/programas/{programaBecaId}/condiciones?carreraId={carreraId}&periodoId={periodoId}
Authorization: Bearer <service-token>
X-Correlation-Id: <uuid>
```

Respuesta `200` esperada:

```json
{
  "programaBecaId": "uuid",
  "carreraId": "uuid",
  "periodoAcademicoId": "uuid",
  "universidadAliadaId": "uuid",
  "politicaCobertura": {
    "modalidad": "PARCIAL",
    "porcentajeCobertura": 50.00,
    "montoFijoPeriodo": 0.00,
    "moneda": "USD"
  },
  "condicionesPermanencia": {
    "promedioMinimoRenovacion": 14.00,
    "materiasMinimasAprobadas": 4,
    "maximoMateriasReprobadas": 1,
    "horasServicioPorPeriodo": 80,
    "consecuenciaIncumplimiento": "SUSPENSION"
  },
  "costoMatricula": {
    "costoMatriculaReferenciaId": "uuid",
    "carreraId": "uuid",
    "periodoAcademicoId": "uuid",
    "montoReferenciaMatricula": 1500.00,
    "moneda": "USD",
    "numeroVersion": 2,
    "fechaVigenciaDesde": "2026-01-01",
    "fechaVigenciaHasta": null,
    "estado": "VIGENTE"
  },
  "convenio": {
    "universidadAliadaId": "uuid",
    "convenioId": "uuid",
    "estado": "VIGENTE",
    "fechaFin": "2027-12-31"
  }
}
```

MS-BEC conserva las proyecciones locales como caché de metadatos del programa,
pero utiliza la política, condiciones, arancel y vigencia del convenio
devueltos por esta consulta para crear el snapshot de la oferta. Un `404` se
traduce a `422 CONFIGURACION_PROGRAMA_NO_DISPONIBLE`; una configuración presente
pero incompleta o inválida también produce `422`. `422 CONVENIO_VENCIDO`
se conserva como rechazo; timeout, error de red, JSON malformado o identificadores
incompatibles se traducen a `503 DEPENDENCIA_NO_DISPONIBLE`.

## MS-PLT: consultar el periodo académico actual

Antes de emitir, MS-BEC consume:

```http
GET /internal/v1/periodos-academicos/actual
Authorization: Bearer <service-token>
X-Correlation-Id: <uuid>
```

Respuesta `200` esperada:

```json
{
  "periodoAcademicoId": "uuid",
  "codigo": "2026-2",
  "nombre": "Segundo periodo 2026",
  "ordenSecuencia": 2,
  "fechaInicio": "2026-07-01",
  "fechaFin": "2026-12-31",
  "estado": "ACTIVO",
  "esActual": true
}
```

La respuesta debe identificar un periodo activo y actual. MS-BEC responde
`503 DEPENDENCIA_NO_DISPONIBLE` si la respuesta falta, es inválida, el
proveedor no está disponible o el identificador/código no coincide con los
de la postulación adjudicable.

La URL base se configura en `Clientes.Plataforma.BaseUrl` y debe apuntar a
MS-PLT; no debe asumirse que coincide con la autoridad de Keycloak. La URL
efectiva del entorno sigue pendiente de confirmación.

## Política OTP para otorgamiento

MS-BEC sigue DEC-07 del DDD: emisión y anulación administrativa de ofertas no
requieren consumir una autorización OTP adicional; las operaciones
administrativas continúan protegidas por 2FA de Keycloak. El consumo OTP queda
reservado para las operaciones obligatorias definidas por el DDD.

Para `CONFIRMAR_DESEMBOLSO` y `MODIFICAR_DATOS_PAGO`, MS-BEC consume
`POST /internal/v1/otp/consumir` en MS-PLT. Envía `usuarioId`, `operacion`,
`recursoTipo`, `recursoId`, `huellaPayload` (SHA-256 del contenido de la
operación) y `codigo`. La confirmación usa `recursoTipo = DESEMBOLSO` e ID de
la cuota; el cambio de cuenta usa `recursoTipo = BECA` e ID de la beca. La
API pública recibe el identificador de la autorización en
`X-Autorizacion-OTP-Id` y el código en el cuerpo; el contexto HTTP entrega el
identificador al caso de uso. El endpoint despacha la confirmación, pero el
adaptador productivo del movimiento contable responde `503` antes de consumir
el OTP hasta que MS-PRG garantice el contrato de 16.10. La petición
lleva `Idempotency-Key` derivada del identificador de la autorización OTP,
que distingue operaciones sucesivas sobre la misma beca. MS-BEC no persiste
ni registra el código OTP ni el número de cuenta en claro. El consumo remoto
precede al commit local dentro de la transacción de MS-BEC; ambos servicios
no comparten una transacción distribuida.

La revocación definitiva también exige OTP (`REVOCAR_BECA`, `recursoTipo = BECA`,
`recursoId = becaId`). Su caso de uso consume y valida el código en MS-PLT
bajo bloqueo de la beca, antes de confirmar la transición y la cancelación de cuotas.

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
La reserva nace `PENDIENTE`. Conforme al contrato 6.2 y DEC-08 del DDD, MS-BEC
debe llamar `POST /internal/v1/programas/{programaBecaId}/reservas/{reservaId}/confirmar`,
`/activar` o `/liberar`, según la transición. Cada llamada usa un *Service Token*,
`X-Correlation-Id` e `Idempotency-Key: {operacion}-{ofertaId}`. El cliente HTTP y
sus pruebas de contrato están implementados. La emisión confirma `/confirmar`
desde una intención duradera antes de llamar a MS-POS. Rechazo, anulación y
expiración exigen confirmar `/liberar` antes del cierre local y conservan la
oferta para reintento si falla MS-PRG. La aceptación guarda una intención
duradera con el plan oficial antes de llamar `/activar`; un job reintenta con
la misma clave y confirma beca, calendario y Outbox localmente. Mientras esa
intención esté pendiente, expiración, rechazo, anulación y revocación de la
oferta otorgada no pueden cerrarla. Falta verificar el ciclo y la idempotencia
con MS-PRG real, incluida una reserva que haya vencido antes del reintento.
Los eventos de oferta documentados en [eventos](./eventos.md) comunican
hechos de negocio y no reemplazan estas operaciones REST.

## Validación pendiente de equipos

| Equipo | Confirmación requerida | Estado |
| :--- | :--- | :--- |
| MS-PLT | Claims, formato de alcance, audiencia, evidencia 2FA y respuesta del periodo actual. | Pendiente |
| MS-POS | Rutas/campos de adjudicabilidad y otorgamiento, idempotencia y errores 404/409/422. | Pendiente |
| MS-PRG | Rutas/respuestas de condiciones y reserva, idempotencia, payload y errores 404/409/422. | Pendiente |
