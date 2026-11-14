# Design: Registro y Configuración de Programas de Becas (`rf11-int3-msprg`)

## Context

El microservicio `MS-PRG` (`Ascenso.Programas`) ya dispone de su infraestructura transaccional sobre PostgreSQL (`ProgramasDbContext` en el esquema `programas`), `ConfiguracionPersistencia` con soporte para `UUIDv7` y concurrencia optimista (`Version`), `Transactional Outbox` de MassTransit (`inbox_state`, `outbox_state`, `outbox_message`), middleware `ManejadorExcepcionesDominio` (`ProblemDetails`) y los agregados `Donante` (`RF-13`) y `UniversidadAliada` (`RF-14`).

Para implementar el **RF-11** (`[HU-11.1]`, `[HU-11.2]`, `[HU-11.3]`) conforme a `openspec/specs/ms-prg/spec_prg.md` (`RN-PRG-01`, `RN-PRG-02`) y `docs/convenciones_c_y_ddd.md`, se incorpora el agregado raíz `ProgramaBeca` y su entidad interna `AsignacionFondoPrograma`, coordinando transaccionalmente con `Donante` (para deducir/restituir `SaldoDisponibleAsignacion`) y con `UniversidadAliada` (para verificar que todas las universidades y carreras elegibles posean convenio `Vigente` antes de pasar el programa a `Configurado`). Asimismo, se mantienen las tres reglas técnicas críticas de `MS-PRG`:
1. `.ValueGeneratedNever()` en todos los `Id` (`Guid` UUIDv7 generados en dominio) y en `Version` (`.IsConcurrencyToken().ValueGeneratedNever()`) mediante `ConfiguracionPersistencia.ConfigurarEntidadAuditable`.
2. Carga con tracking en los métodos de repositorio empleados en escritura (`ObtenerPorIdAsync` sin `AsNoTracking()` ni `_contexto.Update(...)`) para que las nuevas `AsignacionFondoPrograma` se inserten como `EntityState.Added` (`INSERT`) sin provocar `DbUpdateConcurrencyException`.
3. Campos privados con prefijo `_camelCase` en todas las capas y proyectos de prueba (`IDE1006` / `dotnet format --verify-no-changes`).

## Goals / Non-Goals

**Goals:**
- Modelar en `Ascenso.Programas.Domain/Programas/` el agregado raíz `ProgramaBeca`, la entidad `AsignacionFondoPrograma`, los objetos de valor inmutables compuestos (`PoliticaCobertura`, `CriteriosElegibilidadPrograma`, `CondicionesPermanencia`), las enumeraciones (`EstadoProgramaBeca`, `ModalidadCobertura`, `EstadoAsignacionFondo`, `FrecuenciaReporteAcademico`, `ConsecuenciaIncumplimiento`), los eventos de dominio, las excepciones de negocio (`400`, `404`, `409`, `422`) y el puerto `IProgramaBecaRepository`.
- Extender `Donante` (`Ascenso.Programas.Domain/Donantes/Agregados/Donante.cs`) con los métodos de dominio `ComprometerSaldoParaPrograma` y `ReintegrarSaldoDePrograma` para garantizar la invariante `RN-PRG-02` (`409 SALDO_DONANTE_INSUFICIENTE`).
- Implementar en `Ascenso.Programas.Application/Programas/` los comandos (`CrearProgramaBecaCommand`, `AsignarPresupuestoDonantesCommand`, `ReajustarPresupuestoDonanteCommand`, `ConfigurarCriteriosElegibilidadCommand`), consultas (`ConsultarProgramasQuery`, `ObtenerProgramaPorIdQuery`), validadores FluentValidation, DTOs y eventos de integración (`ProgramaBecaCreado`, `PresupuestoProgramaAsignado`, `ProgramaBecaConfigurado`).
- Configurar en `Ascenso.Programas.Infrastructure/Persistence/` el mapeo EF Core de `programas_beca` y `asignaciones_fondo_programa`, la migración correspondiente y `ProgramaBecaRepository`.
- Exponer en `Ascenso.Programas.Api/Controllers/v1/ProgramasController.cs` los endpoints REST bajo `/api/v1/programas`.

