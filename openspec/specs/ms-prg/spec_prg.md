# Specification: MS-PRG (Microservicio de Programas, Fondos, Convenios y Presupuesto)

> En la especificación maestra (`docs/Explicacion_del_DDD_V7.md`) este contexto agrupa **BC-02** (*Programas, Fondos y Convenios Aliados*) y **BC-06 (Parcial)** (*Control de Presupuesto y Cupos*).

## Context

Gestión integral del diseño y configuración de programas de becas (`ProgramaBeca`), control presupuestario y de cupos en tiempo real (`BalancePresupuestarioPrograma`, `ReservaPresupuestoCupo`, `MovimientoPresupuesto`), administración de donantes y asignación de fondos (`Donante`, `AporteDonante`, `AsignacionFondoPrograma`), convenios con universidades aliadas y versionado histórico de aranceles (`UniversidadAliada`, `Convenio`, `CarreraElegible`, `CostoMatriculaReferencia`), y parametrización de convocatorias (`Convocatoria`, `ReglaElegibilidad`, `DocumentoRequerido`).

## Invariants

- Todo agregado y entidad mutable hereda de `EntidadAuditable` con identificador `UUIDv7` (`Guid.CreateVersion7()`), marcas de tiempo en UTC y token de concurrencia optimista (`Version`).
- Todo registro contable o histórico hereda de `EntidadInmutable` (*append-only*, sin actualización ni borrado).
- Ninguna entidad ni agregado del dominio expone setters públicos en propiedades mutables; toda modificación de estado ocurre mediante métodos de dominio expresivos.
- Las variaciones de cobertura, elegibilidad y permanencia de `ProgramaBeca` se modelan mediante composición de Objetos de Valor inmutables (`PoliticaCobertura`, `CriteriosElegibilidadPrograma`, `CondicionesPermanencia`), nunca mediante jerarquías de herencia.
- En todo instante, `BalancePresupuestarioPrograma.presupuestoDisponibleActual >= 0` y `cuposDisponibles >= 0` (`RN-PRG-03`), protegidos mediante restricciones `CHECK` en PostgreSQL y actualización atómica condicional (`C2`).
- Los montos monetarios se expresan en `Decimal(18,2)` en `USD`; las fechas de negocio usan `DateOnly` y las marcas de tiempo usan `DateTimeOffset` en UTC.

## Scenarios

<!-- OpenCode inyectará los escenarios GIVEN/WHEN/THEN detallados al archivar cada HU -->

## Behaviors / Business Rules

### 1. Programas de Becas y Configuración (RF-11, RN-PRG-01)

- **Creación y Composición:** Un `ProgramaBeca` nace en estado `BORRADOR` con presupuesto y cupos ocupados en `0`, componiendo sus objetos de valor inmutables `PoliticaCobertura`, `CriteriosElegibilidadPrograma` y `CondicionesPermanencia`.
- **Transición a Configurado (`RN-PRG-01`):** Solo puede pasar a `CONFIGURADO` si `presupuestoTotalAsignado > 0`, `cupoTotal > 0` y todas sus universidades elegibles poseen un `Convenio` en estado `VIGENTE` (`422 PROGRAMA_NO_CONFIGURABLE`).
- **Calendario Académico del Programa:** `CalendarioAcademicoPrograma` planifica por `PeriodoAcademico` las ventanas de renovación y fechas límite de entrega de reportes académicos.

### 2. Control Presupuestario, Cupos y Reserva Atómica (RF-12, RF-23, RN-PRG-03, C2)

- **Fórmula Única de Disponibilidad en Tiempo Real:**
  - `presupuestoDisponibleActual = presupuestoTotalAsignado - montoDesembolsadoEjecutado - comprometidoOfertasPendientes - comprometidoBecasActivas - comprometidoBecasSuspendidas - reservasRenovacionEnCurso`
  - `cuposDisponibles = cupoTotal - cuposOcupadosActivos - cuposSuspendidos - cuposRetenidosEnOfertas`
- **Invariante Contable de No Doble Conteo:** `comprometidoBecasActivas` y `comprometidoBecasSuspendidas` representan únicamente el saldo comprometido pendiente por desembolsar en el periodo. Al confirmarse un desembolso (`RF-20`), en el mismo movimiento contable se deduce de `comprometidoBecasActivas` y se suma a `montoDesembolsadoEjecutado`, manteniendo intacto `presupuestoDisponibleActual`. Al transicionar una beca entre `ACTIVA ↔ SUSPENDIDA`, su saldo pendiente y su cupo se transfieren entre las bolsas activas y suspendidas sin liberarse como disponibles.
- **Reserva Atómica (`C2`):** Al emitir una oferta o aprobar una renovación (`POST /internal/v1/programas/{id}/reservas` con `Idempotency-Key`), `MS-PRG` ejecuta una actualización atómica condicional (`ExecuteUpdateAsync`) exigiendo `cuposDisponibles >= 1` y `presupuestoDisponibleActual >= monto`. Si `filasAfectadas == 0`, rechaza con `409 SIN_CUPO_O_PRESUPUESTO`.
- **Libro Mayor Inmutable:** Cada reserva, confirmación, liberación o ajuste presupuestario registra un `MovimientoPresupuesto` inmutable con el `disponibleResultante` y el `eventoId` de trazabilidad. Las `ReservaPresupuestoCupo` pendientes tienen TTL de 5 minutos y expiran automáticamente si no se confirman.

