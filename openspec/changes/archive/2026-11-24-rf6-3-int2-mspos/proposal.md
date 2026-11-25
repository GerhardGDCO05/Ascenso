# Proposal

## Why

La HU 6.2 garantiza que cada postulación tenga al menos 2 evaluadores vigentes distintos (RN-POS-04), sin ninguna salida. El PRD (RF-06) indica que «cada postulación debe recibir al menos dos evaluaciones independientes, **salvo que el coordinador autorice una excepción**», y el DDD V7 define que `RN-POS-04` se relaja cuando «un Coordinador sin conflicto registra una `excepcionEvaluadorUnico` con justificación obligatoria». Hoy esa excepción no puede registrarse: el DDD la ubica en `ResultadoConsolidadoRanking` (RF-09), que no existe, y la HU 6.2 dejó solo el punto de extensión `MinimoEvaluadoresVigentes = 2`. Sin la HU 6.3, un comité pequeño (p. ej. un único evaluador externo disponible) bloquea por completo la evaluación de una postulación, y cualquier autorización informal quedaría sin trazabilidad (RF-35 / RN-AUD-01).

## What Changes

- Nueva entidad inmutable `AutorizacionExcepcionEvaluadorUnico` (append-only, `EntidadInmutable`): `postulacionId`, `justificacion` obligatoria, `usuarioAutorizadorId` (coordinador), `fechaAutorizacion` en UTC y `correlacionId`. Es el respaldo local de los campos `excepcionEvaluadorUnico`, `justificacionEvaluadorUnico` y `autorizadorExcepcionId` del DDD hasta que RF-09 cree `ResultadoConsolidadoRanking`. Una sola excepción por postulación; no se edita ni se elimina (índice único, `CHECK` y trigger de inmutabilidad en PostgreSQL).
- `ServicioAsignacionEvaluadores.Asignar` acepta un mínimo efectivo (`1` con excepción autorizada vigente; `2` en caso contrario) mediante un parámetro opcional que conserva el valor por defecto `2`: las pruebas de dominio de HU 6.2 no cambian.
- `AsignarEvaluadoresPostulacionCommandHandler` consulta si la postulación tiene excepción y calcula el mínimo efectivo, permitiendo 1 evaluador en modo manual o automático.
- Casos CQRS nuevos: `AutorizarExcepcionEvaluadorUnicoCommand` (coordinador, con justificación) y `ObtenerExcepcionEvaluadorUnicoQuery`.
- API REST v1 (en `api/v1/asignaciones-evaluacion`): `POST` y `GET` `postulaciones/{postulacionId}/excepcion-evaluador-unico`, solo `COORDINADOR_PROGRAMA`; `MIEMBRO_COMITE` y `ADMIN_FINANCIERO` reciben `403 PERMISO_DENEGADO`.
- Persistencia: tabla `postulaciones.excepciones_evaluador_unico` (`timestamptz`, índice único por postulación, `CHECK`, trigger anti `UPDATE`/`DELETE`) y migración nueva en tarea separada antes de PostgreSQL real.
- Ajustes mínimos y documentados a pruebas de HU 6.2 (no al comportamiento sin excepción): el validador del comando de asignación admite `CantidadEvaluadores >= 1` (el dominio sigue rechazando 1 sin excepción con `EVALUACIONES_INSUFICIENTES`) y el handler recibe un nuevo repositorio por constructor.

**Fuera de alcance:**

- RF-39: verificar de forma real que el coordinador no tenga `ConflictoInteres` con la postulación (RN-POS-06): queda como contrato de integración (`design.md`).
- RF-08 (`Evaluacion`), RF-09 (`ResultadoConsolidadoRanking`, que adoptará estos datos) y la entidad `Postulacion` (Integrante 1): `postulacionId` sigue siendo opaco, sin FK.
- Revocar una excepción, auditoría en `MS-AUD` (RF-35 global), notificaciones, Hangfire (los documentos no exigen segundo plano para HU 6.3), Outbox, MediatR, eventos, módulos `Postulantes` y `Rubricas`, `ComitesEvaluadoresController`, `IComiteEvaluadorRepository`, otros microservicios.

## Capabilities

### New Capabilities

<!-- Ninguna: el comportamiento se agrega a la capacidad existente `ms-pos/comites`, que ya agrupa el dominio de RF-06. -->

### Modified Capabilities

- `ms-pos/comites`: se agregan requisitos de autorización, consulta, trazabilidad y persistencia de la excepción de evaluador único; se modifica el requisito «Asignación de una postulación a evaluadores del comité» para que el mínimo de 2 evaluadores se relaje a 1 solo cuando existe una excepción autorizada para esa postulación.

## Impact

- **Especificaciones OpenSpec:** delta `openspec/changes/rf6-3-int2-mspos/specs/ms-pos/comites/spec.md` (`ADDED` + `MODIFIED`); al archivar se sincroniza con `openspec/specs/ms-pos/comites/spec.md`.
- **Dominio:** `Asignaciones/Entidades/AutorizacionExcepcionEvaluadorUnico`, `Asignaciones/Servicios/ServicioExcepcionEvaluadorUnico`, parámetro de mínimo efectivo en `ServicioAsignacionEvaluadores`, cuatro excepciones nuevas, puerto `IExcepcionEvaluadorUnicoRepository`. Sin cambios en `Domain/Comun`.
- **Aplicación:** `Asignaciones/Comandos/AutorizarExcepcionEvaluadorUnico`, `Asignaciones/Consultas/ObtenerExcepcionEvaluadorUnico`, DTO y mapeador, ajuste del handler y del validador de asignación, registro `AddScoped` en `Application/DependencyInjection.cs`.
- **Infraestructura:** `AutorizacionExcepcionEvaluadorUnicoConfiguracion`, `ExcepcionEvaluadorUnicoRepository`, `DbSet` en `PostulacionesDbContext`, `TraductorExcepcionesPersistencia`, `Infrastructure/DependencyInjection.cs`, migración nueva `ExcepcionEvaluadorUnico`.
- **API:** dos endpoints nuevos en `AsignacionesEvaluacionController` bajo la política existente `GestionAsignacionesEvaluacion`; sin cambios en `Program.cs`, `PoliticasAutorizacionAsignaciones` ni `ManejadorResultadoAutorizacionRubricas`.
- **Pruebas:** `Domain.Tests`, `Application.Tests`, `IntegrationTests` (modelo EF, endpoints, seguridad, Testcontainers).
- **Roadmaps:** agregar y marcar `HU 6.3` anidada bajo `RF-06` (2026-11-24) y marcar `RF-06` en `[x]` porque no quedan HUs pendientes (6.1, 6.2 y 6.3).
