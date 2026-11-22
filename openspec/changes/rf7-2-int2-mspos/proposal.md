# Proposal

## Why

La HU 7.1 permite al Coordinador de programa definir y publicar la `RubricaEvaluacion`, pero hoy la rúbrica `PUBLICADA` nunca llega a `BLOQUEADA`: el estado existe solo para rechazar mutaciones. La HU 7.2 (RF-07) cierra el ciclo de vida del instrumento: una vez iniciada la etapa de evaluación (o registrada la primera puntuación) la rúbrica debe quedar inalterable en el servidor, para garantizar transparencia e igualdad de condiciones entre postulantes (`RN-POS-05`). Sin este bloqueo, los criterios podrían cambiar a mitad de la evaluación (RF-08).

## What Changes

**Alcance: SOLO la HU 7.2 (RF-07).** La HU 7.1 ya está implementada y archivada (`openspec/changes/archive/2026-11-21-rf7-1-int2-mspos/`). Queda fuera la HU 7.3 (clonación de rúbricas).

- **Transición terminal `PUBLICADA` → `BLOQUEADA`** en `RubricaEvaluacion`, mediante el método de dominio `Bloquear(motivo, fecha, usuarioId)`: solo desde `PUBLICADA`; desde `BORRADOR` lanza `RubricaNoPublicadaException` (`RUBRICA_NO_PUBLICADA`, `409`); desde `BLOQUEADA` es idempotente (no cambia nada, no lanza).
- **Campos nuevos** en `RubricaEvaluacion`: `motivoBloqueo` (enum nuevo `MotivoBloqueoRubrica`: `INICIO_EVALUACION`, `PRIMERA_CALIFICACION`) y `fechaBloqueo`.
- **Caso de uso `BloquearRubricaCommand` + handler**, idempotente y seguro bajo concurrencia (dos disparadores simultáneos producen un solo cambio efectivo y ambos terminan sin error). Actor: `UsuarioSistema` (identidad fija del sistema, agregada al núcleo `Domain/Comun`).
- **Sin endpoint HTTP de bloqueo.** Los disparadores reales (inicio de la fase de evaluación de la convocatoria, dependiente de RF-02, y primera calificación, HU 8.1) aún no existen: se documentan en `design.md` como contrato de integración y pregunta abierta.
- **Mensaje explicativo (criterio 3):** `RubricaBloqueadaException` distingue una rúbrica «publicada» de una «bloqueada desde {fecha} por {motivo}». Mantiene `409` y `RUBRICA_BLOQUEADA`.
- **Consulta en solo lectura (criterio 4):** `RubricaDto` agrega `motivoBloqueo`, `fechaBloqueo` y `soloLectura` (`true` cuando `estado != BORRADOR`).
- **Autorización (`MODIFIED`):** `LecturaRubrica` permite `COORDINADOR_PROGRAMA` y `MIEMBRO_COMITE` (solo lectura); la escritura sigue solo con `COORDINADOR_PROGRAMA`; `ADMIN_FINANCIERO` sigue en `403`.
- **Persistencia:** columnas `motivo_bloqueo` y `fecha_bloqueo`, `CHECK` de coherencia con el estado y migración nueva en tarea separada.
- Criterio 2 (UI deshabilita la edición) es frontend y queda fuera de alcance; el DTO expone `soloLectura` para habilitarlo.

**Fuera de alcance:**

- HU 7.3 y cualquier endpoint HTTP de bloqueo; implementación de los disparadores (RF-02, HU 8.1).
- Alcance de lectura por comité (RF-06) y por programa (MS-PLT): preguntas abiertas.
- Hangfire, Outbox, MediatR, eventos de integración, frontend, módulo Postulantes y otros microservicios.

## Capabilities

### New Capabilities

<!-- Ninguna: el comportamiento pertenece a la capacidad existente `ms-pos/rubricas`. -->

### Modified Capabilities

- `ms-pos/rubricas`: se agregan los requisitos de bloqueo, idempotencia, concurrencia, mensaje explicativo y consulta en solo lectura; se modifica el requisito de autorización (lectura para `MIEMBRO_COMITE`).

## Impact

- **Especificaciones OpenSpec:** delta en `openspec/changes/rf7-2-int2-mspos/specs/ms-pos/rubricas/spec.md` (`ADDED` y `MODIFIED`); al archivar se sincroniza con `openspec/specs/ms-pos/rubricas/spec.md`.
- **Dominio:** `Enums/MotivoBloqueoRubrica`, `Comun/UsuarioSistema` (núcleo compartido, mínimo), `RubricaNoPublicadaException`, cambios en `RubricaBloqueadaException` y `RubricaEvaluacion` (`Bloquear`, `MotivoBloqueo`, `FechaBloqueo`).
- **Aplicación:** `Rubricas/Comandos/BloquearRubrica`, `RubricaDto` y `RubricaMapeador`, registro del handler en DI.
- **Infraestructura:** `RubricaEvaluacionConfiguracion` (columnas y `CHECK`), migración nueva (tarea separada antes de las pruebas con PostgreSQL real).
- **API:** `PoliticasAutorizacionRubricas` y `Program.cs` (política `LecturaRubrica` con `MIEMBRO_COMITE`); sin cambios de rutas.
- **Pruebas:** `Domain.Tests`, `Application.Tests` e `IntegrationTests` (modelo EF, endpoints, Testcontainers); las pruebas de 7.1 que forzaban `BLOQUEADA` por reflexión pasan a usar `Bloquear`.
- **Roadmaps:** marcar solo `HU 7.2` bajo `RF-07`, que permanece en `[ ]` hasta completar la HU 7.3.
