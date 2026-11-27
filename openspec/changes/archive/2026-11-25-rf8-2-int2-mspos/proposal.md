# Proposal

## Why

La historia HU 8.1 (`rf8-1-int2-mspos`) habilitó en `ms-pos` el guardado de borradores, el envío definitivo con cálculo de puntaje ponderado, el bloqueo automático de la rúbrica (`RN-POS-05`) y el aislamiento estricto de evaluación ciega por asignación (`RN-POS-04`, `RN-PLT-02`), validando únicamente el plazo individual `fechaLimiteEvaluacion` de cada `AsignacionEvaluacion` porque el contrato interno de convocatorias aún no estaba integrado. Tras la incorporación de HU 2.1 (`rf2-1-int1-msprg`), `MS-PRG` ya expone `GET /internal/v1/convocatorias/{convocatoriaId}` con el estado y las fechas de la ventana de evaluación (`fechaInicioEvaluacion` y `fechaCierreEvaluacion`). Es necesario completar **RF-08** mediante **HU 8.2** para: (1) impedir en servidor el registro de borradores y el envío de evaluaciones fuera de la ventana formal de revisión de la convocatoria (`RN-POS-04`), y (2) habilitar la visibilidad cruzada en solo lectura de las evaluaciones definitivas (`ENVIADA_DEFINITIVA`) entre coevaluadores asignados a una misma postulación únicamente después del cierre formal del periodo de revisión, cerrando `RF-08` sin comprometer la evaluación ciega ni la confidencialidad (`RN-PLT-02`, RF-37).

## What Changes

- **Validación de la ventana de revisión de la convocatoria en escritura (HU 8.2):**
  - `GuardarBorradorEvaluacionCommandHandler` y `EnviarEvaluacionDefinitivaCommandHandler` consultan el estado y las fechas de evaluación de la convocatoria (`ConvocatoriaId` de la asignación) a través de un puerto desacoplado (ACL) hacia `MS-PRG` (`GET /internal/v1/convocatorias/{convocatoriaId}`), de forma **fail-closed** (`503 DEPENDENCIA_NO_DISPONIBLE` si `MS-PRG` no responde o devuelve una respuesta inválida; `404 CONVOCATORIA_NO_ENCONTRADA` si no existe).
  - Se rechaza con `409 Conflict` cualquier intento de guardar borrador o enviar evaluación definitiva antes de iniciar la ventana (`PERIODO_REVISION_NO_INICIADO`), tras el cierre formal del periodo de revisión (`PERIODO_REVISION_CERRADO`) o sobre una convocatoria cancelada (`CONVOCATORIA_CANCELADA`), manteniendo intacta la validación del plazo individual `asignacion.FechaLimiteEvaluacion` (`PLAZO_EVALUACION_VENCIDO`) y todas las invariantes de HU 8.1.
- **Visibilidad cruzada en solo lectura entre coevaluadores tras el cierre formal del periodo de revisión (HU 8.2):**
  - Nuevo caso de uso CQRS de lectura para el rol `MIEMBRO_COMITE`: consulta de evaluaciones definitivas de los coevaluadores sobre una `postulacionId` (`GET /api/v1/evaluaciones/postulaciones/{postulacionId}/coevaluadores`).
  - Exige que el `MIEMBRO_COMITE` autenticado (claim `sub`) posea una `AsignacionEvaluacion` vigente (distinta de `REASIGNADA_POR_CONFLICTO`) sobre esa `postulacionId`; en caso contrario responde `403 Forbidden` con `codigo = EVALUACION_CONFIDENCIAL` sin consultar `MS-PRG` ni filtrar información de la postulación.
  - Exige que el periodo de revisión de la convocatoria asociada haya cerrado formalmente (`EstadoConvocatoria` en `PUBLICADA` o `CERRADA` y `ahora > fechaCierreEvaluacion`); mientras el periodo siga abierto o no haya iniciado responde `409 Conflict` con `codigo = PERIODO_REVISION_ABIERTO`, y si la convocatoria fue cancelada responde `409 Conflict` con `codigo = CONVOCATORIA_CANCELADA`.
  - Expone exclusivamente evaluaciones de los demás evaluadores (`evaluadorId != usuarioActual`) en estado `ENVIADA_DEFINITIVA` —nunca `BORRADOR` ni `INVALIDADA_POR_CONFLICTO`—, omitiendo datos personales del postulante y preservando el anonimato del identificador personal y de asignación de los pares (`EvaluacionCoevaluadorDto`).
- **Seguridad y autorización:**
  - El nuevo endpoint reutiliza la política `GestionMisEvaluaciones` (`MIEMBRO_COMITE`), garantizando `403 PERMISO_DENEGADO` en `ProblemDetails` para `ADMIN_FINANCIERO`, `COORDINADOR_PROGRAMA` y cualquier otro rol no autorizado, y `401 Unauthorized` sin token.
- **Sin cambios de esquema en PostgreSQL:**
  - Los índices existentes (`ux_asignaciones_postulacion_evaluador_vigente` e `ix_evaluaciones_postulacion_estado`) cubren de forma óptima las nuevas consultas de lectura; no se requiere nueva migración EF Core.

**Fuera de alcance:**

