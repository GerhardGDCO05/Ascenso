## Why

El Coordinador de programa necesita un instrumento de medición cuantitativo para evaluar postulaciones. La HU 7.1 (RF-07) introduce en `MS-POS` la `RubricaEvaluacion`: un conjunto de `CriterioEvaluacion` con peso porcentual y rango de calificación, que debe sumar exactamente `100.00%` antes de publicarse y asociarse al ciclo de postulación del programa. Sin rúbrica publicada no es posible la evaluación de mérito (RF-08).

## What Changes

**Alcance: SOLO la HU 7.1 (RF-07).** Quedan fuera la HU 7.2 (bloqueo automático de la rúbrica) y la HU 7.3 (clonación de rúbricas).

- **Nuevo agregado `RubricaEvaluacion`** (`programaBecaId`, `convocatoriaId`, `nombre`, `estado`, `fechaPublicacion`) con entidad hija `CriterioEvaluacion` (`nombre`, `descripcion`, `pesoPorcentual`, `puntajeMinimo`, `puntajeMaximo`, `orden`), siguiendo el patrón `Postulante` → `MiembroFamiliar`.
- **Unicidad por programa y convocatoria (decisión a validar con el equipo):** solo puede existir una rúbrica por combinación de `programaBecaId` y `convocatoriaId`; un intento duplicado responde `409 RUBRICA_DUPLICADA`.
- **Nuevo enum `EstadoRubrica`** (`BORRADOR`, `PUBLICADA`, `BLOQUEADA`). Este cambio solo transiciona entre `BORRADOR` y `PUBLICADA`; `BLOQUEADA` se declara para el modelo y para rechazar mutaciones, pero ningún flujo de esta HU la asigna.
- **Gestión de criterios:** agregar, editar y eliminar criterios mientras la rúbrica está en `BORRADOR`. Peso en `(0, 100]` con máximo 2 decimales; `puntajeMinimo < puntajeMaximo`; nombre (máximo 200 caracteres) y descripción (máximo 1000 caracteres) obligatorios.
- **Suma de pesos calculada por el sistema** y expuesta en la consulta junto con la diferencia respecto de `100.00`.
- **Publicación:** solo si la suma es exactamente `100.00`; de lo contrario `422 PESOS_INVALIDOS` indicando la diferencia. Publicar pasa a `PUBLICADA` y registra la fecha.
- **Inmutabilidad:** editar, agregar, eliminar criterios o publicar una rúbrica `PUBLICADA` o `BLOQUEADA` responde `409 RUBRICA_BLOQUEADA`.
- **Autorización (RN-PLT-02):** solo el rol `COORDINADOR_PROGRAMA` (claim `roles` del JWT) puede operar. `ADMIN_FINANCIERO` u otro rol recibe `403` con `ProblemDetails` y `codigo = PERMISO_DENEGADO`, sin alterar el comportamiento actual de Postulantes.
- **API v1:** `POST /api/v1/rubricas`, `GET /api/v1/rubricas/{id}`, `POST /api/v1/rubricas/{id}/criterios`, `PUT /api/v1/rubricas/{id}/criterios/{criterioId}`, `DELETE /api/v1/rubricas/{id}/criterios/{criterioId}`, `POST /api/v1/rubricas/{id}/publicar`.

**Fuera de alcance:**

- HU 7.2 (bloqueo de la rúbrica al iniciar evaluación o guardar el primer puntaje) y HU 7.3 (clonación, `rubricaOrigenClonadaId`).
- Campos `evaluadoresMinimosRequeridos`, `umbralDiscrepanciaPuntos` y `motivoBloqueo`/`fechaBloqueo` del modelo maestro.
- Validación de `programaBecaId` contra `MS-PRG`, de `convocatoriaId` (RF-02 aún no existe) y alcance por programa del coordinador (depende de `MS-PLT`); quedan como preguntas abiertas en `design.md`.
- Hangfire, Outbox, MediatR y eventos de integración.
- Frontend, módulo Postulantes y otros microservicios.

## Capabilities

### New Capabilities

- `ms-pos/rubricas`: definición, edición y publicación de la rúbrica de evaluación de un programa con criterios ponderados al 100.00%.

### Modified Capabilities

<!-- Ninguna: no se modifica la capacidad `ms-pos/postulantes` ni `spec_pos.md`. -->

## Impact

- **Especificaciones OpenSpec:** delta en `openspec/changes/rf7-1-int2-mspos/specs/ms-pos/rubricas/spec.md`; al archivar se sincroniza con `openspec/specs/ms-pos/rubricas/spec.md`. No se modifica `spec_pos.md`.
- **Dominio (`Ascenso.Postulaciones.Domain`):** `Rubricas/Agregados/RubricaEvaluacion`, `Rubricas/Entidades/CriterioEvaluacion`, enum `EstadoRubrica` (con `CodigoEnum`), excepciones de dominio (`PESOS_INVALIDOS`, `RUBRICA_BLOQUEADA`, rúbrica/criterio inválido o no encontrado, rúbrica duplicada), puerto `IRubricaRepository`.
- **Aplicación (`Ascenso.Postulaciones.Application`):** `Rubricas/Comandos` (crear rúbrica, agregar/editar/eliminar criterio, publicar), `Rubricas/Consultas` (obtener rúbrica), `Rubricas/DTOs`; validadores FluentValidation; registro de handlers en DI.
- **Infraestructura (`Ascenso.Postulaciones.Infrastructure`):** configuración EF de `rubricas` y `criterios_evaluacion` en esquema `postulaciones`, `CHECK` de peso y rango de puntaje, índices únicos, `RubricaRepository`, `DbSet` y migración (tarea separada al final de persistencia).
- **API (`Ascenso.Postulaciones.Api`):** `RubricasController` v1 con política de autorización por rol `COORDINADOR_PROGRAMA` y respuesta `403` `PERMISO_DENEGADO` acotada a los endpoints de rúbricas.
- **Pruebas:** `Domain.Tests`, `Application.Tests`, `IntegrationTests` (endpoints con esquema de autenticación de prueba, modelo de persistencia y pruebas de integración contra PostgreSQL real con Testcontainers) y `ArchitectureTests` sin cambios y en verde.
- **Dependencias entre servicios:** ninguna nueva. No se modifica `MS-PRG`, `MS-PLT` ni `MS-BEC`.
- **Roadmaps:** al completar la implementación se marcará la HU 7.1 sin marcar `RF-07` como completo.
