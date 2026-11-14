# Design: Convenios con Universidades Aliadas, Carreras Elegibles y Versionado Histórico de Costos de Matrícula (`rf14-int3-msprg`)

## Context

El microservicio `MS-PRG` (`Ascenso.Programas`) ya cuenta con su infraestructura de persistencia EF Core (`ProgramasDbContext` en esquema `programas`), `ConfiguracionPersistencia`, soporte de `Transactional Outbox` de MassTransit (`inbox_state`, `outbox_state`, `outbox_message`), middleware `ManejadorExcepcionesDominio` (`ProblemDetails`) y el módulo de Donantes (`RF-13`) en producción local.

Para implementar el **RF-14** (`[HU-14.1]` y `[HU-14.2]`) cumpliendo `openspec/specs/ms-prg/spec_prg.md` (`RN-PRG-05`) y `docs/convenciones_c_y_ddd.md`, se incorpora el agregado `UniversidadAliada` junto con sus entidades internas (`Convenio`, `CarreraElegible`, `CostoMatriculaReferencia`), aplicando las lecciones aprendidas de persistencia EF Core en `MS-PRG`:
1. Todo identificador `Guid` generado en dominio con `UuidV7.Nuevo()` (`Guid.CreateVersion7()`) se configura explícitamente con `.ValueGeneratedNever()` en EF Core para que las entidades hijas agregadas a colecciones navegables sean detectadas por el Change Tracker como `EntityState.Added` (`INSERT`) y nunca como `Modified` (`UPDATE`).
2. El token de concurrencia optimista `Version` se configura con `.IsConcurrencyToken().ValueGeneratedNever()`.
3. Todos los campos privados respetan estrictamente el prefijo `_camelCase` exigido por `.editorconfig` (`IDE1006` / `dotnet format --verify-no-changes`).

## Goals / Non-Goals

**Goals:**
- Modelar en `Ascenso.Programas.Domain/Universidades/` el agregado raíz `UniversidadAliada`, las entidades `Convenio`, `CarreraElegible` y `CostoMatriculaReferencia`, los objetos de valor `ContactoEnlace` y `CuentaBancariaInstitucional`, las enumeraciones (`EstadoUniversidad`, `EstadoConvenio`, `EstadoVersionCosto`), los eventos de dominio, las excepciones de negocio (incluyendo código HTTP `422` para `CONVENIO_VENCIDO`) y el puerto `IUniversidadAliadaRepository`.
- Implementar en `Ascenso.Programas.Application/Universidades/` los comandos y consultas CQRS-Lite para `[HU-14.1]` y `[HU-14.2]`, sus validadores FluentValidation, DTOs y contratos de eventos de integración (`ConvenioUniversidadRegistrado`, `ConvenioUniversidadVencido`, `CostoMatriculaActualizado`).
- Configurar en `Ascenso.Programas.Infrastructure/Persistence/` el mapeo relacional de las cuatro tablas (`universidades_aliadas`, `convenios_universidad`, `carreras_elegibles`, `costos_matricula_referencia`), el repositorio `UniversidadAliadaRepository` y la migración EF Core correspondiente.
- Exponer en `Ascenso.Programas.Api` el controlador `UniversidadesController` bajo `/api/v1/universidades`.

**Non-Goals:**
- Subida directa de binarios PDF desde `MS-PRG` hacia Firebase Storage; al igual que en `RF-13`, el cliente/gateway carga previamente el PDF del convenio en Firebase Storage y suministra `urlDocumentoPdfFirebase` a `MS-PRG`.
- Modificación retroactiva de contratos o becas emitidas en `MS-BEC` cuando cambia un arancel; por diseño (`RN-PRG-05`), `MS-PRG` preserva inmutables las versiones `Historico` (`CostoMatriculaReferencia`) para que las becas existentes conserven su *snapshot* original.

## Decisions

### 1. Modelado del Agregado `UniversidadAliada` y sus Entidades Internas (`Ascenso.Programas.Domain`)

