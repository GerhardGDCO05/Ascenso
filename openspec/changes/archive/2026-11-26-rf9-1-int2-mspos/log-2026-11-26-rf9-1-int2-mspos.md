# Bitácora de implementación: `rf9-1-int2-mspos` (HU 9.1)

- **Fecha de cierre:** 2026-11-26
- **Microservicio:** `MS-POS` (`Apps/services/ms-postulaciones`)
- **Alcance:** HU 9.1 — Consolidación automática de puntuaciones definitivas, alertas de discrepancia y excepción de evaluador único (sin confirmación final de ranking ni cupo/presupuesto `MS-PRG`, que corresponden a HU 9.2).
- **Estado de roadmap:** `HU 9.1` marcada `[x]` bajo `RF-09`; `RF-09` permanece `[ ]` pendiente por `HU 9.2`.

---

## 1. Evidencia real de verificación (comandos ejecutados)

Todos los comandos .NET se ejecutaron desde `Apps/services/ms-postulaciones` tras la última edición de código:

| Verificación | Comando | Resultado real observado |
|---|---|---|
| **6.1 Restore bloqueado** | `dotnet restore Ascenso.Postulaciones.sln --locked-mode` | Exit code `0`; 8 proyectos restaurados; `git status --short packages.lock.json **/packages.lock.json` sin cambios. |
| **6.2 Compilación Release** | `dotnet build Ascenso.Postulaciones.sln --configuration Release` | Exit code `0`; `0 Advertencia(s)`, `0 Errores` (`26.46s`). |
| **6.3 Suite completa de pruebas** | `dotnet test Ascenso.Postulaciones.sln` | Exit code `0`; **1,279** pruebas pasadas, `0` fallidas, `0` omitidas:<br>• `Ascenso.Postulaciones.Domain.Tests`: 530 superadas (`335 ms`)<br>• `Ascenso.Postulaciones.Application.Tests`: 404 superadas (`402 ms`)<br>• `Ascenso.Postulaciones.ArchitectureTests`: 11 superadas (`349 ms`, sin modificaciones)<br>• `Ascenso.Postulaciones.IntegrationTests` (incluye Testcontainers PostgreSQL y concurrencia `C5`): 334 superadas (`10 s`) |
| **6.4 Formato estricto con `--include`** | `dotnet format Ascenso.Postulaciones.sln --verify-no-changes --no-restore --include <94 archivos .cs de develop...HEAD>` | Exit code `0` sobre los 94 archivos `.cs` nuevos o modificados de la rama (`git diff --name-only develop...HEAD -- "*.cs"`), sin tocar archivos ajenos. |
| **6.5 Modelo EF Core** | `dotnet ef migrations has-pending-model-changes --project Ascenso.Postulaciones.Infrastructure --startup-project Ascenso.Postulaciones.Api --no-build` | Exit code `0`: `No changes have been made to the model since the last migration.` |
| **6.5 OpenSpec (cambio)** | `openspec validate rf9-1-int2-mspos --strict` | Exit code `0`: `Change 'rf9-1-int2-mspos' is valid`. |
| **6.5 OpenSpec (specs)** | `openspec validate --specs` | Exit code `0`: `Totals: 10 passed, 0 failed (10 items)` (0 fallos). |

---

## 2. Lista exhaustiva de ajustes mínimos en pruebas previas y fábricas `WebApplicationFactory`

Ninguna aserción previa fue modificada ni eliminada (`git diff --stat develop...HEAD -- Apps/services/ms-postulaciones/tests/` muestra `0 deletions`). Solo se inyectaron sustitutos/dobles mínimos para las nuevas dependencias de constructor o DI:

