# Registro de Auditoría — Auditoría y normalización integral del repositorio

- **Fecha de ejecución:** 12 de noviembre de 2026
- **Rama local:** `Gerhard`
- **Alcance:** auditoría documental, técnica y de históricos del monorepo `Ascenso`, sin modificar las reglas funcionales de `docs/Explicacion_del_DDD_V7.md` ni de `docs/Enunciado/PRD Desarrollo.md`.
- **Trabajo local:** sin commit y sin `git push`.
- **Cambio activo preservado:** `openspec/changes/bootstrap-ms-beca/` no se archivó ni se renombró.

## 1. Objetivo

Corregir la documentación y las especificaciones, validar la arquitectura y las soluciones .NET, normalizar los históricos de `openspec/changes/archive/` al formato `YYYY-MM-DD-[ID-Cambio]-[Integrante]-[Microservicio]` y blindar las reglas de archivado en `openspec/config.yaml`.

## 2. Fase 1 — Documentación y validación técnica

### 2.1 Correcciones documentales

- Se eliminó `openspec/specs/ejemplo.txt`; su contenido útil se trasladó a `docs/guia_specs_openspec.md`.
- Se limpiaron los escapes Markdown de `openspec/specs/ms-plt/spec_plt.md` y `openspec/specs/ms-pos/spec_pos.md` (`\## Scenarios`, `### 1\.`, `COORDINADOR\_PROGRAMA`, etc.).
- Se reescribió `docs/organizacion_repositorio.md` con la estructura real, la convención de nombres y el archivado sin `HHMM`.
- Se corrigió `docs/convenciones_c_y_ddd.md`: `{; }` → `{ get; }` y ejemplos genéricos `<Contexto>DbContext` / `<Entidad>Repository`.
- Se verificaron ambos roadmaps contra los 40 RF de la Sección 5 de `docs/Explicacion_del_DDD_V7.md`. Ambos contienen los 40 RF; ninguno se marcó porque no hay funcionalidad completa de punta a punta.
- Se corrigió el warning de requisito largo en `openspec/changes/bootstrap-ms-beca/specs/ms-beca/otorgamiento-beca/spec.md`, moviendo los detalles de la emisión de oferta a los escenarios.

### 2.2 Auditoría estática de código

- Soluciones existentes: `Apps/services/ms-becas/Ascenso.Becas.sln` y `Apps/services/ms-postulaciones/Ascenso.Postulaciones.sln`.
- Cada solución tiene los cuatro proyectos hexagonales: `Domain`, `Application`, `Infrastructure` y `Api`.
- `Domain` no tiene `PackageReference` ni `ProjectReference`.
- No hay namespaces block-scoped; todos son file-scoped en PascalCase.
- No hay setters públicos en propiedades mutables del dominio.
- El único `using` externo aparente es `System.Net` en `CompromisoBecario.cs`, que pertenece a la biblioteca base de .NET.
- La nomenclatura de negocio del dominio se mantiene en español.

## 3. Fase 2 — Normalización de históricos

### 3.1 Criterios aplicados

- Formato de carpeta: `YYYY-MM-DD-[ID-Cambio]-[Integrante]-[Microservicio]`, sin hora.
- Integrante `int4` para los históricos de becas.
- Microservicio compacto `msbeca`, siguiendo el patrón `mspos` de la plantilla.
- `[ID-Cambio]` con tipo (`tech`, `test`, `chore`) más un slug corto para evitar colisiones.
- Bitácora original conservada y renombrada a `log-YYYY-MM-DD-[ID-Cambio]-[Integrante]-[Microservicio].md`.
- Artefactos mínimos generados: `.openspec.yaml`, `proposal.md`, `design.md` y `tasks.md` con todas las tareas en `[x]`.
- `skip_specs: true` en todos los históricos por ser cambios técnicos sin nueva especificación de comportamiento.

### 3.2 Históricos renombrados

