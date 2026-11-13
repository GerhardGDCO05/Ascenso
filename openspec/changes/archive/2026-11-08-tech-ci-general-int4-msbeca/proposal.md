## Why

El workflow de CI/CD estaba atado explícitamente a `Apps/services/ms-becas/Ascenso.Becas.sln`. Cada nuevo microservicio obligaba a editar el pipeline, lo que impedía que el equipo integrara soluciones de otros contextos sin intervención manual y aumentaba el riesgo de dejar pruebas sin ejecutar.

## What Changes

- Generalizar `.github/workflows/ci-cd.yml` para descubrir automáticamente todas las soluciones `.sln` y `.slnx` bajo `Apps/services/`.
- Agregar un job `discover` que publique una matriz JSON de soluciones y excluya `bin/`, `obj/` y `node_modules/`.
- Ejecutar `build-and-test` una vez por solución, con `fail-fast: false`, restore, build y pruebas.
- Publicar un artefacto `.trx` por solución con nombre único.
- Actualizar `docs/organizacion_repositorio.md`, `README.md` y `openspec/config.yaml` para describir el pipeline general.

**Fuera de alcance:** cambios en el código de los microservicios o en sus soluciones.

## Capabilities

### New Capabilities

- Ninguna. Es una mejora de infraestructura de integración continua.

### Modified Capabilities

- Ninguna.

## Impact

- **CI/CD:** `.github/workflows/ci-cd.yml` pasa de una solución fija a una matriz de soluciones descubiertas.
- **Documentación:** `docs/organizacion_repositorio.md`, `README.md` y `openspec/config.yaml`.
- **Validación:** descubrimiento con una solución, simulación con dos soluciones y exclusión de `bin/`, `actionlint` sin observaciones y 59/59 pruebas correctas.