### 3. Donantes, Aportes y Asignación Multi-Donante (RF-13, RN-PRG-02, RN-PRG-04)

- **Aportes Inmutables (`RN-PRG-04`):** Todo `AporteDonante` exige `numeroReferenciaBancaria` único y `urlComprobanteFirebase` obligatorio (`400 COMPROBANTE_REQUERIDO` / `409 REFERENCIA_DUPLICADA`).
- **Asignación Atómica (`RN-PRG-02`):** Asignar fondos de un `Donante` mediante `AsignacionFondoPrograma` exige `Donante.estado == ACTIVO` y `montoAsignado <= Donante.saldoDisponibleAsignacion`. En una única transacción serializable se deduce el saldo del donante y se incrementa `BalancePresupuestarioPrograma.presupuestoTotalAsignado` (`409 SALDO_DONANTE_INSUFICIENTE`).
- **Bloqueos Operativos:** Un `Donante` con fondos comprometidos en programas activos no puede transicionar a `INACTIVO` (`409 DONANTE_CON_FONDOS`). Asimismo, se prohíbe reducir o desasignar fondos de un `ProgramaBeca` en estado `ACTIVO` con convocatoria en curso (`409 PRESUPUESTO_EN_EJECUCION`).

### 4. Universidades Aliadas, Convenios y Versionado de Matrículas (RF-14, RN-PRG-05)

- **Convenios y Continuidad de Becas Activas:** Cada `UniversidadAliada` administra sus `Convenio` y `CarreraElegible`. Si un `Convenio` pasa a `VENCIDO`, las becas en estado `ACTIVA` continúan sin interrupción, pero se rechaza cualquier nueva postulación u oferta de beca vinculada a esa universidad (`422 CONVENIO_VENCIDO`).
- **Versionado Histórico de Aranceles (`RN-PRG-05`):** Al publicar un nuevo `CostoMatriculaReferencia` (`montoReferenciaMatricula > 0`) para una carrera y periodo académico, la versión anterior pasa a `HISTORICO` inmutable y se crea la versión `N+1` como `VIGENTE`. Los contratos y becas en curso conservan intacta su fotografía (`costoMatriculaReferenciaSnapshotId`) sin alteración retroactiva.

### 5. Convocatorias y Documentos Requeridos (RF-02, RF-03)

- **Parametrización de Convocatorias:** Cada `Convocatoria` se vincula a un `ProgramaBeca` y a un `PeriodoAcademico`, definiendo ventanas (`fechaApertura < fechaCierre`, `fechaLimiteSubsanacionDocumentos`), `cupoConvocatoria`, `ReglaElegibilidad` evaluables y el catálogo de `DocumentoRequerido` (tipo de documento, obligatoriedad, tipos MIME permitidos y tamaño máximo en MB).
- **Publicación y Cierre:** Al publicarse, emite `ConvocatoriaPublicada` para sincronizar la réplica `ConvocatoriaRef` en `MS-POS`.

### 6. Integraciones de Negocio

- `MS-PLT` publica `PeriodoAcademicoActivado` para habilitar calendarios y convocatorias en `MS-PRG`.
- `MS-PRG` publica `ProgramaBecaCreado`, `ProgramaBecaConfigurado`, `PresupuestoProgramaAsignado`, `BalancePresupuestarioActualizado`, `AporteDonanteRegistrado`, `ConvenioUniversidadRegistrado`, `ConvenioUniversidadVencido`, `CostoMatriculaActualizado`, `ConvocatoriaPublicada`, `ConvocatoriaCerrada` y `ConvocatoriaFinalizada`.
- `MS-BEC` consume `POST /internal/v1/programas/{id}/reservas` (con `Idempotency-Key`) para reservar cupo y presupuesto de ofertas/renovaciones, y publica `DesembolsoConfirmadoConOTP` y `BecaRevocadaDefinitivamente` para actualizar los saldos en `BalancePresupuestarioPrograma`.
- `MS-POS` consume `GET /internal/v1/programas/{id}/disponibilidad` para calcular la línea de corte del ranking y promociones de lista de espera.

