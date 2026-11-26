# Bitácora de implementación: `rf9-2-int2-mspos` (HU 9.2)

- **Fecha de cierre:** 2026-11-26
- **Microservicio:** `MS-POS` (`Apps/services/ms-postulaciones`)
- **Alcance:** HU 9.2 — Ranking de postulantes, trazado automático de línea de corte por cupo y presupuesto (descontando renovaciones pendientes), advertencia de discrepancias sin revisar en o adyacentes al corte, bloqueo de empates en frontera (`RN-POS-07`), confirmación atómica `PROVISIONAL → CONFIRMADO` y contrato interno de adjudicabilidad (`GET /internal/v1/postulaciones/{postulacionId}/adjudicable`).
- **Estado de roadmap:** `HU 9.2` marcada `[x]` bajo `RF-09` y **`RF-09`** marcado `[x]` al 100% tanto en `openspec/roadmaps/por_integrante.md` como en `openspec/roadmaps/por_microservicio.md`.

---

## 1. Evidencia real de verificación (comandos ejecutados)

Todos los comandos .NET se ejecutaron desde `Apps/services/ms-postulaciones` tras finalizar el código de producción y pruebas:

| Verificación | Comando | Resultado real observado |
|---|---|---|
| **6.1 Restore bloqueado** | `dotnet restore Ascenso.Postulaciones.sln --locked-mode` | Exit code `0`; 8 proyectos restaurados; `git status --short -- "*packages.lock.json"` sin cambios. |
| **6.2 Compilación Release** | `dotnet build Ascenso.Postulaciones.sln --configuration Release --no-restore` | Exit code `0`; `0 Advertencia(s)`, `0 Errores` (`00:00:13.05`). |
| **6.3 Suite completa de pruebas** | `dotnet test Ascenso.Postulaciones.sln` | Exit code `0`; **1,433** pruebas superadas, `0` fallidas, `0` omitidas (incluyendo las 1,279 pruebas previas intactas + 154 nuevas pruebas de HU 9.2):<br>• `Ascenso.Postulaciones.Domain.Tests`: 589 superadas (`139 ms`)<br>• `Ascenso.Postulaciones.Application.Tests`: 434 superadas (`262 ms`)<br>• `Ascenso.Postulaciones.ArchitectureTests`: 11 superadas (`381 ms`, sin modificaciones)<br>• `Ascenso.Postulaciones.IntegrationTests` (incluye Testcontainers PostgreSQL con confirmación atómica, `CHECK`, índice único parcial y concurrencia optimista): 399 superadas (`11 s`) |
| **6.4 Formato estricto con `--include`** | `dotnet format Ascenso.Postulaciones.sln --verify-no-changes --no-restore --include <85 archivos .cs de develop...HEAD>` | Exit code `0` sobre los 85 archivos `.cs` nuevos o modificados obtenidos mediante `git diff --name-only develop...HEAD -- "*.cs"`, sin modificar archivos ajenos. |
| **6.5 Modelo EF Core** | `dotnet ef migrations has-pending-model-changes --project Ascenso.Postulaciones.Infrastructure --startup-project Ascenso.Postulaciones.Api --no-build` | Exit code `0`: `No changes have been made to the model since the last migration.` |
| **6.5 OpenSpec (cambio)** | `openspec validate rf9-2-int2-mspos --strict` | Exit code `0`: `Change 'rf9-2-int2-mspos' is valid`. |
| **6.5 OpenSpec (specs)** | `openspec validate --specs` | Exit code `0`: `Totals: 11 passed, 0 failed (11 items)` (0 fallos). |

---

## 2. Lista exhaustiva de ajustes mínimos de DI en pruebas previas y fábricas `WebApplicationFactory`

`git diff --stat develop...HEAD -- Apps/services/ms-postulaciones/tests/` confirma **`36 files changed, 5507 insertions(+)` y `0` eliminaciones**, preservando intactas todas las aserciones previas:

1. **`tests/Ascenso.Postulaciones.Application.Tests/Configuracion/InyeccionDependenciasTests.cs`** (`+31` líneas): registrados los 4 sustitutos `IRankingConvocatoriaRepository`, `IConsultaEstadoEvaluacionesConvocatoriaRepository`, `IDisponibilidadRankingClient` e `IProveedorMontoEstimadoBeca` y verificada la resolución DI de los 3 nuevos handlers (`ObtenerRankingConvocatoriaQueryHandler`, `ConfirmarRankingConvocatoriaCommandHandler`, `ConsultarAdjudicabilidadPostulacionQueryHandler`) y sus 3 validadores (`ObtenerRankingConvocatoriaQueryValidator`, `ConfirmarRankingConvocatoriaCommandValidator`, `ConsultarAdjudicabilidadPostulacionQueryValidator`), sin alterar verificaciones previas.
2. **`tests/Ascenso.Postulaciones.IntegrationTests/EnsambladoApiTests.cs`** (`+4` líneas): registrados en `WebApplicationFactory` los dobles mínimos `IRankingConvocatoriaRepository`, `IConsultaEstadoEvaluacionesConvocatoriaRepository`, `IDisponibilidadRankingClient` e `IProveedorMontoEstimadoBeca`.
3. **`tests/Ascenso.Postulaciones.IntegrationTests/Persistencia/TraductorExcepcionesPersistenciaTests.cs`** (`+12` líneas): agregado caso de prueba aditivo para la traducción del índice único parcial `ux_resultados_consolidados_ranking_convocatoria_posicion` (`ExcepcionConflictoConcurrencia` `409`), sin modificar aserciones previas.
4. **`tests/Ascenso.Postulaciones.IntegrationTests/Controllers/SwaggerApiTests.cs`** (`+92` líneas): agregados los 4 dobles de ranking (`IRankingConvocatoriaRepository`, `IConsultaEstadoEvaluacionesConvocatoriaRepository`, `IDisponibilidadRankingClient`, `IProveedorMontoEstimadoBeca`) en las 6 fábricas `WebApplicationFactory` existentes (`+4` líneas cada una) y añadida nueva prueba `Debe_DocumentarEndpointsDeRankingsYAdjudicabilidadInternaYSusEsquemas_Cuando_EntornoEsDevelopment` verificando las respuestas y esquemas Swagger de `RankingsController` y `PostulacionesInternasController`.
5. **Fábricas `WebApplicationFactory` en controladores previos** (`+8` líneas por archivo — `RemoveAll<T>` + `AddSingleton(Substitute.For<T>())` para los 4 nuevos puertos ISP):
   - `tests/Ascenso.Postulaciones.IntegrationTests/Controllers/AsignacionesEvaluacionControllerTests.cs`
   - `tests/Ascenso.Postulaciones.IntegrationTests/Controllers/ComitesEvaluadoresControllerTests.cs`
   - `tests/Ascenso.Postulaciones.IntegrationTests/Controllers/EvaluacionesControllerTests.cs`
   - `tests/Ascenso.Postulaciones.IntegrationTests/Controllers/ExcepcionEvaluadorUnicoControllerTests.cs`
   - `tests/Ascenso.Postulaciones.IntegrationTests/Controllers/PostulantesControllerTests.cs`
   - `tests/Ascenso.Postulaciones.IntegrationTests/Controllers/ResultadosConsolidadosControllerTests.cs`
   - `tests/Ascenso.Postulaciones.IntegrationTests/Controllers/RubricasControllerTests.cs`
6. **Fábricas `WebApplicationFactory` en suites de autorización previas** (`+8` líneas por archivo — `RemoveAll<T>` + `AddSingleton(Substitute.For<T>())` para los 4 nuevos puertos ISP):
   - `tests/Ascenso.Postulaciones.IntegrationTests/Seguridad/AutorizacionAsignacionesTests.cs`
   - `tests/Ascenso.Postulaciones.IntegrationTests/Seguridad/AutorizacionComitesTests.cs`
   - `tests/Ascenso.Postulaciones.IntegrationTests/Seguridad/AutorizacionConsolidacionTests.cs`
   - `tests/Ascenso.Postulaciones.IntegrationTests/Seguridad/AutorizacionEvaluacionesTests.cs`
   - `tests/Ascenso.Postulaciones.IntegrationTests/Seguridad/AutorizacionExcepcionEvaluadorUnicoTests.cs`
   - `tests/Ascenso.Postulaciones.IntegrationTests/Seguridad/AutorizacionRubricasTests.cs`