**Non-Goals:**
- Reserva atómica de cupos y presupuesto individual por becario (`POST /internal/v1/programas/{id}/reservas`, `BalancePresupuestarioPrograma`, `ReservaPresupuestoCupo`, `MovimientoPresupuesto`), que pertenecen al alcance del **RF-12**.
- Publicación y parametrización de convocatorias (`Convocatoria`), que pertenecen al **RF-02**.

## Decisions

### 1. Modelado del Agregado `ProgramaBeca`, Composición de Objetos de Valor y `AsignacionFondoPrograma` (`Ascenso.Programas.Domain`)

- **Decisión:**
  - `ProgramaBeca : AggregateRoot` (`EntidadAuditable<Guid>`) en `Ascenso.Programas.Domain/Programas/Agregados/ProgramaBeca.cs`:
    - Propiedades con `{ get; private set; }`:
      - `Codigo` (`string`, normalizado en mayúsculas sin espacios extremos; único en BD → `409 CODIGO_PROGRAMA_DUPLICADO`).
      - `Nombre` (`string`), `Descripcion` (`string`).
      - `PoliticaCobertura` (`PoliticaCobertura`, obligatorio desde creación).
      - `CupoTotal` (`int > 0`), `CuposOcupados` (`int`, nace en `0`), `DuracionPeriodos` (`int > 0`).
      - `PeriodoInicialId` (`string`), `VigenciaDesde` (`DateOnly`), `VigenciaHasta` (`DateOnly`, validando `VigenciaHasta > VigenciaDesde` → `FechasVigenciaProgramaInvalidasException` con HTTP `400` y código `FECHAS_VIGENCIA_PROGRAMA_INVALIDAS`).
      - `PermiteAcumulacion` (`bool`), `RequiereCompromisoFirmado` (`bool`), `TerminosCompromisoEspecial` (`string?`).
      - `PresupuestoTotalAsignado` (`decimal`, nace en `0.00m`), `Moneda` (`string`, `"USD"`).
      - `Estado` (`EstadoProgramaBeca`: `Borrador`, `Configurado`, `Activo`, `Cerrado`, `Archivado`; nace en `Borrador`).
      - `CriteriosElegibilidad` (`CriteriosElegibilidadPrograma?`, opcional en `Borrador`, requerido para pasar a `Configurado`).
      - `CondicionesPermanencia` (`CondicionesPermanencia?`, opcional en `Borrador`, requerido para pasar a `Configurado`).
      - Colección privada `_asignacionesFondo` (`List<AsignacionFondoPrograma>`) expuesta como `IReadOnlyCollection<AsignacionFondoPrograma> AsignacionesFondo`.
  - **Objetos de Valor Inmutables por Composición (`Domain/Programas/ObjetosValor/`):**
    - `PoliticaCobertura`:
      - `Modalidad` (`ModalidadCobertura`: `Total`, `Parcial`, `MontoFijo`), `PorcentajeCobertura` (`decimal`), `MontoFijoPeriodo` (`decimal`), `Moneda` (`string`, `"USD"`).
      - Validación en instanciación (`PoliticaCoberturaInvalidaException` → HTTP `400`, código `POLITICA_COBERTURA_INVALIDA`):
        - Si `Modalidad == ModalidadCobertura.Total`: `PorcentajeCobertura == 100m` y `MontoFijoPeriodo >= 0m`.
        - Si `Modalidad == ModalidadCobertura.Parcial`: `PorcentajeCobertura > 0m && PorcentajeCobertura < 100m` y `MontoFijoPeriodo >= 0m`.
        - Si `Modalidad == ModalidadCobertura.MontoFijo`: `MontoFijoPeriodo > 0m` y `PorcentajeCobertura is >= 0m and <= 100m`.
    - `CriteriosElegibilidadPrograma`:
      - `PromedioMinimoIngreso` (`decimal` entre `0.00m` y `20.00m` → `PromedioAcademicoInvalidoException` con HTTP `400` y código `PROMEDIO_ACADEMICO_INVALIDO`), `EdadMinima` (`int > 0`), `EdadMaxima` (`int >= EdadMinima`), `TopeIngresoFamiliarMensual` (`decimal > 0`), `UniversidadesElegiblesIds` (`IReadOnlyList<Guid>`, al menos 1 ID no vacío sin duplicados), `CarrerasElegiblesIds` (`IReadOnlyList<Guid>`, al menos 1 ID no vacío sin duplicados).
    - `CondicionesPermanencia`:
      - `PromedioMinimoRenovacion` (`decimal` entre `0.00m` y `20.00m` → `PromedioAcademicoInvalidoException`), `MateriasMinimasAprobadas` (`int >= 0`), `MaximoMateriasReprobadas` (`int >= 0`), `HorasServicioPorPeriodo` (`int >= 0`), `FrecuenciaReportes` (`FrecuenciaReporteAcademico`: `Semestral`, `Trimestral`, `Anual`), `ConsecuenciaIncumplimiento` (`ConsecuenciaIncumplimiento`: `LlamadoAtencion`, `Suspension`, `Revocacion`), `MargenRiesgoPromedio` (`decimal >= 0`), `DuracionMaximaPausaPeriodos` (`int >= 0`), `PermiteRenovacionTardiaExcepcional` (`bool`).
  - **Entidad `AsignacionFondoPrograma : EntidadAuditable<Guid>` (`Domain/Programas/Entidades/AsignacionFondoPrograma.cs`):**
    - Propiedades con `{ get; private set; }`: `ProgramaBecaId`, `DonanteId`, `PeriodoAcademicoId`, `MontoAsignado` (`decimal > 0`), `Estado` (`EstadoAsignacionFondo`: `Asignada`, `Desasignada`), `FechaAsignacion` (`DateTimeOffset`), `AutorizadoPorUsuarioId` (`Guid`).
    - Métodos internos gobernados por `ProgramaBeca`: `CrearAsignada(...)` y `ReajustarODesasignar(decimal nuevoMontoAsignado, Guid usuarioModificacionId, DateTimeOffset fechaModificacion)` (si `nuevoMontoAsignado == 0m`, cambia `Estado = EstadoAsignacionFondo.Desasignada` y `MontoAsignado = 0m`; si `0 < nuevoMontoAsignado < MontoAsignado`, actualiza `MontoAsignado = nuevoMontoAsignado` manteniendo `EstadoAsignacionFondo.Asignada`).
