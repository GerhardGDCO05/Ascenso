## Why

El archivo `tasks.md` del cambio `bootstrap-ms-beca` contenía tareas cuya verificación no había sido confrontada con el estado real del repositorio. Era necesario marcar solo las tareas cuyo entregable y criterio de verificación estuvieran presentes y ejecutados, evitando reflejar avances inexistentes.

## What Changes

- Analizar `openspec/changes/bootstrap-ms-beca/tasks.md` con el criterio de entregable verificado.
- Marcar `12.10` (validaciones locales de calidad) tras ejecutar build, pruebas y formato.
- Marcar `14.4` (validación de OpenSpec) tras ejecutar `openspec validate bootstrap-ms-beca --strict`.
- Revisar y dejar sin marcar las tareas cuyos entregables aún faltaban.
- Confirmar que ningún RF de `MS-BEC` se marca en los roadmaps.

**Fuera de alcance:** implementar código nuevo o cambiar el alcance del cambio activo.

## Capabilities

### New Capabilities

- Ninguna. Es una verificación de tareas y calidad.

### Modified Capabilities

- Ninguna.

## Impact

- **Documentación:** `openspec/changes/bootstrap-ms-beca/tasks.md` pasa de 9 a 11 tareas completadas de 114.
- **Roadmaps:** sin cambios; todos los RF siguen pendientes.
- **Validación:** build sin advertencias, 59/59 pruebas, formato correcto y OpenSpec válido.
