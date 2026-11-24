# Proposal

## Why

La HU 6.1 ya permite conformar el `ComiteEvaluador` con sus `MiembroComite`, pero ninguna postulación puede llegar todavía a un evaluador. RF-06 exige que «cada postulación reciba al menos dos evaluaciones independientes» (RN-POS-04) y el DDD V7 (§2.3.B) introduce `AsignacionEvaluacion` para separar la responsabilidad administrativa de **asignar y balancear la carga** de la de calificar (`Evaluacion`, RF-08). Sin esta HU, RF-08 (puntuación), RF-39 (conflictos de interés) y RF-09 (consolidación) no tienen sobre qué operar.

## What Changes

- Nuevo agregado `AsignacionEvaluacion` (DDD V7: `comiteEvaluadorId`, `convocatoriaId`, `postulacionId`, `miembroComiteId`, `usuarioEvaluadorId`, `fechaAsignacion`, `fechaLimiteEvaluacion`, `estado`) y enum `EstadoAsignacionEvaluacion` (`PENDIENTE`, `EN_PROGRESO`, `COMPLETADA`, `REASIGNADA_POR_CONFLICTO`). En HU 6.2 solo se crea en `PENDIENTE`; las demás transiciones pertenecen a RF-08 y RF-39.
- Asignación atómica de una postulación a evaluadores **activos del comité**: el coordinador elige evaluadores o delega la selección balanceada (menor carga vigente). El resultado de cada operación deja a la postulación con **al menos 2 evaluadores vigentes distintos** (RN-POS-04); no se admite el mismo evaluador vigente dos veces para una postulación, ni siquiera con solicitudes simultáneas.
- Consultas CQRS: asignaciones de una postulación y carga por evaluador de un comité (`COORDINADOR_PROGRAMA`), y «mis asignaciones» paginadas para el evaluador (`MIEMBRO_COMITE`, evaluación ciega: sin identidad del postulante ni de los coevaluadores).
- API REST v1 `api/v1/asignaciones-evaluacion` con `ProblemDetails`; `ADMIN_FINANCIERO` siempre `403 PERMISO_DENEGADO`.
- Persistencia: tabla `postulaciones.asignaciones_evaluacion` (`timestamptz`, índice único parcial de evaluador vigente por postulación, `CHECK`) y migración nueva en tarea separada antes de PostgreSQL real.

**Fuera de alcance:**

- HU 6.3 (excepción de evaluador único con justificación): HU 6.2 nunca deja una postulación con menos de 2 vigentes; la excepción se documenta como contrato de integración.
- RF-39 (`ConflictoInteres`, reasignación automática y estado `REASIGNADA_POR_CONFLICTO` como transición) y RF-08 (`Evaluacion`, puntuación, transiciones `EN_PROGRESO` y `COMPLETADA`).
- Verificar que la postulación exista y esté en `EN_EVALUACION`: `Postulacion` **no existe** aún en `ms-postulaciones` (solo `Postulante`); se referencia por `postulacionId` sin FK (ver `design.md`).
- Notificación al evaluador (RF-31 / `MS-PLT`), Hangfire, Outbox, MediatR, eventos de integración, módulos `Postulantes` y `Rubricas`, otros microservicios y cambios en la HU 6.1 (p. ej. bloquear la desvinculación con asignaciones vigentes: pregunta abierta).

## Capabilities

### New Capabilities

<!-- Ninguna: el comportamiento se agrega a la capacidad existente `ms-pos/comites`, que ya agrupa el dominio de RF-06. -->

### Modified Capabilities

- `ms-pos/comites`: se agregan requisitos de asignación de postulaciones a evaluadores (mínimo 2 vigentes, evaluador activo del comité, sin duplicados vigentes, balanceo de carga), consultas de asignaciones, evaluación ciega en «mis asignaciones» y autorización de los nuevos endpoints. Los requisitos de la HU 6.1 no cambian.

## Impact

- **Especificaciones OpenSpec:** delta `openspec/changes/rf6-2-int2-mspos/specs/ms-pos/comites/spec.md` (solo `ADDED`); al archivar se sincroniza con `openspec/specs/ms-pos/comites/spec.md`.
- **Dominio:** `Asignaciones/Agregados/AsignacionEvaluacion`, servicio de dominio de asignación y balanceo, `Enums/EstadoAsignacionEvaluacion`, excepciones de asignación, puerto `IAsignacionEvaluacionRepository`. Sin cambios en `Domain/Comun`.
- **Aplicación:** `Asignaciones/Comandos`, `Asignaciones/Consultas`, `Asignaciones/DTOs`, validadores FluentValidation, registro `AddScoped` en `Application/DependencyInjection.cs`.
- **Infraestructura:** `AsignacionEvaluacionConfiguracion`, `AsignacionEvaluacionRepository`, `DbSet` en `PostulacionesDbContext`, `TraductorExcepcionesPersistencia` (índice único nuevo), `Infrastructure/DependencyInjection.cs`, migración nueva `AsignacionesEvaluacion`.
- **API:** `AsignacionesEvaluacionController`, `PoliticasAutorizacionAsignaciones` y ampliación mínima y aditiva de `ManejadorResultadoAutorizacionRubricas` (ya ampliado en HU 6.1) para `PERMISO_DENEGADO`; registro de políticas en `Program.cs`. Sin cambios en los controladores de `Comites`, `Postulantes` ni `Rubricas`.
- **Pruebas:** `Domain.Tests`, `Application.Tests`, `IntegrationTests` (modelo EF, endpoints, seguridad, Testcontainers); las pruebas de HU 6.1 no se modifican.
- **Roadmaps:** agregar y marcar `HU 6.2` anidada bajo `RF-06` (2026-11-24); `RF-06` permanece en `[ ]` hasta completar la HU 6.3.