1. **`tests/Ascenso.Postulaciones.Application.Tests/Evaluaciones/Comandos/EnviarEvaluacionDefinitivaCommandHandlerTests.cs`** (`+4` líneas): agregado campo `IConsolidadorPostulacionEvaluacion _consolidadorPostulacion = Substitute.For<IConsolidadorPostulacionEvaluacion>();` pasado al constructor de `EnviarEvaluacionDefinitivaCommandHandler`.
2. **`tests/Ascenso.Postulaciones.Application.Tests/Evaluaciones/Comandos/EnviarEvaluacionDefinitivaVentanaRevisionTests.cs`** (`+4` líneas): agregado campo `IConsolidadorPostulacionEvaluacion _consolidadorPostulacion = Substitute.For<IConsolidadorPostulacionEvaluacion>();` pasado al constructor de `EnviarEvaluacionDefinitivaCommandHandler`.
3. **`tests/Ascenso.Postulaciones.Application.Tests/Asignaciones/Comandos/AutorizarExcepcionEvaluadorUnicoCommandHandlerTests.cs`** (`+4` líneas): agregado campo `IConsolidadorPostulacionEvaluacion _consolidadorPostulacion = Substitute.For<IConsolidadorPostulacionEvaluacion>();` pasado al constructor de `AutorizarExcepcionEvaluadorUnicoCommandHandler`.
4. **`tests/Ascenso.Postulaciones.IntegrationTests/Persistencia/EvaluacionPostgreSqlTests.cs`** (`+10` líneas): agregada clase privada `ConsolidadorPostulacionNoOp : IConsolidadorPostulacionEvaluacion` pasada al constructor de `EnviarEvaluacionDefinitivaCommandHandler` para mantener aisladas las pruebas previas de evaluación en PostgreSQL.
5. **`tests/Ascenso.Postulaciones.IntegrationTests/Persistencia/ExcepcionEvaluadorUnicoPostgreSqlTests.cs`** (`+2` líneas): pasado `Substitute.For<IConsolidadorPostulacionEvaluacion>()` al constructor de `AutorizarExcepcionEvaluadorUnicoCommandHandler`.
6. **`tests/Ascenso.Postulaciones.Application.Tests/Configuracion/InyeccionDependenciasTests.cs`** (`+43` líneas): ampliación aditiva para verificar la resolución DI de los nuevos handlers (`ConsolidarPostulacionCommandHandler`, `RevisarDiscrepanciaConsolidacionCommandHandler`, `ObtenerResultadoConsolidadoPostulacionQueryHandler`, `ListarResultadosConsolidadosConvocatoriaQueryHandler`), validadores y `IConsolidadorPostulacionEvaluacion → ConsolidadorPostulacionEvaluacionAdapter`.
7. **`tests/Ascenso.Postulaciones.IntegrationTests/EnsambladoApiTests.cs`** (`+3` líneas): registrados en `WebApplicationFactory` los dobles mínimos `IResultadoConsolidadoRepository`, `IConsultaDatosConsolidacionRepository` e `IProveedorParametrosConsolidacion`.
8. **Fábricas `WebApplicationFactory` en pruebas previas de controladores, Swagger y autorización** (`+6` líneas por archivo en `Controllers/` y `Seguridad/`, y ampliación de `SwaggerApiTests.cs`):
   - `Controllers/AsignacionesEvaluacionControllerTests.cs`
   - `Controllers/ComitesEvaluadoresControllerTests.cs`
   - `Controllers/EvaluacionesControllerTests.cs`
   - `Controllers/ExcepcionEvaluadorUnicoControllerTests.cs`
   - `Controllers/PostulantesControllerTests.cs`
   - `Controllers/RubricasControllerTests.cs`
   - `Controllers/SwaggerApiTests.cs`
   - `Seguridad/AutorizacionAsignacionesTests.cs`
   - `Seguridad/AutorizacionComitesTests.cs`
   - `Seguridad/AutorizacionEvaluacionesTests.cs`
   - `Seguridad/AutorizacionExcepcionEvaluadorUnicoTests.cs`
   - `Seguridad/AutorizacionRubricasTests.cs`

---

## 3. Resumen de decisiones de diseño aplicadas

