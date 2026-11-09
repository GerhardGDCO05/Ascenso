# Registro de Auditoría — Organización de MS-BECA y actualizaciones del equipo

- **Fecha y hora de ejecución:** 8 de noviembre de 2026, 16:42 (UTC-4)
- **Rama local:** `Gerhard`
- **Alcance:** análisis de las actualizaciones traídas de GitHub, corrección del pipeline CI/CD y reorganización de la documentación y especificaciones con foco exclusivo en `ms-beca`.
- **Trabajo local:** sin `git push` ni modificación de ramas remotas.

> **Nota de rama:** el registro anterior se asoció a la rama `Core`; el checkout local continúa en `Gerhard` y no se creó ni cambió ninguna rama en esta sesión.

## 1. Análisis de las actualizaciones del equipo

Se revisaron los cambios incorporados desde GitHub (`docs/`, `openspec/config.yaml`, `openspec/roadmaps/` y las especificaciones de `ms-pos` y `ms-plt`):

- La documentación general quedó centralizada en `docs/`: PRD, explicación del DDD V7 y convenciones C#/DDD.
- `openspec/config.yaml` ahora incluye el contexto general y las reglas de validación del equipo.
- Los roadmaps de integrantes y microservicios se mantienen como planificación general.
- `openspec/specs/ms-pos/` y `openspec/specs/ms-plt/` son contexto de otros microservicios y no se modificaron.

## 2. Corrección del pipeline CI/CD

Se corrigió `.github/workflows/ci-cd.yml` para que las pruebas se ejecuten de forma determinista:

- Usa el SDK fijado en `global.json` mediante `actions/setup-dotnet` (`global-json-file`).
- Apunta explícitamente a `Apps/services/ms-becas/Ascenso.Becas.sln` en lugar de buscar el primer `.sln`.
- Restaura, compila en `Release` con `--warnaserror` y ejecuta `dotnet test --no-build`.
- Publica los resultados `.trx` como artefacto del workflow.
- Se activa en `push` a `main`, `develop`, `Gerhard`, `feature-core` y `feature/**`, en `pull_request` a `main`, `develop` y `Gerhard`, y de forma manual.
- Incluye caché de NuGet, `concurrency` y tiempo máximo de ejecución.

## 3. Deduplicación de documentación general

- `openspec/specs/domain-spec.md` era idéntico a `docs/Explicacion_del_DDD_V7.md`; se eliminó la copia de `openspec/specs/`.
- `openspec/specs/convenciones.md` difería de `docs/convenciones_c_y_ddd.md` únicamente en rutas obsoletas (`src/` y `Ascenso.Becas.API`). Se actualizó el archivo de `docs/` a la estructura vigente (`Apps/services/ms-becas/` y `Ascenso.Becas.Api`) y se eliminó la copia de `openspec/specs/`.
- `openspec/config.yaml` ahora incluye como contexto `docs/Explicacion_del_DDD_V7.md` y `docs/convenciones_c_y_ddd.md`, y se corrigieron las rutas de proyecto y los nombres de los roadmaps.

## 4. Especificación duradera de MS-BECA

Se creó `openspec/specs/ms-beca/spec_beca.md`, al mismo nivel que `ms-pos` y `ms-plt`, con:

- Contexto del otorgamiento, ciclo de vida, desembolsos y seguimiento académico.
- Invariantes de estados, no doble beneficio, vencimiento de oferta, compuerta de compromiso, techo presupuestario, OTP y snapshots inmutables.
- Sección de escenarios reservada para que OpenCode los inyecte al archivar cada HU.
- Reglas de negocio de RF-15, RF-16, RF-17, RF-19, RF-20, RF-21, RF-22, RF-24, RF-25, RF-26 y RF-27.
- Integraciones de negocio con `MS-POS`, `MS-PRG`, `MS-PLT` y `MS-AUD`.

## 5. Reorganización del cambio activo

- Se renombró el cambio `bootstrap-ms-bec` a `bootstrap-ms-beca`.
- Se renombró su carpeta de especificaciones de `specs/ms-bec/` a `specs/ms-beca/`.
- Se actualizaron las referencias en `proposal.md`, `design.md` y `tasks.md`.
- Se actualizó `openspec/specs/ejemplo.txt` para usar la ruta de ejemplo `specs/ms-beca/`.
- El registro histórico del 2026-11-07 se conserva en `openspec/changes/archive/2026-11-07-1348-feature-core-shared-kernel-ms-beca/` con una nota sobre el renombrado.

## 6. Validación ejecutada

```text
dotnet restore Apps/services/ms-becas/Ascenso.Becas.sln        → correcto
dotnet build   ... --configuration Release --warnaserror      → 0 advertencias, 0 errores
dotnet test    ... --configuration Release                    → 59/59 pruebas correctas
openspec validate bootstrap-ms-beca --strict                  → cambio válido
```

## 7. Estado local

- Cambios locales sin commit y sin `git push`.
- `ms-pos` y `ms-plt` permanecen intactos.
- El foco de los próximos trabajos es `ms-beca`; la documentación de `docs/` queda como contexto general.