- **Decisión:**
  - `UniversidadAliada : AggregateRoot` (`EntidadAuditable<Guid>`) es la única raíz de agregado del módulo en `Ascenso.Programas.Domain/Universidades/Agregados/UniversidadAliada.cs`, gobernando las colecciones privadas `_convenios` (`List<Convenio>`) y `_carreras` (`List<CarreraElegible>`).
  - Objetos de Valor inmutables (`sealed record` en `Domain/Universidades/ObjetosValor/`):
    - `ContactoEnlace`: `Nombre`, `Email`, `Telefono` (validados al instanciar).
    - `CuentaBancariaInstitucional`: `Banco`, `TipoCuenta`, `NumeroCuentaCifrado`, `RifTitular` (opcional en el alta de la universidad; si se provee, valida todos sus campos).
  - Entidad `Convenio : EntidadAuditable<Guid>` (`Domain/Universidades/Entidades/Convenio.cs`):
    - Propiedades con `{ get; private set; }`: `UniversidadAliadaId`, `NumeroConvenio`, `TerminosResumen`, `FechaInicio` (`DateOnly`), `FechaFin` (`DateOnly`), `UrlDocumentoPdfFirebase`, `PermiteIntercambioAutomaticoDatos` (`bool`), `Estado` (`EstadoConvenio`: `Vigente`, `Vencido`, `Suspendido`).
    - Invariantes en construcción: `FechaFin > FechaInicio` (`FechasConvenioInvalidasException` → HTTP `400`, código `FECHAS_CONVENIO_INVALIDAS`) y `UrlDocumentoPdfFirebase` no vacía con extensión `.pdf` ignorando query strings (`DocumentoConvenioPdfRequeridoException` → HTTP `400`, código `DOCUMENTO_CONVENIO_PDF_REQUERIDO`).
    - Método interno `MarcarVencido(Guid usuarioModificacionId, DateTimeOffset fechaModificacion)`: transiciona a `EstadoConvenio.Vencido`.
  - Entidades `CarreraElegible : EntidadAuditable<Guid>` y `CostoMatriculaReferencia : EntidadAuditable<Guid>`:
    - `CarreraElegible` mantiene `UniversidadAliadaId`, `CodigoCarrera` (normalizado en mayúsculas), `Nombre`, `DuracionSemestres` (`> 0`), `Activa` (`bool`) y la colección privada `_costosMatricula` (`List<CostoMatriculaReferencia>`).
    - `CostoMatriculaReferencia` mantiene `CarreraElegibleId`, `PeriodoAcademicoId`, `NumeroVersion` (`int >= 1`), `MontoReferenciaMatricula` (`decimal > 0` con 2 decimales), `Moneda` (`"USD"`), `FechaVigenciaDesde` (`DateTimeOffset`), `FechaVigenciaHasta` (`DateTimeOffset?`) y `Estado` (`EstadoVersionCosto`: `Vigente`, `Historico`).
    - Al invocar `UniversidadAliada.ActualizarCostoMatricula(Guid carreraId, string periodoAcademicoId, decimal montoReferenciaMatricula, string moneda, Guid usuarioModificacionId, DateTimeOffset? fechaVigencia = null)`:
      1. Localiza la `CarreraElegible` (o lanza `CarreraElegibleNoEncontradaException` → `404`).
      2. Busca si existe una versión en `EstadoVersionCosto.Vigente` para ese `periodoAcademicoId`.
      3. Si existe, invoca `versionAnterior.MarcarComoHistorico(fechaEfectiva, usuarioModificacionId)` cerrando `FechaVigenciaHasta = fechaEfectiva` y cambiando su estado a `EstadoVersionCosto.Historico`, y calcula `nuevoNumeroVersion = versionAnterior.NumeroVersion + 1` (o `1` si es el primer arancel del período).
      4. Crea la nueva entidad `CostoMatriculaReferencia` con `UuidV7.Nuevo(fechaEfectiva)`, `NumeroVersion = nuevoNumeroVersion`, `Estado = EstadoVersionCosto.Vigente` y `FechaVigenciaHasta = null`.
  - Regla `RN-PRG-05` sobre convenios vencidos:
    - `UniversidadAliada.TieneConvenioVigente(DateOnly? fechaReferencia = null)` verifica si existe al menos un `Convenio` en `EstadoConvenio.Vigente` con `FechaFin >= fechaReferencia`.
    - `UniversidadAliada.ValidarConvenioVigenteParaNuevasPostulaciones(DateOnly? fechaReferencia = null)` lanza `ConvenioUniversidadVencidoException` (`422 Unprocessable Entity`, código `CONVENIO_VENCIDO`) cuando la universidad carece de convenio vigente, bloqueando nuevas postulaciones/ofertas sin afectar becas ya activas.
- **Alternativas consideradas:**
  - Sobrescribir en sitio el monto de `CostoMatriculaReferencia` al cambiar el arancel. Se descartó porque viola frontalmente `RN-PRG-05`, que exige conservar las versiones anteriores en estado `HISTORICO` inmutable con número de versión incremental `N+1`.

### 2. Configuración EF Core, Tracking y Prevención de `DbUpdateConcurrencyException`

