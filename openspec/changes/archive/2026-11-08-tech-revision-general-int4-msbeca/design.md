## How

Se realizó una revisión estática y funcional del repositorio para alinear el estado documentado con el código realmente existente.

### Estado general verificado

- La organización por microservicio está documentada en `docs/organizacion_repositorio.md`.
- La documentación general está centralizada en `docs/` sin copias duplicadas en `openspec/specs/`.
- `openspec/specs/ms-beca/spec_beca.md` existe junto a `ms-pos` y `ms-plt`.
- `ms-pos` y `ms-plt` permanecen intactos.
- El cambio activo es `bootstrap-ms-beca`.
- El pipeline descubre todas las soluciones de `Apps/services/`.
- No hay referencias rotas a `bootstrap-ms-bec`, `ms-bec`, `domain-spec.md`, `convenciones.md` ni `src/`.

### Avance del cambio

- Tareas completadas: 9 de 114 (`1.1`, `1.2` y `2.1` a `2.7`).
- Feature 2 (shared kernel) implementada y validada.
- Feature 3 (modelo de otorgamiento), persistencia, aplicación, integraciones, seguridad y API pendientes.
- No existen aún `Beca`, `OfertaBeca`, `CompromisoBecario` ni `HistorialEstadoBeca`.

### Revisión de roadmaps

- Se revisaron `RF-15`, `RF-16`, `RF-17`, `RF-19`, `RF-20`, `RF-21`, `RF-22`, `RF-24`, `RF-25`, `RF-26` y `RF-27`.
- Ningún RF se marcó porque ninguno estaba funcionalmente completo.
- `RF-18` pertenece a `MS-AUD`/`MS-BEC` y tampoco estaba implementado.

### Validación ejecutada

```text
dotnet build --configuration Release --warnaserror  → 0 advertencias, 0 errores
dotnet test --configuration Release                 → 59/59 correctas
dotnet format --verify-no-changes                   → correcto
actionlint .github/workflows/ci-cd.yml              → sin observaciones
openspec validate bootstrap-ms-beca --strict        → cambio válido
git diff --check                                    → sin errores
```
