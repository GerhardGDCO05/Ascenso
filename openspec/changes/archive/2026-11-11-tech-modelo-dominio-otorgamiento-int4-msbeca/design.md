## How

Se construyó el primer corte del agregado de otorgamiento de `MS-BEC`, manteniendo `Ascenso.Becas.Domain` como C# puro.

### Agregado y entidades

- `Beca`:
  - Fábrica `CrearOtorgada` con `Estado = OTORGADA`, `CadenaBecaId = id`, `BecaAnteriorId = null`, `PeriodosCursados = 0`, `MontoDesembolsadoPeriodoActual = 0` y `TieneRenovacionEnCurso = false`.
  - Snapshot de `PoliticaCobertura`, `CostoMatriculaReferenciaSnapshotId` y `MontoComprometidoPeriodoActual`.
  - Emisión de oferta con vencimiento exacto a 15 días y evento `OfertaBecaEmitidaEvento`.
  - Activación, rechazo, anulación, expiración, suspensión, reactivación, renovación, conclusión y revocación.
  - Compuerta del compromiso firmado antes de habilitar desembolsos.
  - Historial y eventos de dominio expuestos sin setters públicos.
- `OfertaBeca`: montos, duración, condiciones snapshot, URL y hash de carta, vencimiento, job de expiración y estados terminales.
- `CompromisoBecario`: estados `PENDIENTE_CARGA`, `EN_REVISION`, `APROBADO` y `RECHAZADO`; intentos, PDF, hash, IP y revisión.
- `HistorialEstadoBeca`: entidad inmutable append-only con estado anterior/nuevo, actor, correlación, justificación, OTP y marca terminal.

### Servicios y valores

- `CalculadoraMontoOferta`: modalidades `TOTAL`, `PARCIAL` y `MONTO_FIJO`, redondeo a dos decimales con `MidpointRounding.AwayFromZero`.
- `PoliticaNoDobleBeneficio`: rechaza otra beca `ACTIVA` con cobertura `TOTAL` y otra beca `ACTIVA` en el mismo periodo, salvo acumulación permitida.
- `ContextoOperacion` y datos explícitos de creación de beca y oferta.

### Eventos y pruebas

- Eventos `OfertaBecaEmitida`, `BecaActivada`, `OfertaBecaRechazada`, `OfertaBecaAnulada`, `OfertaBecaExpirada`, `CompromisoBecarioCargado`, `CompromisoBecarioAprobado` y `CompromisoBecarioRechazado`.
- Pruebas de fábrica, campos iniciales, transiciones permitidas y prohibidas, estados terminales, compromiso, historial inmutable, no doble beneficio, cálculo de montos y eventos.

### Decisiones de diseño

- **Raíz única de consistencia:** `OfertaBeca` y `CompromisoBecario` solo se modifican a través de `Beca`; sus métodos quedan `internal`.
- **Cierre por oferta:** rechazo, anulación y expiración cierran la beca como `REVOCADA` con el `TipoCierreBeca` correspondiente.
- **Revocación directa:** solo desde `ACTIVA` o `SUSPENDIDA`; `OTORGADA → REVOCADA` ocurre por cierre de oferta.
- **Justificaciones:** anulación y revocación exigen `Trim().Length >= 10`.
- **Auditoría:** una fila de historial por transición y versión incrementada con `ActualizarAuditoria`.

### Validación ejecutada

```text
dotnet build -c Release --no-restore      → 0 advertencias, 0 errores
Ascenso.Becas.Domain.Tests                → 136/136 correctas
Ascenso.Becas.Application.Tests           →   1/1 correcta
Ascenso.Becas.ArchitectureTests           →   8/8 correctas
Ascenso.Becas.IntegrationTests            →   1/1 correcta
Total                                     → 146/146 correctas
dotnet format --verify-no-changes         → correcto
```