- **Alternativas consideradas:**
  - Modelar tipos de programas mediante subclasificación (`ProgramaCoberturaTotal : ProgramaBeca`). Se descartó porque `openspec/specs/ms-prg/spec_prg.md` exige explícitamente composición de objetos de valor (`PoliticaCobertura`, `CriteriosElegibilidadPrograma`, `CondicionesPermanencia`) sobre herencia.

### 2. Coordinación Transaccional con `Donante` (`RN-PRG-02`) y con `UniversidadAliada` (`RN-PRG-01`)

- **Decisión:**
  - **Asignación de fondos (`[HU-11.2]`, `RN-PRG-02`):**
    - `Donante.ComprometerSaldoParaPrograma(decimal montoAsignado, Guid usuarioModificacionId, DateTimeOffset? fechaModificacion = null)` verifica `EstadoDonante == EstadoDonante.Activo` (`DonanteInactivoException` → `409 DONANTE_INACTIVO`), `montoAsignado > 0` y `montoAsignado <= SaldoDisponibleAsignacion` (`SaldoDonanteInsuficienteException` → `409 SALDO_DONANTE_INSUFICIENTE`), y descuenta `SaldoDisponibleAsignacion -= montoAsignado`.
    - `AsignarPresupuestoDonantesCommandHandler` carga el `ProgramaBeca` y cada `Donante` involucrado con tracking desde `ProgramasDbContext`, invoca `donante.ComprometerSaldoParaPrograma(...)` y `programa.AsignarFondoDonante(...)`, persiste todo en una sola llamada a `_unitOfWork.GuardarCambiosAsync(cancellationToken)` y encola `PresupuestoProgramaAsignado` en el Outbox.
  - **Reajuste / Desasignación (`[HU-11.2]`, `RN-PRG-02`):**
    - `ProgramaBeca.ReajustarAsignacionFondo(Guid asignacionId, decimal montoAReducir, ...)` valida que `Estado` sea `EstadoProgramaBeca.Borrador` o `EstadoProgramaBeca.Configurado`. Si el programa está en `EstadoProgramaBeca.Activo` (o `Cerrado`/`Archivado`), lanza `PresupuestoEnEjecucionException` (`409 Conflict`, código `PRESUPUESTO_EN_EJECUCION`).
    - Cuando el programa está en `Borrador` o `Configurado`, reduce la asignación y `PresupuestoTotalAsignado` (si `PresupuestoTotalAsignado` llega a `0m` estando en `Configurado`, regresa el programa a `Borrador` para preservar `RN-PRG-01`), y el handler invoca `donante.ReintegrarSaldoDePrograma(montoReducido, ...)` en la misma transacción.
  - **Configuración de elegibilidad y transición a `Configurado` (`[HU-11.3]`, `RN-PRG-01`):**
    - `ConfigurarCriteriosElegibilidadCommandHandler` carga el `ProgramaBeca` y consulta las `UniversidadAliada` correspondientes a `CriteriosElegibilidad.UniversidadesElegiblesIds`.
    - Verifica que todas las `universidadesElegiblesIds` existan, tengan `TieneConvenioVigente() == true`, y que todas las `carrerasElegiblesIds` existan y estén activas dentro de dichas universidades.
    - Invoca `programa.ConfigurarElegibilidadYPermanencia(criterios, condiciones, todasLasUniversidadesTienenConvenioVigente, todasLasCarrerasSonElegibles, usuarioModificacionId)`.
    - Dentro del dominio, `ProgramaBeca` valida `PresupuestoTotalAsignado > 0`, `CupoTotal > 0`, `todasLasUniversidadesTienenConvenioVigente` y `todasLasCarrerasSonElegibles`; si cualquiera falla, lanza `ProgramaNoConfigurableException` (`422 Unprocessable Entity`, código `PROGRAMA_NO_CONFIGURABLE`). Si todo se cumple, asigna los objetos de valor, cambia `Estado = EstadoProgramaBeca.Configurado` y emite `ProgramaBecaConfiguradoDominioEvent` → publicado en Outbox como `ProgramaBecaConfigurado`.

