# Contratos de eventos de MS-BEC

## Sobre común

Todos los mensajes de integración usan `EventoIntegracion<T>`:

```json
{
  "eventId": "uuid",
  "occurredAt": "2026-11-22T12:00:00Z",
  "correlationId": "uuid",
  "producer": "ms-bec",
  "aggregateId": "uuid",
  "aggregateVersion": 1,
  "payload": {}
}
```

Los nombres de propiedades JSON son camelCase. `eventId` permite deduplicar,
`correlationId` propaga trazabilidad, y `aggregateId`/`aggregateVersion`
identifican el origen y la versión. El sobre no lleva una propiedad
`eventType`: el tipo del mensaje y el exchange identifican el payload.
Ningún evento de MS-BEC incluye nombre, documento, correo, información
bancaria o datos socioeconómicos.

## Publicados por MS-BEC

Los exchanges se nombran `Ascenso.Becas:<nombre-del-payload>`.

| Payload | Campos del payload |
| :--- | :--- |
| `OfertaBecaEmitida` | `becaId`, `ofertaId`, `postulacionId`, `programaBecaId`, `periodoAcademicoId`, `fechaVencimientoAceptacion`, `estadoOferta` |
| `BecaActivada` | `becaId`, `ofertaId`, `postulacionId`, `programaBecaId`, `periodoAcademicoId`, `estadoBeca` |
| `OfertaBecaRechazada` | `becaId`, `ofertaId`, `postulacionId`, `programaBecaId`, `periodoAcademicoId`, `estadoOferta`, `tipoCierre` |
| `OfertaBecaAnulada` | `becaId`, `ofertaId`, `postulacionId`, `programaBecaId`, `periodoAcademicoId`, `estadoOferta`, `tipoCierre` |
| `OfertaBecaExpirada` | `becaId`, `ofertaId`, `postulacionId`, `programaBecaId`, `periodoAcademicoId`, `estadoOferta`, `tipoCierre` |
| `CompromisoBecarioCargado` | `becaId`, `compromisoId`, `numeroIntentoCarga`, `estadoCompromiso` |
| `CompromisoBecarioAprobado` | `becaId`, `compromisoId`, `estadoCompromiso` |
| `CompromisoBecarioRechazado` | `becaId`, `compromisoId`, `estadoCompromiso` |

El payload representa el estado resultante; las transiciones de oferta y beca
se publican mediante Outbox.

## Consumidos desde MS-PRG

Los exchanges de entrada se nombran `Ascenso.Programas:<nombre-del-payload>`.

| Payload | Campos del payload |
| :--- | :--- |
| `ProgramaBecaConfigurado` | `programaBecaId`, `codigo`, `nombre`, `politicaCobertura`, `condicionesPermanencia`, `permiteAcumulacion`, `requiereCompromisoFirmado`, `duracionPeriodos`, `estado` |
| `CostoMatriculaActualizado` | `costoMatriculaReferenciaId`, `carreraId`, `periodoAcademicoId`, `montoReferenciaMatricula`, `moneda`, `numeroVersion`, `fechaVigenciaDesde`, `fechaVigenciaHasta`, `estado` |
| `ConvenioUniversidadRegistrado` | `universidadAliadaId`, `convenioId`, `estado`, `fechaFin` |
| `ConvenioUniversidadVencido` | `universidadAliadaId`, `convenioId`, `estado`, `fechaFin` |

`politicaCobertura` contiene `modalidad`, `porcentajeCobertura`,
`montoFijoPeriodo` y `moneda`. `condicionesPermanencia` contiene
`promedioMinimoRenovacion`, `materiasMinimasAprobadas`,
`maximoMateriasReprobadas`, `horasServicioPorPeriodo` y
`consecuenciaIncumplimiento`.

Los nombres y campos reflejan los tipos de contrato actualmente implementados
por MS-BEC. El equipo de MS-PRG debe validar el esquema y el enrutamiento antes
de acordar compatibilidad de producción.
