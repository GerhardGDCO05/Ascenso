# Contrato interno de convocatorias: MS-PRG → MS-POS

## Alcance y seguridad

`MS-PRG` es el servicio propietario del agregado `Convocatoria`. `MS-POS` consume estas rutas de solo lectura y no replica el agregado ni consulta directamente la base de datos de `MS-PRG`.

Las rutas requieren JWT Bearer válido con el permiso `convocatorias:leer` en el claim `permissions`. El token conserva sus roles; `permissions` es un arreglo JSON de strings, no un string con permisos separados por comas.

| HTTP | Resultado |
|---|---|
| `401 Unauthorized` | Token ausente o inválido. |
| `403 Forbidden` | Falta `convocatorias:leer`; el cuerpo incluye `codigo: "PERMISO_DENEGADO"`. |

El mapper de `permissions` en Keycloak es una dependencia del equipo de identidad y no forma parte de este cambio.

## Rutas

### Consultar una convocatoria

`GET /internal/v1/convocatorias/{convocatoriaId}`

- `convocatoriaId`: UUID de la convocatoria.
- `200 OK`: objeto `ConvocatoriaDetalleDto`.
- `404 Not Found`: no existe; cuerpo `ProblemDetails` con `codigo: "CONVOCATORIA_NO_ENCONTRADA"`.

La respuesta incluye `estado`, por lo que el consumidor puede distinguir `BORRADOR`, `PUBLICADA`, `CERRADA` y `CANCELADA`.

### Consultar convocatorias abiertas

`GET /internal/v1/convocatorias/abiertas`

- Sin parámetros de consulta ni paginación.
- `200 OK`: arreglo de `ConvocatoriaAbiertaDto`; si no hay resultados, devuelve `[]`.
- Incluye convocatorias en estado `PUBLICADA` cuya apertura es menor o igual al instante actual y cuyo cierre es mayor o igual al instante actual del reloj de `MS-PRG`.

## DTOs

Los nombres JSON se serializan en `camelCase`. Las fechas son instantes `DateTimeOffset` en ISO 8601 con offset; los identificadores son UUID.

### `ConvocatoriaDetalleDto`

```json
{
  "id": "uuid",
  "programaBecaId": "uuid",
  "codigo": "CODIGO",
  "nombre": "Nombre de la convocatoria",
  "periodoAcademicoId": "uuid",
  "estado": "PUBLICADA",
  "fechaApertura": "2026-12-01T04:00:00+00:00",
  "fechaCierre": "2026-12-21T03:59:59+00:00",
  "fechaLimiteSubsanacionDocumentos": "2026-12-22T03:59:59+00:00",
  "fechaInicioEvaluacion": "2026-12-22T04:00:00+00:00",
  "fechaCierreEvaluacion": "2027-01-10T03:59:59+00:00",
  "requiereRevisionDocumentalPrevia": true,
  "cuposAsignados": 10,
  "criteriosElegibilidad": {
    "promedioMinimo": 15.0,
    "edadMinima": 18,
    "edadMaxima": 30,
    "topeIngresoFamiliar": 1000.0,
    "universidadesPermitidasIds": ["uuid"],
    "carrerasPermitidasIds": ["uuid"]
  },
  "documentosRequeridos": [
    {
      "id": "uuid",
      "tipoDocumento": "IDENTIDAD",
      "nombre": "Documento de identidad",
      "descripcion": null,
      "obligatorio": true,
      "tiposMimePermitidos": ["application/pdf"],
      "tamanioMaximoMb": 5
    }
  ],
  "fechaPublicacion": "2026-11-30T16:00:00+00:00",
  "fechaCreacion": "2026-11-30T16:00:00+00:00",
  "fechaUltimaModificacion": "2026-11-30T16:00:00+00:00"
}
```

`fechaPublicacion` puede ser `null`.

### `ConvocatoriaAbiertaDto`

Cada elemento de la respuesta de `/abiertas` contiene `id`, `programaBecaId`, `codigo`, `nombre`, `periodoAcademicoId`, `fechaApertura`, `fechaCierre`, `fechaLimiteSubsanacionDocumentos`, `fechaInicioEvaluacion`, `fechaCierreEvaluacion`, `requiereRevisionDocumentalPrevia`, `cuposAsignados`, `criteriosElegibilidad` y `documentosRequeridos`, con la misma estructura anidada descrita arriba. No incluye `estado`, `fechaPublicacion` ni campos de auditoría.

## Dependencia de periodo académico en MS-PLT

Al crear, editar o publicar una convocatoria, `MS-PRG` consulta:

`GET /internal/v1/periodos-academicos/{id}`

`{id}` debe ser un UUID. La respuesta `200` debe ser JSON con `id` (el mismo UUID), `nombre` y `estado` no vacíos; `estado` debe ser `VIGENTE`. El cliente de `MS-PRG` no consume fechas de inicio o fin del periodo.

| Respuesta de MS-PLT | Comportamiento de MS-PRG |
|---|---|
| `200` con estado distinto de `VIGENTE`, o `404` | `422 Unprocessable Entity`, `codigo: "PERIODO_ACADEMICO_NO_VIGENTE"`. |
| Error HTTP distinto de `404`, timeout, conexión fallida o respuesta inválida | `503 Service Unavailable`, `codigo: "SERVICIO_PERIODO_ACADEMICO_NO_DISPONIBLE"`. |

La ruta está implementada en el cliente de `MS-PRG`; el endpoint y su mecanismo de autenticación deben coordinarse con el equipo de `MS-PLT`, que es propietario de ese servicio.