### 3. Persistencia EF Core (`ProgramaBecaConfiguracion` y `AsignacionFondoProgramaConfiguracion`)

- **Decisión:**
  - Ambas configuraciones invocan `ConfiguracionPersistencia.ConfigurarEntidadAuditable(builder)` para aplicar `.ValueGeneratedNever()` en `Id` y `.IsConcurrencyToken().ValueGeneratedNever()` en `Version`.
  - Tabla `programas_beca` (esquema `programas`):
    - Índice único `ux_programa_beca_codigo` sobre `codigo`.
    - `PoliticaCobertura` mapeado con `OwnsOne` en columnas `modalidad_cobertura`, `porcentaje_cobertura`, `monto_fijo_periodo`, `moneda_cobertura`.
    - `CriteriosElegibilidadPrograma` y `CondicionesPermanencia` (opcionales mientras el programa está en `Borrador`) mapeados con `OwnsOne` (o conversión JSON/columnas anulables en `programas_beca`, serializando `UniversidadesElegiblesIds` y `CarrerasElegiblesIds` como `jsonb` con `ValueComparer` para colecciones inmutables `IReadOnlyList<Guid>`).
    - Restricciones `CHECK`: `ck_programa_beca_cupo_positivo` (`cupo_total > 0`), `ck_programa_beca_duracion_positiva` (`duracion_periodos > 0`), `ck_programa_beca_fechas_vigencia` (`vigencia_hasta > vigencia_desde`), `ck_programa_beca_presupuesto_no_negativo` (`presupuesto_total_asignado >= 0`).
  - Tabla `asignaciones_fondo_programa` (esquema `programas`):
    - Clave foránea a `programas_beca(id)` e índice por `(programa_beca_id, donante_id, periodo_academico_id)`.
    - Restricción `CHECK`: `ck_asignacion_fondo_monto_no_negativo` (`monto_asignado >= 0`).

