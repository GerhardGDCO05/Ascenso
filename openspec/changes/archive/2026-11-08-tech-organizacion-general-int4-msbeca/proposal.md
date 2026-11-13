## Why

La organización del repositorio estaba descrita con foco exclusivo en `ms-beca` y no permitía que los demás equipos reutilizaran la estructura sin mezclar código ni especificaciones. Era necesario documentar una organización general por microservicio, manteniendo `ms-beca` como área de trabajo activa.

## What Changes

- Crear `docs/organizacion_repositorio.md` con la estructura de alto nivel, la tabla de microservicios y las reglas de convivencia.
- Generalizar `docs/convenciones_c_y_ddd.md` para que la estructura hexagonal aplique a cualquier microservicio.
- Agregar `docs/organizacion_repositorio.md` como contexto de OpenSpec y describir la estructura general en `openspec/config.yaml`.
- Documentar en el `README.md` la organización del repositorio y el trabajo activo en `ms-beca`.
- Aclarar que `ms-pos` y `ms-plt` son contexto de otros equipos y no se modifican.

**Fuera de alcance:** cambios de código, creación de nuevos microservicios o modificación de las especificaciones de otros equipos.

## Capabilities

### New Capabilities

- Ninguna. Es documentación organizativa transversal.

### Modified Capabilities

- Ninguna.

## Impact

- **Documentación:** `docs/organizacion_repositorio.md`, `docs/convenciones_c_y_ddd.md`, `README.md` y `openspec/config.yaml`.
- **Código:** sin cambios.
- **Validación:** YAML válido, `openspec validate bootstrap-ms-beca --strict` correcto, 59/59 pruebas y `git diff --check` sin errores.