---

## 3. Resumen de decisiones de diseño aplicadas

- **Extensión aditiva de dominio (`ResultadoConsolidadoRanking`, `ClasificacionRanking`, objetos de valor y `ServicioCalculoRankingCorte`):**
  - Se agregaron propiedades anulables con `private set;` (`PosicionRanking`, `MontoEstimadoBeca`, `Clasificacion`, `FechaConfirmacionRanking`, `ConfirmadoPorUsuarioId`), la propiedad calculada `EsAdjudicable` y `ConfirmarRanking(...)` (`PROVISIONAL → CONFIRMADO`), junto con `DisponibilidadProgramaRanking`, `ElementoRankingEvaluado`, `CorteRankingConvocatoria` y `ServicioCalculoRankingCorte` (ordenamiento por `PuntajeConsolidado DESC`, desempate técnico determinista `(FechaConsolidacion ASC, Id ASC)`, corte por cupo y presupuesto efectivos sin saltar orden de mérito, detección de empates en frontera `422 EMPATES_SIN_RESOLVER` y advertencias de discrepancias sin revisar en o adyacentes al corte `422 DISCREPANCIAS_SIN_REVISAR_EN_CORTE`).
  - Se crearon 4 puertos ISP nuevos (`IRankingConvocatoriaRepository`, `IConsultaEstadoEvaluacionesConvocatoriaRepository`, `IDisponibilidadRankingClient`, `IProveedorMontoEstimadoBeca`) sin alterar ninguno de los 10 puertos previos protegidos por pruebas de contrato por reflexión.
- **Migración EF Core `20261126194657_ConfirmacionRankingConvocatoria` y ajuste del `CHECK` por orden alfabético en PostgreSQL:**
  - Generada migración separada agregando las 5 columnas anulables, el índice único parcial `ux_resultados_consolidados_ranking_convocatoria_posicion` (`WHERE posicion_ranking IS NOT NULL`, traducido en `TraductorExcepcionesPersistencia` a `ExcepcionConflictoConcurrencia` `409`) y 3 restricciones `CHECK` nuevas sin modificar las 7 `CHECK` de HU 9.1.
  - En `ck_resultados_consolidados_confirmacion_coherente` se incluyó la guarda `OR estado_confirmacion_ranking NOT IN ('PROVISIONAL', 'CONFIRMADO')` debido a que PostgreSQL evalúa las restricciones `CHECK` en orden alfabético (`ck_resultados_consolidados_confirmacion_coherente` antes que `ck_resultados_consolidados_estado_valido`). Esto garantiza que un intento de insertar un `estado_confirmacion_ranking` fuera de catálogo siga disparando `ck_resultados_consolidados_estado_valido`, manteniendo 100% intactas las pruebas previas de `ConsolidacionPostgreSqlTests`.
- **Clientes ACL *fail-closed* hacia `MS-PRG`:**
  - `DisponibilidadRankingClient`: consulta `GET /internal/v1/convocatorias/{convocatoriaId}` (`404` → `null`) y la ruta configurable `Ranking:RutaDisponibilidadPrograma` (por defecto `api/v1/programas/{programaId}/balance`, con soporte nativo para `comprometidoRenovacionesPendientes` y preparado para adoptar `internal/v1/programas/{programaId}/disponibilidad` por configuración; `404` de programa → `ProgramaBecaNoEncontradoException`; errores HTTP, red, timeout o JSON inválido → `503 DependenciaNoDisponibleException`).
  - `ProveedorMontoEstimadoBecaClient`: consulta `Ranking:RutaDetallePrograma` (`api/v1/programas/{programaId}`) para proyectar el monto estimado por beca (`montoFijoPeriodo`, cobertura `PARCIAL` o `TOTAL` con `Ranking:MontoEstimadoBecaReferenciaPorDefecto = 1000.00m`) en modo *fail-closed*, desacoplado para adoptar aranceles por carrera cuando exista la entidad `Postulacion`.
