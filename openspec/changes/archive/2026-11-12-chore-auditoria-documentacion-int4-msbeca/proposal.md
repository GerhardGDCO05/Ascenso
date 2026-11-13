## Why

El repositorio Ascenso acumulaba deuda documental y de organización: un borrador en `openspec/specs/`, escapes Markdown en las especificaciones de `ms-plt` y `ms-pos`, restricciones obsoletas sobre otros equipos, ejemplos C# inválidos, históricos archivados sin los artefactos mínimos de OpenSpec y un warning de validación en el cambio activo. Además, las reglas de archivado no estaban blindadas en `openspec/config.yaml`, lo que permitía seguir creando carpetas con hora `HHMM` y sin bitácora.

## What Changes

- Limpiar y corregir la documentación de `docs/` y las especificaciones de `openspec/`.
- Verificar los dos roadmaps contra los 40 RF de la especificación maestra.
- Corregir el warning de requisito largo en `bootstrap-ms-beca` moviendo los casos borde a escenarios.
- Auditar la arquitectura hexagonal, el aislamiento de `Domain` y la nomenclatura del código.
- Normalizar los 11 históricos de `openspec/changes/archive/` al formato `YYYY-MM-DD-[ID-Cambio]-[Integrante]-[Microservicio]`.
- Generar en cada histórico los artefactos mínimos: `.openspec.yaml`, `proposal.md`, `design.md`, `tasks.md` y su bitácora renombrada.
- Añadir a `openspec/config.yaml` las reglas obligatorias de nomenclatura y artefactos para futuros archivados.
- Registrar esta propia sesión como histórico con el formato nuevo.

**Fuera de alcance:** modificar las reglas funcionales de `docs/Explicacion_del_DDD_V7.md` o `docs/Enunciado/PRD Desarrollo.md`, archivar `bootstrap-ms-beca`, alterar `ms-pos` o `ms-plt` y hacer commit o `git push`.

## Capabilities

### New Capabilities

- Ninguna. Es una auditoría y normalización documental y de históricos.

### Modified Capabilities

- Ninguna. No cambia el comportamiento funcional de ningún microservicio.

## Impact

- **Documentación:** `docs/organizacion_repositorio.md`, `docs/convenciones_c_y_ddd.md`, `docs/guia_specs_openspec.md`, `openspec/specs/ms-plt/spec_plt.md` y `openspec/specs/ms-pos/spec_pos.md`.
- **Especificaciones:** corrección del requisito de emisión de oferta en `bootstrap-ms-beca`; sin cambio de reglas de negocio.
- **Históricos:** 11 carpetas renombradas y completadas con artefactos OpenSpec.
- **Configuración:** `openspec/config.yaml` con `operations.archive.guidance`.
- **Roadmaps:** verificados contra los 40 RF; sin marcar RFs incompletos.
- **Código:** sin cambios; solo auditoría estática y ejecución de build/pruebas.
