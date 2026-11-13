## Why

El equipo incorporó actualizaciones desde GitHub que centralizaron la documentación en `docs/`, cambiaron la configuración de OpenSpec y agregaron especificaciones de otros microservicios. Era necesario revisar esas actualizaciones, corregir el pipeline para que las pruebas fueran deterministas, eliminar documentación duplicada y reorganizar el cambio activo de `ms-beca`.

## What Changes

- Analizar las actualizaciones del equipo en `docs/`, `openspec/config.yaml`, `openspec/roadmaps/` y las especificaciones de `ms-pos` y `ms-plt`.
- Corregir `.github/workflows/ci-cd.yml` para usar el SDK fijado y apuntar explícitamente a `Ascenso.Becas.sln`.
- Eliminar de `openspec/specs/` las copias duplicadas de la explicación del DDD y de las convenciones.
- Crear la especificación duradera `openspec/specs/ms-beca/spec_beca.md`.
- Renombrar el cambio activo `bootstrap-ms-bec` a `bootstrap-ms-beca` y su carpeta de specs de `ms-bec` a `ms-beca`.
- Actualizar las referencias afectadas en `proposal.md`, `design.md`, `tasks.md` y el histórico del 2026-11-07.

**Fuera de alcance:** cambios en el código de negocio, en `ms-pos` o en `ms-plt`.

## Capabilities

### New Capabilities

- `ms-beca`: especificación duradera del otorgamiento, ciclo de vida, desembolsos y seguimiento académico de la beca.

### Modified Capabilities

- Ninguna.

## Impact

- **Documentación:** `openspec/specs/ms-beca/spec_beca.md`, `docs/` y `openspec/config.yaml`.
- **CI/CD:** `.github/workflows/ci-cd.yml` con ejecución determinista sobre la solución de `ms-becas`.
- **Cambio activo:** renombrado a `bootstrap-ms-beca`; el histórico del 2026-11-07 conserva una nota del renombrado.
- **Validación:** restore, build, 59/59 pruebas y `openspec validate bootstrap-ms-beca --strict` correctos.