| Nombre anterior | Nombre nuevo |
| :--- | :--- |
| `2026-11-07-1348-feature-core-shared-kernel-ms-beca` | `2026-11-07-tech-shared-kernel-int4-msbeca` |
| `2026-11-08-1642-organizacion-ms-beca` | `2026-11-08-tech-organizacion-int4-msbeca` |
| `2026-11-08-1646-organizacion-general-ms-beca` | `2026-11-08-tech-organizacion-general-int4-msbeca` |
| `2026-11-08-1653-ci-general-ms-beca` | `2026-11-08-tech-ci-general-int4-msbeca` |
| `2026-11-08-1727-revision-general-ms-beca` | `2026-11-08-tech-revision-general-int4-msbeca` |
| `2026-11-10-2348-verificacion-tasks-ms-beca` | `2026-11-10-test-verificacion-tasks-int4-msbeca` |
| `2026-11-11-0008-task-1-3-proyectos-prueba-ms-beca` | `2026-11-11-test-proyectos-prueba-int4-msbeca` |
| `2026-11-11-0049-preparacion-entorno-ms-beca` | `2026-11-11-chore-preparacion-entorno-int4-msbeca` |
| `2026-11-11-0118-modelo-dominio-otorgamiento-ms-beca` | `2026-11-11-tech-modelo-dominio-otorgamiento-int4-msbeca` |
| `2026-11-12-0107-persistencia-dbcontext-ms-beca` | `2026-11-12-tech-persistencia-dbcontext-int4-msbeca` |
| `2026-11-12-0125-persistencia-seccion-4-2-4-8-ms-beca` | `2026-11-12-tech-persistencia-seccion-4-2-4-8-int4-msbeca` |

La plantilla `2026-11-08-tech-setup-int1-mspos` se conservó sin cambios por ya cumplir el formato.

### 3.3 Artefactos generados

Cada uno de los 11 históricos contiene ahora:

- `.openspec.yaml` con `schema: spec-driven` y `skip_specs: true`.
- `proposal.md` con `Why`, `What Changes`, `Capabilities` e `Impact`.
- `design.md` con el resumen técnico de lo ejecutado.
- `tasks.md` con todas las tareas completadas.
- `log-2026-11-12-...md` con la bitácora original conservada.

## 4. Fase 3 — Blindaje de archivado

Se mantuvo `context: |` como string válido de OpenSpec 2.0.0 y se agregó `operations.archive.guidance` con dos reglas obligatorias:

1. Formato de carpeta `YYYY-MM-DD-[ID-Cambio]-[Integrante]-[Microservicio]`, sin hora `HHMM`.
2. Verificación previa de `.openspec.yaml`, `proposal.md`, `design.md`, `tasks.md` con todas las tareas en `[x]` y la bitácora `log-YYYY-MM-DD-[ID-Cambio]-[Integrante]-[Microservicio].md`.

## 5. Validaciones ejecutadas

```text
openspec validate --all --strict       → 1 cambio válido, 0 errores, 0 warnings
openspec validate --archived --strict  → 13 históricos válidos, 0 errores
git diff --check                       → sin errores de espacio
dotnet build ms-becas -c Release --no-restore -p:NoWarn=NU1900
                                       → 0 advertencias, 0 errores
dotnet test ms-becas -c Release        → 253/253 pruebas correctas
  Domain.Tests                         → 136/136
  IntegrationTests                     → 108/108
  ArchitectureTests                    →   8/8
  Application.Tests                    →   1/1
dotnet build ms-postulaciones -c Release --no-restore
                                       → 0 advertencias, 0 errores
dotnet test ms-postulaciones -c Release
                                       → 0 pruebas (proyecto esqueleto sin tests), exit 0
```

Durante la primera ejecución, `dotnet build` quedó bloqueado intentando resolver IPv6 contra `api.nuget.org`. Se detuvo el proceso, se restauró `ms-postulaciones` con `DOTNET_SYSTEM_NET_DISABLEIPV6=1` y se ejecutaron build y pruebas con `--no-restore` sobre los paquetes ya cacheados. Las advertencias `NU1900` de `ms-becas` provienen de la falta de acceso al índice de vulnerabilidades de NuGet, no del código; se excluyeron con `-p:NoWarn=NU1900` para obtener una compilación limpia.

## 6. Archivos innecesarios detectados (pendientes de confirmación)

- `openspec/specs/.gitkeep`: el directorio ya contiene las especificaciones de `ms-beca`, `ms-plt` y `ms-pos`.
- `openspec/changes/archive/.gitkeep`: el directorio ya contiene carpetas históricas.
- `Apps/services/ms-becas/Ascenso.Becas.Infrastructure/Persistence/.gitkeep`: el directorio ya contiene archivos de persistencia.

No se eliminó ninguno de estos archivos para no alterar el repositorio sin confirmación.

## 7. Estado local

- Cambios sin commit y sin `git push`.
- `ms-pos` y `ms-plt` no se modificaron.
- `bootstrap-ms-beca` sigue activo en `openspec/changes/bootstrap-ms-beca/`.
- Los roadmaps mantienen los 40 RF sin marcar porque ninguno está funcionalmente completo.