- **Agregado `ResultadoConsolidadoRanking` y tabla `postulaciones.resultados_consolidados_ranking`:**
  - Generación de migración EF Core dedicada `20261126153413_ResultadosConsolidadosRanking` (junto con su `.Designer.cs` y `PostulacionesDbContextModelSnapshot.cs`) sobre esquema `postulaciones`.
  - Relación 1:1 por postulación garantizada por el índice único `ux_resultados_consolidados_ranking_postulacion`, traducido en `TraductorExcepcionesPersistencia` a `ResultadoConsolidadoDuplicadoException` (`409 RESULTADO_CONSOLIDADO_DUPLICADO`).
  - Restricciones `CHECK` en PostgreSQL para `estado_confirmacion_ranking` (`PROVISIONAL`/`CONFIRMADO`), rangos `[0, 100]` / `(0, 100]`, coherencia de excepción de evaluador único, coherencia de alerta de discrepancia y coherencia de revisión de discrepancia.
  - Índices compuestos `ix_resultados_consolidados_ranking_convocatoria_puntaje` e `ix_resultados_consolidados_ranking_convocatoria_discrepancia`.
- **Puerto ISP `IProveedorParametrosConsolidacion`:**
  - Desacopla el origen de `EvaluadoresMinimosRequeridos` (`2` por defecto) y `UmbralDiscrepanciaPuntos` (`20.00` por defecto) mediante `ConsolidacionOptions` y `ProveedorParametrosConsolidacionConfigurado`, sin modificar `Rubricas` y dejando listo el reemplazo cuando RF-07 incorpore umbrales por rúbrica.
  - Cada fila persiste el snapshot `umbral_discrepancia_aplicado` para trazabilidad histórica.
- **Disparo automático e idempotencia bajo concurrencia `C5`:**
  - `ConsolidadorPostulacionEvaluacionAdapter` encapsula `ConsolidarPostulacionCommandHandler.IntentarConsolidarAutomaticamenteAsync` tras `GuardarCambiosAsync` en `EnviarEvaluacionDefinitivaCommandHandler` y `AutorizarExcepcionEvaluadorUnicoCommandHandler`.
  - Ante colisión concurrente (`ResultadoConsolidadoDuplicadoException` o `ExcepcionConflictoConcurrencia`), el handler recarga desde BD vía `RecargarParaActualizarPorPostulacionAsync`, verifica `CoincideConCalculo` para retornar idempotentemente sin error cuando coincide, o ejecuta 1 reintento acotado si llegó una evaluación más reciente; verificado en `ConsolidacionPostgreSqlTests` con dos `DbContext` sincronizados por barrera en PostgreSQL real.

---

## 4. Preguntas abiertas heredadas de `design.md` (para seguimiento futuro)

1. **Valor por defecto del umbral de discrepancia (`UmbralDiscrepanciaPuntos = 20.00`):** confirmar con negocio si `20.00` puntos sobre escala `[0.00, 100.00]` es el umbral institucional por defecto hasta que una extensión futura de RF-07 incorpore el umbral configurable por rúbrica (`RubricaEvaluacion.UmbralDiscrepanciaPuntos`).
2. **Facultad de `DIRECCION` para revisar discrepancias:** en HU 9.1 `DIRECCION` tiene acceso de solo lectura (`ConsultaConsolidacion`) y solo `COORDINADOR_PROGRAMA` marca discrepancias como revisadas (`GestionConsolidacion`); confirmar si `DIRECCION` debe poder revisar discrepancias directamente sin delegación por conflicto de interés (RF-39).
3. **Bloqueo de confirmación de ranking con discrepancias sin revisar (HU 9.2):** confirmar si en HU 9.2 la confirmación del ranking (`PROVISIONAL → CONFIRMADO`) deberá exigir que todas las postulaciones con `alertaDiscrepancia == true` tengan `discrepanciaRevisada == true`, o si la revisión es solo informativa/recomendada.
4. **Comportamiento ante invalidación posterior por conflicto (RF-39):** si una postulación ya consolidada en `PROVISIONAL` pierde una evaluación por `INVALIDADA_POR_CONFLICTO` quedando por debajo del mínimo requerido, confirmar si en RF-39 el resultado provisional se marcará pendiente/obsoleto o se recalculará únicamente al completarse la reasignación.
