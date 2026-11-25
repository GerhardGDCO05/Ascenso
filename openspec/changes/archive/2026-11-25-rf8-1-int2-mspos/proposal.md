# Proposal

## Why

Las historias previas en `ms-pos` ya permiten publicar y bloquear la `RubricaEvaluacion` (RF-07: HU 7.1 y HU 7.2) y conformar comités y asignar postulaciones a evaluadores independientes (RF-06: HU 6.1, HU 6.2 y HU 6.3), pero los evaluadores todavía no pueden registrar sus puntuaciones ni comentarios sobre las postulaciones asignadas. RF-08 exige habilitar el registro de puntuaciones por criterio y comentarios respetando los rangos de la rúbrica, la completitud obligatoria al enviar, la inmutabilidad tras el envío definitivo, el bloqueo automático de la rúbrica en la primera calificación (`RN-POS-05`) y el aislamiento estricto de la evaluación ciega (`RN-POS-04`, `RN-PLT-02`), paso indispensable previo a la consolidación y ranking (RF-09).

## What Changes

- **Nuevo agregado `Evaluacion` y entidad hija `CalificacionCriterio`** (vocabulario ubicuo de DDD V7 §2.3.B) con objeto de valor inmutable `DatosCalificacionCriterio` y enum `EstadoEvaluacion` (`BORRADOR`, `ENVIADA_DEFINITIVA`, `INVALIDADA_POR_CONFLICTO`) persistido con `CodigoEnum`.
- **Flujo de borrador y envío definitivo (HU 8.1):**
  - El `MIEMBRO_COMITE` asignado puede guardar avances en `BORRADOR` (una o más calificaciones de criterios dentro de `[puntajeMinimo, puntajeMaximo]` y comentarios opcionales por criterio y generales) mientras el plazo `fechaLimiteEvaluacion` esté vigente. El primer borrador transiciona `AsignacionEvaluacion` de `PENDIENTE → EN_PROGRESO` y dispara el bloqueo automático de `RubricaEvaluacion` (`PUBLICADA → BLOQUEADA` con motivo `PRIMERA_CALIFICACION`, reutilizando `BloquearRubricaCommandHandler` de HU 7.2 de forma idempotente y segura bajo concurrencia `C5`).
  - El `MIEMBRO_COMITE` asignado puede enviar la evaluación definitiva (`BORRADOR → ENVIADA_DEFINITIVA`) únicamente cuando el 100% de los criterios de la rúbrica tienen puntaje válido dentro de su rango y el plazo está vigente; el servidor calcula de forma determinista `puntajeTotalPonderado` ∈ `[0.00, 100.00]` (fórmulas DDD V7 §2.3.B), registra `fechaEnvio` UTC, sella la inmutabilidad de la evaluación y transiciona `AsignacionEvaluacion` de `EN_PROGRESO → COMPLETADA`.
- **Transiciones explícitas de estado en `AsignacionEvaluacion`:** métodos de dominio aditivos `IniciarEvaluacion` (`PENDIENTE → EN_PROGRESO`) y `CompletarEvaluacion` (`EN_PROGRESO → COMPLETADA`), sin alterar la lógica ni contratos existentes de HU 6.2 y HU 6.3.
- **Consultas CQRS con evaluación ciega estricta (`RN-POS-04`, `RN-PLT-02`):**
  - `MIEMBRO_COMITE` consulta únicamente su propia evaluación por asignación y su listado paginado de evaluaciones propias (identidad tomada del claim `sub` del JWT). Todo intento de leer, editar o enviar una asignación o evaluación de otro evaluador responde `403 Forbidden` con `codigo = EVALUACION_CONFIDENCIAL`.
  - `COORDINADOR_PROGRAMA` no puede puntuar ni ver borradores privados, y dispone de una consulta de supervisión de solo lectura por postulación que expone únicamente evaluaciones en estado distinto de `BORRADOR` (`ENVIADA_DEFINITIVA` y eventuales `INVALIDADA_POR_CONFLICTO`).
  - `ADMIN_FINANCIERO` recibe siempre `403 Forbidden` con `codigo = PERMISO_DENEGADO` en formato `ProblemDetails`.
- **Persistencia en el esquema `postulaciones`:** tablas `evaluaciones` y `calificaciones_criterio` (`timestamptz`, tipos `numeric` exactos, índice único 1:0..1 en `asignacion_evaluacion_id`, índice único `(evaluacion_id, criterio_evaluacion_id)`, restricciones `CHECK` y `FK` con `ON DELETE RESTRICT`/`CASCADE`), traducción en `TraductorExcepcionesPersistencia` y migración nueva en tarea separada antes de pruebas con PostgreSQL real (Testcontainers).
- **API REST v1:** nuevo controlador `api/v1/evaluaciones` con `ProblemDetails`, políticas de autorización específicas y documentación Swagger.

**Fuera de alcance:**