- **Decisión:**
  - En `UniversidadAliadaConfiguracion`, `ConvenioConfiguracion`, `CarreraElegibleConfiguracion` y `CostoMatriculaReferenciaConfiguracion` se invoca `ConfiguracionPersistencia.ConfigurarEntidadAuditable(builder)` (que ya aplica `.ValueGeneratedNever()` sobre `Id` y `.IsConcurrencyToken().ValueGeneratedNever()` sobre `Version`).
  - `ContactoEnlace` y `CuentaBancariaInstitucional` se mapean en la tabla `universidades_aliadas` mediante `OwnsOne` (`CuentaBancariaInstitucional` con columnas anulables al ser opcional en el registro).
  - Índices únicos y restricciones `CHECK`:
    - `ux_universidad_aliada_codigo_institucional` único sobre `universidades_aliadas(codigo_institucional)`.
    - `ux_carrera_elegible_universidad_codigo` único compuesto sobre `carreras_elegibles(universidad_aliada_id, codigo_carrera)`.
    - `ux_costo_matricula_carrera_periodo_version` único compuesto sobre `costos_matricula_referencia(carrera_elegible_id, periodo_academico_id, numero_version)`.
    - `ck_convenio_fechas_validas` (`fecha_fin > fecha_inicio`), `ck_carrera_duracion_positiva` (`duracion_semestres > 0`) y `ck_costo_matricula_monto_positivo` (`monto_referencia_matricula > 0`).
  - `UniversidadAliadaRepository.ObtenerPorIdAsync` carga el grafo del agregado **con tracking** (`Include(u => u.Convenios)` e `Include(u => u.Carreras).ThenInclude(c => c.CostosMatricula)`) sin `AsNoTracking()` ni `_contexto.Update(...)`, garantizando que al agregar un `Convenio`, una `CarreraElegible` o una versión `N+1` de `CostoMatriculaReferencia`, EF Core marque las nuevas entidades como `Added` (`INSERT`) y la versión anterior `N` como `Modified` (`UPDATE estado = 'HISTORICO'`).

### 3. Diseño de Endpoints REST (`UniversidadesController`)

- **Decisión:** Ubicado en `Ascenso.Programas.Api/Controllers/v1/UniversidadesController.cs` bajo `[Route("api/v1/universidades")]`:
  1. `POST /api/v1/universidades` → `RegistrarUniversidadConvenioCommand` → `201 Created`, `400 Bad Request`, `409 Conflict` (`CODIGO_INSTITUCIONAL_DUPLICADO`).
  2. `GET /api/v1/universidades` → `ConsultarUniversidadesQuery` (`pagina`, `tamanoPagina`, `estado`, `busqueda`) → `200 OK` (`PaginadoResponse<UniversidadResumenDto>`).
  3. `GET /api/v1/universidades/{universidadId:guid}` → `ObtenerUniversidadPorIdQuery` → `200 OK` (`UniversidadDetalleDto`) o `404 Not Found`.
  4. `POST /api/v1/universidades/{universidadId:guid}/convenios` → `RegistrarNuevoConvenioCommand` → `201 Created` (`ConvenioDto`), `400 Bad Request` o `404 Not Found`.
  5. `POST /api/v1/universidades/{universidadId:guid}/convenios/{convenioId:guid}/vencer` → `MarcarConvenioVencidoCommand` → `200 OK` (`ConvenioDto`) o `404 Not Found`.
  6. `POST /api/v1/universidades/{universidadId:guid}/carreras` → `AgregarCarreraElegibleCommand` → `201 Created` (`CarreraElegibleDto`), `400 Bad Request`, `404 Not Found`, `409 Conflict` (`CODIGO_CARRERA_DUPLICADO`) o `422 Unprocessable Entity` (`CONVENIO_VENCIDO` si se exige convenio vigente).
  7. `POST /api/v1/universidades/{universidadId:guid}/carreras/{carreraId:guid}/costos-matricula` → `ActualizarCostoMatriculaCommand` → `201 Created` (`CostoMatriculaReferenciaDto`), `400 Bad Request` o `404 Not Found`.
  8. `GET /api/v1/universidades/{universidadId:guid}/carreras/{carreraId:guid}/costos-matricula` → `ConsultarHistorialCostosCarreraQuery` (`periodoAcademicoId` opcional) → `200 OK` (`IReadOnlyList<CostoMatriculaReferenciaDto>`) o `404 Not Found`.

## Risks / Trade-offs

- **[Risk] Entidades hijas con `Guid` UUIDv7 marcadas como `Modified` en EF Core al agregarse a colecciones navegables** → **Mitigación:** Reutilizar `ConfiguracionPersistencia.ConfigurarEntidadAuditable(builder)` en las 4 entidades (`UniversidadAliada`, `Convenio`, `CarreraElegible`, `CostoMatriculaReferencia`) para garantizar `ValueGeneratedNever()` en `Id` y `Version`, y verificarlo explícitamente con una prueba de Change Tracker en `IntegrationTests`.
- **[Risk] Concurrencia al publicar dos actualizaciones de costo de matrícula simultáneas para la misma carrera y período** → **Mitigación:** La modificación ocurre a través del agregado raíz `UniversidadAliada` (protegido por el token de concurrencia `Version`) y la tabla `costos_matricula_referencia` cuenta además con el índice único `ux_costo_matricula_carrera_periodo_version`.

## Migration Plan

1. Implementar las clases de `Domain`, `Application`, `Infrastructure` y `Api` siguiendo las tareas de `tasks.md`.
2. Generar la migración EF Core `ConveniosUniversidadesYCostos` en `Ascenso.Programas.Infrastructure/Persistence/Migrations/`.
3. Verificar `dotnet format --verify-no-changes`, `dotnet build --configuration Release` (`0 warnings`) y `dotnet test --configuration Release` (`100%` en verde).