- **RF-09** (`ResultadoConsolidadoRanking`, consolidación de promedio entre evaluadores, detección de discrepancias y ordenamiento de ranking).
- **RF-10** (resolución de empates y `EntradaListaEspera`).
- **RF-39** (declaración de `ConflictoInteres`, `ReasignarPorConflicto` e `InvalidarPorConflicto`).
- Entidad `Postulacion` (Integrante 1; `postulacionId` sigue siendo un UUID opaco en `Evaluaciones`).
- Cualquier modificación a `MS-PRG` u otros microservicios, MediatR, Outbox, Hangfire, y cambios sobre `Postulantes`, `Rubricas`, `Comites`, `AsignacionesEvaluacionController`, `Domain/Comun` o los contratos de puertos previos (`IRubricaRepository`, `IComiteEvaluadorRepository`, `IAsignacionEvaluacionRepository`, `IEvaluacionRepository`, `IConsultaAsignacionParaEvaluacionRepository`, `IConsultaRubricaParaEvaluacionRepository`).

## Capabilities

### New Capabilities

<!-- Ninguna: HU 8.2 extiende la capacidad existente `ms-pos/evaluaciones` para completar RF-08. -->

### Modified Capabilities

- `ms-pos/evaluaciones`: incorpora la validación fail-closed de la ventana de revisión de la convocatoria (`MS-PRG`) al guardar borradores y enviar evaluaciones definitivas, y agrega la consulta de visibilidad cruzada en solo lectura entre coevaluadores únicamente tras el cierre formal del periodo de revisión, respetando `RN-POS-04`, `RN-PLT-02` y RF-37.

## Impact

- **Especificaciones OpenSpec:** delta en `openspec/changes/rf8-2-int2-mspos/specs/ms-pos/evaluaciones/spec.md`; al archivar se sincroniza con `openspec/specs/ms-pos/evaluaciones/spec.md`.
- **Dominio (`Ascenso.Postulaciones.Domain`):** nuevos enums `EstadoConvocatoriaExterna` y `EstadoPeriodoRevision` (`Enums/`), objeto de valor inmutable `VentanaRevisionConvocatoria` (`Evaluaciones/ObjetosValor/`), excepciones de dominio (`ConvocatoriaNoEncontradaException`, `PeriodoRevisionNoIniciadoException`, `PeriodoRevisionCerradoException`, `PeriodoRevisionAbiertoException`, `ConvocatoriaCanceladaException` en `Excepciones/`), y puertos ISP puros (`IConvocatoriaEvaluacionClient`, `IConsultaEvaluacionesCoevaluadoresRepository` en `Interfaces/`). Sin cambios en `Domain/Comun`, en `Evaluacion`, en `AsignacionEvaluacion` ni en puertos previos.
- **Aplicación (`Ascenso.Postulaciones.Application`):** integración de `IConvocatoriaEvaluacionClient` en `GuardarBorradorEvaluacionCommandHandler` y `EnviarEvaluacionDefinitivaCommandHandler`; nueva consulta CQRS `ObtenerEvaluacionesCoevaluadoresPostulacion` (query, validator, handler), DTOs (`EvaluacionCoevaluadorDto`, `EvaluacionesCoevaluadoresPostulacionDto`), extensión aditiva de `EvaluacionMapeador` y registro `AddScoped` en `Application/DependencyInjection.cs`.
- **Infraestructura (`Ascenso.Postulaciones.Infrastructure`):** adaptador HTTP fail-closed `ConvocatoriaEvaluacionClient` (`Adapters/`, consumiendo `ServiciosExternos:Programas:UrlBase` y `TimeoutSegundos`), repositorio de lectura `ConsultaEvaluacionesCoevaluadoresRepository` (`Persistence/Repositorios/`) y registro en `Infrastructure/DependencyInjection.cs`. Sin cambios de esquema ni nueva migración EF Core.
- **API (`Ascenso.Postulaciones.Api`):** nuevo endpoint `GET /api/v1/evaluaciones/postulaciones/{postulacionId:guid}/coevaluadores` en `EvaluacionesController` (reutilizando `PoliticasAutorizacionEvaluaciones.GestionMisEvaluaciones`) y anotación `503` en Swagger para escritura y consulta dependientes de `MS-PRG`. Sin cambios en `Program.cs` ni en `ManejadorResultadoAutorizacionRubricas`.
- **Pruebas:** nuevas pruebas unitarias y de integración (`Domain.Tests`, `Application.Tests`, `IntegrationTests` incluyendo pruebas HTTP del adaptador y PostgreSQL real con Testcontainers); ajuste mínimo y documentado en constructores/fábricas de pruebas de HU 8.1 por la nueva dependencia `IConvocatoriaEvaluacionClient` (y `IConsultaEvaluacionesCoevaluadoresRepository` en `WebApplicationFactory`), manteniendo intactas las aserciones y `ArchitectureTests`.
- **Roadmaps:** al concluir la implementación, agregar y marcar `HU 8.2` anidada bajo `RF-08` (con fecha `2026-11-25`) y marcar `RF-08` en `[x]` en `openspec/roadmaps/por_integrante.md` y `openspec/roadmaps/por_microservicio.md`, confirmando el cierre total de `RF-08`.
