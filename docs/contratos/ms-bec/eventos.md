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
| `ReporteAcademicoEntregado` | `reporteAcademicoId`, `becaId`, `periodoAcademicoId`, `fechaLimitePeriodo`, `entregaTardia`; sin notas ni datos personales |
| `AlertaReporteAcademicoTardio` | `reporteAcademicoId`, `becaId`, `programaBecaId`, `periodoAcademicoId`, `fechaLimitePeriodo`; señal para que MS-PLT avise al coordinador |
| `EvaluacionPermanenciaRegistrada` | `evaluacionPermanenciaId`, `reporteAcademicoId`, `becaId`, `cumpleCondiciones`, `consecuenciaRecomendada`; sin calificaciones ni datos socioeconómicos |
| `ConsecuenciaPermanenciaAplicada` | `evaluacionPermanenciaId`, `reporteAcademicoId`, `becaId`, `consecuenciaAplicada`; se emite después de aplicar una consecuencia confirmada |
| `DesembolsoProgramado` | `becaId`, `calendarioId`, `desembolsoId` |
| `DesembolsoConfirmadoConOTP` | `becaId`, `calendarioId`, `desembolsoId` |
| `DesembolsoFallido` | `becaId`, `calendarioId`, `desembolsoId` |
| `DesembolsoDevuelto` | `becaId`, `calendarioId`, `desembolsoId` |
| `DesembolsoReintentado` | `becaId`, `calendarioId`, `desembolsoId` |
| `DesembolsoCancelado` | `becaId`, `calendarioId`, `desembolsoId`; se encola al cancelar una cuota por revocación |
| `DesembolsosBecaSuspendidos` | `becaId`, `suspensionId`, `programaBecaId`, `periodoAcademicoId`, `saldoPendiente`; se encola al suspender la beca |
| `DesembolsosBecaReanudados` | `becaId`, `suspensionId`, `programaBecaId`, `periodoAcademicoId`, `saldoPendiente`; se encola al reanudar la beca |
| `BecaRevocadaDefinitivamente` | `becaId`, `programaBecaId`, `periodoAcademicoId`, `estadoBeca`, `tipoCierre`; se encola al revocar definitivamente |
| `RenovacionBecaSolicitada` | `solicitudRenovacionId`, `becaId`, `periodoOrigenId`, `periodoSolicitadoId`; se encola al solicitar y persistir |
| `RenovacionBecaAprobada` | `solicitudRenovacionId`, `becaAnteriorId`, `becaSucesoraId`, `cadenaBecaId`, `periodoOrigenId`, `periodoNuevoId`; se encola con la aprobación local |
| `BecaActivadaNuevoPeriodo` | `becaId`, `becaAnteriorId`, `cadenaBecaId`, `periodoNuevoId`; se encola al crear la sucesora |
| `BecaConcluidaPorVencimientoRenovacion` | `becaId`, `cadenaBecaId`, `programaBecaId`, `periodoOrigenId`, `periodoSolicitadoId`; se encola después de confirmar la liberación del cupo ocupado |

El payload representa el estado resultante; las transiciones de oferta y beca
se publican mediante Outbox.
`DesembolsosBecaSuspendidos`, `DesembolsosBecaReanudados` y
`BecaRevocadaDefinitivamente` informan los cambios de estado. Los movimientos
de saldo y cupo en MS-PRG requieren la llamada REST idempotente de 17.6; estos
eventos no ejecutan movimientos presupuestarios.
Los contratos `RenovacionBecaAprobada` y `BecaActivadaNuevoPeriodo` se
encolan únicamente cuando la aprobación confirma la reserva por su puerto y
crea la beca sucesora. No se emiten desde solicitudes pendientes. La
integración real con MS-PRG continúa pendiente.

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