### 4. Diseño de Endpoints REST (`ProgramasController`)

- **Decisión:** Ubicado en `Ascenso.Programas.Api/Controllers/v1/ProgramasController.cs` bajo `[Route("api/v1/programas")]`:
  1. `POST /api/v1/programas` → `CrearProgramaBecaCommand` → `201 Created` (`ProgramaBecaDetalleDto`), `400 Bad Request`, `409 Conflict` (`CODIGO_PROGRAMA_DUPLICADO`).
  2. `GET /api/v1/programas` → `ConsultarProgramasQuery` (`pagina`, `tamanoPagina`, `estado`, `periodoInicialId`, `modalidadCobertura`, `busqueda`) → `200 OK` (`PaginadoResponse<ProgramaBecaResumenDto>`).
  3. `GET /api/v1/programas/{programaId:guid}` → `ObtenerProgramaPorIdQuery` → `200 OK` (`ProgramaBecaDetalleDto`) o `404 Not Found`.
  4. `POST /api/v1/programas/{programaId:guid}/asignaciones-presupuesto` → `AsignarPresupuestoDonantesCommand` → `201 Created` (`ProgramaBecaDetalleDto`), `400 Bad Request`, `404 Not Found`, `409 Conflict` (`SALDO_DONANTE_INSUFICIENTE`, `DONANTE_INACTIVO`).
  5. `POST /api/v1/programas/{programaId:guid}/asignaciones-presupuesto/{asignacionId:guid}/reajustar` → `ReajustarPresupuestoDonanteCommand` → `200 OK` (`ProgramaBecaDetalleDto`), `400 Bad Request`, `404 Not Found`, `409 Conflict` (`PRESUPUESTO_EN_EJECUCION`).
  6. `PUT /api/v1/programas/{programaId:guid}/configuracion-elegibilidad` → `ConfigurarCriteriosElegibilidadCommand` → `200 OK` (`ProgramaBecaDetalleDto`), `400 Bad Request` (`PROMEDIO_ACADEMICO_INVALIDO`), `404 Not Found`, `422 Unprocessable Entity` (`PROGRAMA_NO_CONFIGURABLE`).

## Risks / Trade-offs

- **[Risk] Mapeo en EF Core de objetos de valor opcionales (`CriteriosElegibilidadPrograma?` y `CondicionesPermanencia?`) que contienen listas `IReadOnlyList<Guid>`** → **Mitigación:** Configurar conversiones deterministas (`HasConversion` a JSON o `OwnsOne` con columnas anulables y `ValueComparer` explícito para `IReadOnlyList<Guid>`) y validarlo en `ModeloPersistenciaProgramasTests` verificando round-trip completo en estado `Borrador` (`null`) y en estado `Configurado` (con valores poblados).
- **[Risk] Concurrencia al comprometer saldo del mismo `Donante` en dos asignaciones simultáneas** → **Mitigación:** Tanto `Donante` como `ProgramaBeca` heredan de `EntidadAuditable<Guid>` con token de concurrencia optimista `Version` (`.IsConcurrencyToken().ValueGeneratedNever()`). Si otra transacción modifica el `Donante` concurrentemente, `ProgramasDbContext.GuardarCambiosAsync` captura `DbUpdateConcurrencyException` y la traduce a `ExcepcionConflictoConcurrencia` (`409 CONFLICTO_CONCURRENCIA`).