- **Controladores REST y contrato interno (`RankingsController` y `PostulacionesInternasController`):**
  - `RankingsController` (`api/v1/rankings`): `GET convocatorias/{convocatoriaId}` (`ConsultaConsolidacion`) y `POST convocatorias/{convocatoriaId}/confirmar` (`GestionConsolidacion`), reutilizando `IConvocatoriaEvaluacionClient` para exigir cierre formal del periodo de revisión antes de confirmar.
  - `PostulacionesInternasController` (`internal/v1/postulaciones/{postulacionId:guid}/adjudicable`): política aditiva `ConsultaAdjudicabilidadInterna` (`COORDINADOR_PROGRAMA`, `DIRECCION`, o token de servicio con `scope` `postulaciones.internal.read` / `permissions` `postulaciones:leer`), exponiendo `Adjudicable = (EstadoConfirmacionRanking == CONFIRMADO && Clasificacion == DENTRO_DE_CUPO)` y `MotivoNoAdjudicable` (`RANKING_NO_CONFIRMADO` o `FUERA_DE_CUPO_EN_LISTA_DE_ESPERA`).

---

## 4. Preguntas abiertas heredadas de `design.md` (para seguimiento futuro)

1. **Contrato definitivo `MS-POS → MS-PRG` (`GET /internal/v1/programas/{id}/disponibilidad`):** confirmar con el Integrante 3 (`MS-PRG`) cuándo se publicará el endpoint interno de disponibilidad de DDD V7 §6.2 y si su campo `presupuestoDisponible` vendrá ya neto de `comprometidoRenovacionesPendientes` (como hoy en `/balance` vía `presupuestoReservado`) o bruto para que `MS-POS` reste `comprometidoRenovacionesPendientes`.
2. **Resolución de `montoEstimadoBeca` por carrera/universidad:** cuando el Integrante 1 incorpore la entidad `Postulacion` (`universidadAliadaId`, `carreraId`), confirmar si `MS-PRG` expondrá una consulta en lote de aranceles vigentes o si el adaptador `IProveedorMontoEstimadoBeca` consultará `GET /api/v1/universidades/{id}/carreras/{id}/costos-matricula` por par único `(universidad, carrera)`.
3. **Enriquecimiento de `GET /internal/v1/postulaciones/{postulacionId}/adjudicable` para `MS-BEC`:** confirmar con el Integrante 3/4 (`MS-BEC`) la sincronización de los campos de datos de estudiante y programa (`programaBecaId`, `periodoAcademicoId`, `universidadAliadaId`, `carreraId`, `postulanteEstudianteId`, `nombreEstudiante`, `numeroDocumentoEstudiante`) una vez que `Postulacion` exista en `MS-POS`, así como el nombre definitivo del `scope`/`permission` y audiencia del *Service Account Token* en Keycloak (`MS-PLT`).
4. **Criterio de adyacencia en advertencia de discrepancias:** en HU 9.2 se considera adyacente a la línea de corte todo postulante dentro de cupo, en el grupo empatado en frontera o perteneciente al primer grupo de puntaje de `LISTA_DE_ESPERA` inmediatamente debajo del corte; confirmar con negocio si en convocatorias con alta varianza desean un margen configurable en puntos debajo del puntaje de corte (p. ej. `±5.00` puntos).
5. **Orden de mérito ante caso límite presupuestario sin empate (RF-10):** en HU 9.2, cuando el siguiente postulante por mérito no cabe en el presupuesto restante (sin empate), la línea de corte se traza antes de él sin saltar a un postulante de menor puntaje con beca más económica; confirmar que el eventual aprovechamiento de ese remanente quedará bajo decisión justificada del coordinador en RF-10.
