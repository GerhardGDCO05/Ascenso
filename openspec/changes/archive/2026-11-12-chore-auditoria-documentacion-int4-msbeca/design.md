## How

La auditoría se ejecutó en tres fases secuenciales, sin alterar las reglas funcionales de la especificación maestra ni el cambio activo `bootstrap-ms-beca`.

### Fase 1 — Documentación y validación técnica

- Se eliminó `openspec/specs/ejemplo.txt` y su contenido útil se trasladó a `docs/guia_specs_openspec.md`.
- Se limpiaron los escapes Markdown de `openspec/specs/ms-plt/spec_plt.md` y `openspec/specs/ms-pos/spec_pos.md`.
- Se reescribió `docs/organizacion_repositorio.md` para reflejar la estructura real, la convención de nombres y el archivado sin `HHMM`.
- Se corrigió `docs/convenciones_c_y_ddd.md` (`{; }` → `{ get; }`) y se generalizaron los ejemplos de infraestructura.
- Se verificaron los dos roadmaps contra los 40 RF de la Sección 5 de `docs/Explicacion_del_DDD_V7.md`; todos los RF permanecen pendientes porque ninguno está funcionalmente completo.
- Se corrigió el warning de `bootstrap-ms-beca` moviendo los detalles de la emisión de oferta a los escenarios `GIVEN/WHEN/THEN`.

### Fase 2 — Normalización de históricos

- Se auditó cada carpeta de `openspec/changes/archive/` y se identificó fecha, tipo, integrante y microservicio desde su bitácora.
- Se renombraron 11 carpetas al formato `YYYY-MM-DD-[ID-Cambio]-[Integrante]-[Microservicio]`, sin hora.
- Se renombró cada bitácora a `log-YYYY-MM-DD-[ID-Cambio]-[Integrante]-[Microservicio].md` sin alterar su contenido.
- Se generaron `.openspec.yaml`, `proposal.md`, `design.md` y `tasks.md` en cada histórico, con `skip_specs: true` por tratarse de cambios técnicos.
- No se archivó ni renombró `openspec/changes/bootstrap-ms-beca/`, que sigue activo.

### Fase 3 — Blindaje de reglas

- Se mantuvo `context: |` como string válido de OpenSpec 2.0.0.
- Se agregó `operations.archive.guidance` con las dos reglas obligatorias: formato de carpeta sin `HHMM` y artefactos mínimos antes de archivar.

### Auditoría estática de código

- Solo existen dos soluciones: `Ascenso.Becas.sln` y `Ascenso.Postulaciones.sln`, cada una con `Domain`, `Application`, `Infrastructure` y `Api`.
- `Domain` no tiene `PackageReference` ni `ProjectReference` externos.
- No hay namespaces block-scoped, setters públicos en propiedades mutables ni namespaces en minúscula.
- El único `using` externo aparente es `System.Net` en `CompromisoBecario.cs`, que pertenece a la biblioteca base de .NET.

### Validación ejecutada

```text
openspec validate --all --strict       → 1 cambio válido, 0 errores, 0 warnings
openspec validate --archived --strict  → 13 históricos válidos (incluido este)
dotnet build ms-becas                  → 0 advertencias, 0 errores (NU1900 excluida)
dotnet test ms-becas                   → 253/253 pruebas correctas
dotnet build ms-postulaciones          → 0 advertencias, 0 errores
dotnet test ms-postulaciones           → 0 pruebas (esqueleto sin tests), exit 0
```