- **HU 8.2 / HUs posteriores de RF-08:** visibilidad cruzada entre coevaluadores tras el cierre formal del periodo de revisión de la convocatoria y validación del estado de cierre de la ventana de revisión contra `Convocatoria` (cuando exista en `MS-PRG`/`MS-POS`). `RF-08` permanece en `[ ]` en los roadmaps al cerrar esta HU.
- **RF-09** (consolidación de promedio entre evaluadores, detección de discrepancias y `ResultadoConsolidadoRanking`) y **RF-10** (resolución de empates y `EntradaListaEspera`).
- **RF-39** (declaración de `ConflictoInteres`, transición `REASIGNADA_POR_CONFLICTO` e invalidación `INVALIDADA_POR_CONFLICTO` de evaluaciones ya enviadas; se documenta como contrato de integración en `design.md`).
- Validación de existencia y estado `EN_EVALUACION` de la entidad `Postulacion` (no existe aún en `ms-postulaciones`; `postulacionId` sigue siendo opaco, coherente con HU 6.2 y HU 6.3).
- Hangfire (no exigido por `docs/` para RF-08), MediatR, Outbox, eventos de integración, notificaciones (RF-31 / `MS-PLT`), y cualquier cambio sobre `Postulantes`, `ComitesEvaluadoresController`, `IComiteEvaluadorRepository`, `Rubricas` u otros microservicios.

## Capabilities

### New Capabilities

- `ms-pos/evaluaciones`: registro de puntuaciones por criterio y comentarios de evaluadores en borrador y envío definitivo sobre postulaciones asignadas, cálculo determinista de puntaje ponderado, inmutabilidad tras envío, bloqueo de rúbrica por primera calificación, transiciones de asignación y consultas bajo evaluación ciega.

### Modified Capabilities

<!-- Ninguna: los requisitos de `ms-pos/rubricas` (incluido el bloqueo por `PRIMERA_CALIFICACION` de HU 7.2) y de `ms-pos/comites` (conformación, asignación y excepción de evaluador único de RF-06) no cambian. -->

## Impact

- **Especificaciones OpenSpec:** nueva capacidad `openspec/changes/rf8-1-int2-mspos/specs/ms-pos/evaluaciones/spec.md`; al archivar se sincroniza en `openspec/specs/ms-pos/evaluaciones/spec.md`.
- **Dominio (`Ascenso.Postulaciones.Domain`):** `Evaluaciones/Agregados/Evaluacion`, `Evaluaciones/Entidades/CalificacionCriterio`, `Evaluaciones/ObjetosValor/DatosCalificacionCriterio`, `Evaluaciones/Servicios/ServicioCalculoPuntajeEvaluacion`, `Enums/EstadoEvaluacion`, nuevas excepciones en `Excepciones/`, métodos de transición aditivos en `Asignaciones/Agregados/AsignacionEvaluacion`, y puertos pequeños y específicos (`IEvaluacionRepository`, `IConsultaAsignacionParaEvaluacionRepository`, `IConsultaRubricaParaEvaluacionRepository`) en `Interfaces/`. Sin cambios en `Domain/Comun`, `IComiteEvaluadorRepository`, `IRubricaRepository` ni `IAsignacionEvaluacionRepository`.
- **Aplicación (`Ascenso.Postulaciones.Application`):** `Evaluaciones/Comandos/` (`GuardarBorradorEvaluacion`, `EnviarEvaluacionDefinitiva`), `Evaluaciones/Consultas/` (`ObtenerMiEvaluacionPorAsignacion`, `ObtenerMisEvaluaciones`, `ObtenerEvaluacionesPostulacion`), `Evaluaciones/DTOs/`, `Evaluaciones/Interfaces/IBloqueadorRubricaEvaluacion`, `Evaluaciones/Servicios/BloqueadorRubricaEvaluacionAdapter` y registro `AddScoped` en `Application/DependencyInjection.cs` sin MediatR.
- **Infraestructura (`Ascenso.Postulaciones.Infrastructure`):** `EvaluacionConfiguracion`, `CalificacionCriterioConfiguracion`, `EvaluacionRepository`, `ConsultaAsignacionParaEvaluacionRepository`, `ConsultaRubricaParaEvaluacionRepository`, nuevos `DbSet` en `PostulacionesDbContext`, constante y traducción en `TraductorExcepcionesPersistencia`, registro en `Infrastructure/DependencyInjection.cs` y migración nueva `EvaluacionesCriterios`.
- **API (`Ascenso.Postulaciones.Api`):** `EvaluacionesController`, `PoliticasAutorizacionEvaluaciones`, ampliación mínima y aditiva de `ManejadorResultadoAutorizacionRubricas` y registro de políticas en `Program.cs`. Sin cambios en los controladores existentes (`Postulantes`, `Rubricas`, `ComitesEvaluadores`, `AsignacionesEvaluacion`).
- **Pruebas:** nuevas suites en `Domain.Tests`, `Application.Tests` e `IntegrationTests` (modelo EF, migración, controladores, autorización, evaluación ciega y PostgreSQL real con Testcontainers); `ArchitectureTests` y suites previas intactas.
- **Roadmaps:** agregar y marcar únicamente `HU 8.1` anidada bajo `RF-08` (con fecha `2026-11-25`) en `openspec/roadmaps/por_integrante.md` y `openspec/roadmaps/por_microservicio.md`, manteniendo `RF-08` en `[ ]`.
