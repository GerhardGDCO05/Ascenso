# Registro de Auditoría — Tasks 3.1 a 3.11: modelo de dominio de otorgamiento de MS-BECA

- **Fecha y hora de ejecución:** 11 de noviembre de 2026, 01:18 (UTC-4)
- **Rama local:** `Gerhard`
- **Alcance:** implementar las tareas `3.1` a `3.11` de `openspec/changes/bootstrap-ms-beca/tasks.md`.
- **Trabajo local:** sin `git push` ni modificación de ramas remotas.
- **Dependencias externas:** ninguna nueva; `Ascenso.Becas.Domain` sigue siendo C# puro.

## 1. Objetivo

Construir el primer corte del agregado de otorgamiento de `MS-BEC`: la raíz `Beca`, la entidad `OfertaBeca`, la entidad `CompromisoBecario` y el historial inmutable `HistorialEstadoBeca`, junto con la máquina de estados cerrada, las invariantes monetarias, la política de no doble beneficio, los eventos de dominio y las pruebas que verifican cada regla.

## 2. Cambios realizados

### Agregado y entidades

- `Apps/services/ms-becas/Ascenso.Becas.Domain/Otorgamiento/Agregados/Beca.cs`
  - Fábrica `CrearOtorgada` con `Estado = OTORGADA`, `CadenaBecaId = id`, `BecaAnteriorId = null`, `PeriodosCursados = 0`, `MontoDesembolsadoPeriodoActual = 0`, `TieneRenovacionEnCurso = false`, snapshot de `PoliticaCobertura`, `CostoMatriculaReferenciaSnapshotId` y `MontoComprometidoPeriodoActual`.
  - Emisión de oferta con vencimiento exacto a 15 días y evento `OfertaBecaEmitidaEvento`.
  - Activación por aceptación de oferta, rechazo, anulación y expiración con cierre terminal de la beca.
  - Transiciones `SUSPENDIDA → ACTIVA`, `ACTIVA → SUSPENDIDA`, `ACTIVA → RENOVADA`, `ACTIVA → CONCLUIDA` y revocación definitiva desde `ACTIVA` o `SUSPENDIDA`.
  - Compuerta del compromiso firmado: la beca con `RequiereCompromisoFirmado` no queda habilitada para desembolso hasta que el compromiso se apruebe.
  - Exposición de `Historial` y de `EventosDominio`; sin setters públicos.
- `Otorgamiento/Entidades/OfertaBeca.cs`
  - Campos `MontoOfertaPorPeriodo`, `MontoTotalEstimadoDuracion`, `DuracionPeriodos`, `CondicionesSnapshot`, `UrlCartaPdfFirebase`, `HashCartaSha256`, `FechaVencimientoAceptacion`, `ReferenciaJobExpiracionHangfireId`, `Version`, `Estado` y trazabilidad de emisión/respuesta.
  - Respuestas `ACEPTADA`, `RECHAZADA_POR_POSTULANTE`, `EXPIRADA` y `ANULADA_POR_COORDINACION`; los estados terminales no admiten nuevas transiciones.
  - Validación de hash SHA-256, monto total coherente y vencimiento posterior a la emisión.
- `Otorgamiento/Entidades/CompromisoBecario.cs`
  - Estados `PENDIENTE_CARGA`, `EN_REVISION`, `APROBADO` y `RECHAZADO`.
  - Incremento de `NumeroIntentoCarga`, URL del PDF, hash, IP de carga, fechas y datos de revisión.
  - Rechazo con observaciones obligatorias y prohibición de reutilizar el hash del intento rechazado.
- `Otorgamiento/Entidades/HistorialEstadoBeca.cs`
  - Entidad inmutable derivada de `EntidadInmutable`, append-only.
  - Registra estado anterior y nuevo, fecha, actor, correlación, justificación, OTP y marca de terminal irreversible.
  - No expone setters ni métodos públicos de modificación o borrado.

### Servicios y valores de dominio

- `Otorgamiento/Servicios/CalculadoraMontoOferta.cs`: cálculo por modalidad `TOTAL`, `PARCIAL` y `MONTO_FIJO`, redondeo a dos decimales con `MidpointRounding.AwayFromZero` y total estimado por duración.
- `Otorgamiento/Servicios/PoliticaNoDobleBeneficio.cs`: rechaza otra beca `ACTIVA` con cobertura `TOTAL` y otra beca `ACTIVA` en el mismo periodo salvo que ambos programas permitan acumulación.
- `Otorgamiento/ValueObjects/ContextoOperacion.cs`: actor, correlación, fecha, justificación normalizada y OTP opcional.
- `Otorgamiento/Datos/DatosCreacionBeca.cs` y `DatosOfertaBeca.cs`: entradas explícitas de negocio.
- `Enums/TipoEntidadHistorial.cs`: discriminador `Beca`, `OfertaBeca`, `CompromisoBecario`.

