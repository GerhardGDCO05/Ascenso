# Bitácora de ejecución: `rf6-2-int2-mspos` (2026-11-24)

**Cambio:** `rf6-2-int2-mspos` — HU 6.2: Asignación de postulaciones a evaluadores del comité (`RF-06`, `MS-POS`, Integrante 2).
**Directorio de trabajo:** `Apps/services/ms-postulaciones`

## 1. Evidencia de verificación integral

### 6.1 Restore con lockfile
- **Comando:** `dotnet restore Ascenso.Postulaciones.sln --locked-mode`
- **Resultado:** Código de salida `0`. Restaurados los 8 proyectos de la solución sin modificaciones a `packages.lock.json`.

### 6.2 Compilación en Release
- **Comando:** `dotnet build Ascenso.Postulaciones.sln --configuration Release`
- **Resultado:** Código de salida `0`. `Compilación correcta. 0 Advertencia(s), 0 Errores` (tiempo transcurrido: `00:00:09.24`).

### 6.3 Suite completa de pruebas
- **Comando:** `dotnet test Ascenso.Postulaciones.sln`
- **Resultado:** Código de salida `0`. Total **745 pruebas superadas**, `0` con error, `0` omitidas:
  - `Ascenso.Postulaciones.Domain.Tests.dll`: `287` superadas, `0` con error, `0` omitidas (`99 ms`).
  - `Ascenso.Postulaciones.Application.Tests.dll`: `241` superadas, `0` con error, `0` omitidas (`223 ms`).
  - `Ascenso.Postulaciones.ArchitectureTests.dll`: `11` superadas, `0` con error, `0` omitidas (`351 ms`).
  - `Ascenso.Postulaciones.IntegrationTests.dll` (incluye Testcontainers con PostgreSQL real y migraciones): `206` superadas, `0` con error, `0` omitidas (`5 s`).

### 6.4 Verificación de formato (`dotnet format`)
- **Alcance:** `68` archivos `.cs` nuevos o modificados respecto a `develop` (`git diff --name-only develop...HEAD -- "*.cs"`).
- **Comando:** `dotnet format Ascenso.Postulaciones.sln --verify-no-changes --no-restore --include <68 rutas relativas a ms-postulaciones>`
- **Resultado:** Código de salida `0`. Sin observaciones ni cambios pendientes.

### 6.5 Validación OpenSpec
- `openspec validate rf6-2-int2-mspos --strict` → Código de salida `0` (`Change 'rf6-2-int2-mspos' is valid`).
- `openspec validate --specs` → Código de salida `0` (`Totals: 8 passed, 0 failed (8 items)`).

### 6.6 Actualización de roadmaps
- Actualizados `openspec/roadmaps/por_integrante.md` y `openspec/roadmaps/por_microservicio.md`: agregada y marcada `[x] HU 6.2: Asignación de postulaciones a evaluadores del comité (2026-11-24)` anidada bajo `RF-06` después de `HU 6.1`, manteniendo `RF-06` en `[ ]` hasta completar `HU 6.3`.

## 2. Preguntas abiertas pendientes de `design.md`

1. **Texto de la HU 6.2:** ¿existe una definición con criterios de aceptación fuera de `docs/` que contradiga los escenarios derivados de RF-06, DDD y RN-POS-04?
2. **Origen de `fechaLimiteEvaluacion`:** ¿lo indica el coordinador por asignación (supuesto actual) o se deriva de la convocatoria («cierre de revisión», RN-POS-04) en `MS-PRG`? ¿Se puede ampliar después?
3. **Balanceo:** ¿la carga es el conteo de asignaciones abiertas (supuesto) o debe ponderar plazo/tipo (`INTERNO`/`EXTERNO`)? ¿La selección automática es obligatoria o basta la manual?
4. **Máximo de evaluadores por postulación** y si la jefatura del comité puede ser evaluadora.
5. **Quién valida el estado `EN_EVALUACION` de la postulación** y cómo se obtiene (puerto a `Postulacion` de Integrante 1, llamada a otro servicio): ¿se bloquea 6.2 hasta que exista?
6. **Desvinculación de un miembro con asignaciones abiertas (HU 6.1):** ¿se bloquea, se reasigna o se permite? ¿Debe seguir viéndolas en «mis asignaciones»?
7. **Cancelar o reemplazar una asignación** antes de RF-39: ¿se requiere (nuevo estado) o solo vía conflicto?
8. **Evaluador igual al postulante o con relación con él:** ¿qué se valida en 6.2 (solo RF-39 con conflicto declarado) y cómo se resuelve con evaluadores externos sin cuenta?
9. **Notificación al evaluador (RF-31):** canal y momento (¿al asignar?) sin Outbox.
10. **`MIEMBRO_COMITE` y vista por postulación:** ¿debe el evaluador ver el expediente de su postulación en este servicio o en otro endpoint de RF-08?
