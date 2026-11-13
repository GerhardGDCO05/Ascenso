## Why

Tras la reorganización general, el pipeline y el renombrado del cambio activo, era necesario revisar el estado real del repositorio y de los roadmaps para evitar marcar como completados RFs que aún no tenían funcionalidad de negocio de punta a punta. El shared kernel era una base técnica, no un RF de negocio.

## What Changes

- Revisar la estructura general, la documentación centralizada y las referencias rotas del repositorio.
- Verificar el avance del cambio `bootstrap-ms-beca`: tareas completadas y features implementadas.
- Revisar los roadmaps `por_integrante.md` y `por_microservicio.md` frente a los RF de `MS-BEC`.
- Confirmar que ningún RF de `MS-BEC` se marca hasta estar funcionalmente completo.
- Documentar los pendientes inmediatos de las features 3 y 4 en adelante.

**Fuera de alcance:** implementar nuevas funcionalidades o modificar los roadmaps para reflejar avances inexistentes.

## Capabilities

### New Capabilities

- Ninguna. Es una revisión de estado sin cambio de comportamiento.

### Modified Capabilities

- Ninguna.

## Impact

- **Documentación:** revisión de `docs/organizacion_repositorio.md`, specs y roadmaps.
- **Roadmaps:** sin cambios; todos los RF permanecen pendientes.
- **Validación:** build sin advertencias, 59/59 pruebas, formato correcto, `actionlint` sin observaciones y OpenSpec válido.
