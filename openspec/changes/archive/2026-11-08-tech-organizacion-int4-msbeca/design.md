## How

Se revisaron las actualizaciones incorporadas desde GitHub y se aplicaron correcciones de organización, CI y documentación con foco exclusivo en `ms-beca`.

### Pipeline CI/CD

- El workflow usa `actions/setup-dotnet` con `global-json-file` para respetar el SDK fijado.
- Apunta explícitamente a `Apps/services/ms-becas/Ascenso.Becas.sln`.
- Ejecuta restore, build en `Release` con `--warnaserror` y `dotnet test --no-build`.
- Publica los resultados `.trx` como artefacto.
- Se activa en `push` a `main`, `develop`, `Gerhard`, `feature-core` y `feature/**`, en `pull_request` a `main`, `develop` y `Gerhard`, y de forma manual.
- Incluye caché de NuGet, `concurrency` y tiempo máximo de ejecución.

### Deduplicación documental

- `openspec/specs/domain-spec.md` era idéntico a `docs/Explicacion_del_DDD_V7.md`; se eliminó la copia.
- `openspec/specs/convenciones.md` se actualizó en `docs/convenciones_c_y_ddd.md` con las rutas vigentes y se eliminó la copia.
- `openspec/config.yaml` quedó referenciando la documentación de `docs/`.

### Especificación duradera y renombrado

- Se creó `openspec/specs/ms-beca/spec_beca.md` con contexto, invariantes, escenarios reservados, reglas de RF-15 a RF-27 e integraciones con `MS-POS`, `MS-PRG`, `MS-PLT` y `MS-AUD`.
- El cambio activo pasó de `bootstrap-ms-bec` a `bootstrap-ms-beca` y su carpeta de specs de `ms-bec` a `ms-beca`.
- Se actualizaron `proposal.md`, `design.md`, `tasks.md` y el borrador `openspec/specs/ejemplo.txt`.

### Validación ejecutada

```text
dotnet restore Ascenso.Becas.sln                    → correcto
dotnet build --configuration Release --warnaserror  → 0 advertencias, 0 errores
dotnet test --configuration Release                 → 59/59 correctas
openspec validate bootstrap-ms-beca --strict        → cambio válido
```