### Eventos de dominio

- Base `EventoBeca`, `EventoOfertaBeca` y `EventoCompromisoBecario`.
- Eventos concretos: `OfertaBecaEmitidaEvento`, `BecaActivadaEvento`, `OfertaBecaRechazadaEvento`, `OfertaBecaAnuladaEvento`, `OfertaBecaExpiradaEvento`, `CompromisoBecarioCargadoEvento`, `CompromisoBecarioAprobadoEvento` y `CompromisoBecarioRechazadoEvento`.

### Pruebas agregadas

- `tests/Ascenso.Becas.Domain.Tests/Otorgamiento/PruebasOtorgamiento.cs`: fábrica de datos y escenarios compartidos.
- `BecaTests.cs`: campos iniciales, compromiso inicial, validaciones de creación, historial y justificación.
- `OfertaBecaTests.cs`: creación, valores por defecto, vencimiento a 15 días, hash, total coherente y oferta única.
- `TransicionesBecaTests.cs`: activación, suspensión, reactivación, renovación, conclusión, revocación, transiciones prohibidas, estados terminales y justificación insuficiente.
- `TransicionesOfertaTests.cs`: aceptación, rechazo, anulación, expiración y bloqueo de estados terminales.
- `CompromisoBecarioTests.cs`: ciclo completo, reintentos, rechazo, reutilización de hash, IP inválida y programa sin compromiso.
- `HistorialEstadoBecaTests.cs`: inmutabilidad por reflexión, una fila por transición y datos de auditoría.
- `PoliticaNoDobleBeneficioTests.cs`: cobertura total, mismo periodo, acumulación, periodos distintos y becas ignoradas.
- `CalculadoraMontoOfertaTests.cs`: 0%, 100%, monto fijo, matrícula cero, redondeo y entradas inválidas.
- `EventosDominioTests.cs`: evento exacto por transición y ausencia de eventos en fallos.

## 3. Decisiones de diseño

- **Raíz única de consistencia:** `OfertaBeca` y `CompromisoBecario` solo se modifican a través de `Beca`; sus métodos internos quedan `internal`.
- **Cierre por oferta:** el rechazo, la anulación y la expiración de la oferta cierran la `Beca` como `REVOCADA`, registrando el `TipoCierreBeca` correspondiente.
- **Revocación definitiva:** `Revocar` exige que la beca esté `ACTIVA` o `SUSPENDIDA`; el paso `OTORGADA → REVOCADA` se produce por el cierre de la oferta, no por revocación directa.
- **Vencimiento:** la oferta vence exactamente a los 15 días; la aceptación exige `ahora <= fechaVencimientoAceptacion`.
- **Justificaciones:** anulación y revocación exigen `Trim().Length >= 10`; rechazo y expiración usan justificaciones canónicas.
- **Historial:** se registra una fila por cada transición de beca, oferta o compromiso, con la marca de terminal irreversible cuando corresponde.
- **Auditoría:** la versión de las entidades se incrementa con `ActualizarAuditoria`; se evitó duplicar actualizaciones en las operaciones de cierre.

## 4. Validación ejecutada

```text
dotnet build Ascenso.Becas.sln -c Release --no-restore
→ 0 advertencias, 0 errores

dotnet test Ascenso.Becas.sln -c Release --no-restore
→ Ascenso.Becas.Domain.Tests        136/136 correctas
→ Ascenso.Becas.Application.Tests     1/1   correcta
→ Ascenso.Becas.ArchitectureTests     8/8   correctas
→ Ascenso.Becas.IntegrationTests      1/1   correcta
→ Total                            146/146 correctas

dotnet format Ascenso.Becas.sln --verify-no-changes --no-restore
→ correcto (exit 0)
```

La tabla completa de transiciones se verifica en `TransicionesTests` para `Beca`, `OfertaBeca` y `CompromisoBecario`, incluyendo todas las duplas origen/destino prohibidas. Las pruebas de estados terminales refuerzan que ninguna transición posterior es aceptada.

## 5. Resultado

- **Antes:** 18 de 114 tareas completadas.
- **Después:** 29 de 114 tareas completadas.
- **Tareas cerradas:** `3.1`, `3.2`, `3.3`, `3.4`, `3.5`, `3.6`, `3.7`, `3.8`, `3.9`, `3.10` y `3.11`.
- **Roadmaps:** no se marcó ningún RF de MS-BEC porque todavía no existe funcionalidad de negocio completa de punta a punta.
- **Capas:** `Ascenso.Becas.Domain` no incorporó EF Core, ASP.NET, MassTransit, Firebase, Keycloak ni Npgsql.
- **Estado local:** sin commit y sin `git push`; `ms-pos` y `ms-plt` intactos.

## 6. Siguiente bloque

Continuar con las tareas `4.x` (persistencia y migraciones) del cambio `bootstrap-ms-beca`.
