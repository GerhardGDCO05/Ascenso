# Bitácora de ejecución: `rf6-1-int2-mspos`

- **Fecha:** 2026-11-22
- **Cambio:** `rf6-1-int2-mspos` — HU 6.1: Conformación del comité evaluador por programa y convocatoria (`RF-06`)
- **Microservicio:** `MS-POS` (`Apps/services/ms-postulaciones`)
- **Integrante:** Integrante 2 (Evaluación, Comités y Selección de Mérito)

---

## 1. Evidencia de verificaciones integrales (Sección 6)

### 6.1 Restauración de paquetes (`--locked-mode`)
- **Comando:** `dotnet restore Ascenso.Postulaciones.sln --locked-mode` (desde `Apps/services/ms-postulaciones`)
- **Resultado:** Exitoso (código de salida `0`). Restaurados los 8 proyectos de la solución (`Domain`, `Application`, `Infrastructure`, `Api`, `Domain.Tests`, `Application.Tests`, `ArchitectureTests`, `IntegrationTests`) sin modificar `packages.lock.json`.

### 6.2 Compilación en Release
- **Comando:** `dotnet build Ascenso.Postulaciones.sln --configuration Release`
- **Resultado:** `Compilación correcta. 0 Advertencia(s), 0 Errores` (código de salida `0`, `00:00:07.07`).

### 6.3 Suite completa de pruebas (incluyendo ArchitectureTests y Testcontainers)
- **Comando:** `dotnet test Ascenso.Postulaciones.sln`
- **Resultado:** Exitoso (código de salida `0`), **619 pruebas superadas, 0 con error, 0 omitidas**:
  - `Ascenso.Postulaciones.Domain.Tests`: `244` superadas, `0` con error, `0` omitidas (`76 ms`)
  - `Ascenso.Postulaciones.Application.Tests`: `194` superadas, `0` con error, `0` omitidas (`151 ms`)
  - `Ascenso.Postulaciones.ArchitectureTests`: `11` superadas, `0` con error, `0` omitidas (`360 ms`)
  - `Ascenso.Postulaciones.IntegrationTests`: `170` superadas, `0` con error, `0` omitidas (`4 s`)

### 6.4 Verificación de formato (`--include`)
- **Comando:**
  `git diff --name-only develop...HEAD -- "*.cs"` (68 archivos `.cs` nuevos o modificados respecto a `develop`, relativos a `Apps/services/ms-postulaciones`) seguido de:
  `dotnet format Ascenso.Postulaciones.sln --verify-no-changes --no-restore --include <68 archivos>`
- **Resultado:** Exitoso (código de salida `0`), salida limpia sin cambios pendientes de formato.

### 6.5 Validación OpenSpec
- **Comandos:**
  - `openspec validate rf6-1-int2-mspos --strict` → `Change 'rf6-1-int2-mspos' is valid`
  - `openspec validate --specs` → `Totals: 7 passed, 0 failed (7 items)` (previo al archivado/sincronización)

### 6.6 Actualización de roadmaps
- Actualizados `openspec/roadmaps/por_integrante.md` y `openspec/roadmaps/por_microservicio.md`:
  - Agregada y marcada `[x] HU 6.1: Conformación del comité evaluador por programa y convocatoria (2026-11-22)` anidada bajo `RF-06`.
  - Mantenido `[ ] **RF-06**: Conformación de comités evaluadores` sin marcar hasta completar las HU 6.2 y 6.3.

---

## 2. Preguntas abiertas registradas en `design.md`

Las siguientes 7 preguntas abiertas quedan documentadas para validación con el equipo y coordinación con futuras HUs (HU 6.2, HU 6.3 y RF-39):

1. **Texto de la HU 6.1:** ¿existe una definición y criterios de aceptación fuera de `docs/` que contradigan los escenarios derivados de RF-06 y el DDD?
2. **Valores de `TipoEvaluador` y `RolMiembroComite`:** ¿se confirman `INTERNO`/`EXTERNO` y `JEFATURA`/`EVALUADOR`? ¿La jefatura puede ser `EXTERNO`?
3. **Lectura por `MIEMBRO_COMITE`:** ¿puede un miembro consultar la nómina de su propio comité (hoy `403`)?
4. **Cambio de jefatura y desactivación del comité:** ¿van en 6.1 o posteriores?
5. **Desvinculación con asignaciones vigentes:** ¿se bloquea, se reasigna o se permite (se define con HU 6.2 y RF-39)?
6. **Un comité por programa y convocatoria o por programa:** el DDD tiene ambas claves; se asume una combinación única.
7. **Validación de usuario evaluador:** ¿debe verificarse en MS-PLT (rol `MIEMBRO_COMITE` o usuario externo) y cómo se registra un evaluador externo sin cuenta interna?
