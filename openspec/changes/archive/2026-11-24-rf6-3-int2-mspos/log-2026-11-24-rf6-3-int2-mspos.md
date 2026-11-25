# Bitácora de ejecución: `rf6-3-int2-mspos` (2026-11-24)

**Cambio:** `rf6-3-int2-mspos` — HU 6.3: Autorización de excepción de evaluador único por el coordinador (`RF-06`, `MS-POS`, Integrante 2).
**Directorio de trabajo:** `Apps/services/ms-postulaciones`

## 1. Evidencia de verificación integral

### 6.1 Restore con lockfile
- **Comando:** `dotnet restore Ascenso.Postulaciones.sln --locked-mode`
- **Resultado:** Código de salida `0`. Restaurados los 8 proyectos de la solución sin modificaciones a `packages.lock.json`.

### 6.2 Compilación en Release
- **Comando:** `dotnet build Ascenso.Postulaciones.sln --configuration Release`
- **Resultado:** Código de salida `0`. `Compilación correcta. 0 Advertencia(s), 0 Errores` (tiempo transcurrido: `00:00:08.17`).

### 6.3 Suite completa de pruebas
- **Comando:** `dotnet test Ascenso.Postulaciones.sln`
- **Resultado:** Código de salida `0`. Total **827 pruebas superadas**, `0` con error, `0` omitidas:
  - `Ascenso.Postulaciones.Domain.Tests.dll`: `316` superadas, `0` con error, `0` omitidas (`115 ms`).
  - `Ascenso.Postulaciones.Application.Tests.dll`: `267` superadas, `0` con error, `0` omitidas (`190 ms`).
  - `Ascenso.Postulaciones.ArchitectureTests.dll`: `11` superadas, `0` con error, `0` omitidas (`272 ms`).
  - `Ascenso.Postulaciones.IntegrationTests.dll` (incluye Testcontainers con PostgreSQL real y migración `20261124234414_ExcepcionEvaluadorUnico`): `233` superadas, `0` con error, `0` omitidas (`8 s`).

### 6.4 Verificación de formato (`dotnet format`)
- **Alcance:** `57` archivos `.cs` nuevos o modificados respecto a `develop` (`git diff --name-only develop...HEAD -- "*.cs"`).
- **Comando:** `dotnet format Ascenso.Postulaciones.sln --verify-no-changes --no-restore --include <57 rutas relativas a ms-postulaciones>`
- **Resultado:** Código de salida `0`. Sin observaciones ni cambios pendientes.

### 6.5 Validación OpenSpec
- `openspec validate rf6-3-int2-mspos --strict` → Código de salida `0` (`Change 'rf6-3-int2-mspos' is valid`).
- `openspec validate --specs` → Código de salida `0` (`Totals: 8 passed, 0 failed (8 items)`).

### 6.6 Actualización de roadmaps
- Actualizados `openspec/roadmaps/por_integrante.md` y `openspec/roadmaps/por_microservicio.md`: agregada y marcada `[x] HU 6.3: Autorización de excepción de evaluador único por el coordinador (2026-11-24)` anidada bajo `RF-06` después de `HU 6.2`, y marcado `[x] RF-06` en ambos roadmaps tras verificar que no quedan HUs pendientes (6.1, 6.2 y 6.3 completas).

## 2. Ajustes mínimos a pruebas de HU 6.2 y `WebApplicationFactory`

Los cambios sobre suites existentes se limitaron exclusivamente a lo necesario por el nuevo umbral del validador y la nueva dependencia `IExcepcionEvaluadorUnicoRepository`, sin modificar aserciones de negocio ni contratos previos:

1. **Validador de asignación (`tests/Ascenso.Postulaciones.Application.Tests/Asignaciones/Comandos/AsignarEvaluadoresPostulacionCommandValidatorTests.cs`):** se renombró `Debe_Rechazar_Cuando_CantidadEvaluadoresEsMenorQueDos` a `Debe_Rechazar_Cuando_CantidadEvaluadoresEsMenorQueUno`, retirando `[InlineData(1)]` porque el validador ahora admite `CantidadEvaluadores >= 1` (la exigencia de 2 evaluadores sin excepción se mantiene en el dominio y en `AsignarEvaluadoresPostulacionExcepcionTests`).
2. **Handler de asignación (`tests/Ascenso.Postulaciones.Application.Tests/Asignaciones/Comandos/AsignarEvaluadoresPostulacionCommandHandlerTests.cs`):** se incorporó `_excepcionRepository` (`Substitute.For<IExcepcionEvaluadorUnicoRepository>()` devolviendo `null` en `ObtenerPorPostulacionAsync`) al constructor de `AsignarEvaluadoresPostulacionCommandHandler`.
3. **Configuración de `WebApplicationFactory` en pruebas de integración:** se añadió `RemoveAll<IExcepcionEvaluadorUnicoRepository>()` + `AddSingleton(Substitute.For<IExcepcionEvaluadorUnicoRepository>())` en `AsignacionesEvaluacionControllerTests.cs`, `ComitesEvaluadoresControllerTests.cs`, `PostulantesControllerTests.cs`, `RubricasControllerTests.cs`, `AutorizacionAsignacionesTests.cs`, `AutorizacionComitesTests.cs` y `AutorizacionRubricasTests.cs`, y `AddSingleton(Substitute.For<IExcepcionEvaluadorUnicoRepository>())` en `EnsambladoApiTests.cs`, para aislar la resolución de DI del servicio sin alterar los casos ni aserciones existentes.

## 3. Preguntas abiertas pendientes de `design.md`

1. **Texto de la HU 6.3:** ¿existe una definición con criterios de aceptación fuera de `docs/` que contradiga los escenarios derivados de RF-06 y RN-POS-04?
2. **Justificación:** ¿longitud mínima y máxima oficiales (supuesto 10 y 1000)? ¿Puede adjuntarse evidencia documental?
3. **Revocación:** ¿debe poder retirarse una excepción (con nueva justificación e historial) o es definitiva?
4. **`correlacionId`:** ¿debe provenir de un encabezado de correlación de la plataforma en lugar de generarse en el handler?
5. **Auditoría:** ¿la publicación a `MS-AUD` es síncrona (llamada interna) o por evento, y desde qué HU?
6. **Conflicto de interés:** ¿quién cubre el caso en que el único coordinador del programa tenga conflicto (delegación a Dirección, RF-39)?
7. **Momento de la autorización:** ¿puede autorizarse antes de que la postulación llegue a `EN_EVALUACION` o solo cuando esté en ese estado?
8. **Regla «menos de 2 vigentes»:** ¿se permite autorizar anticipadamente con 2 vigentes (p. ej. por una baja futura), o es correcto rechazarla con `EXCEPCION_NO_APLICABLE`?
9. **Notificación:** ¿a quién se avisa (Dirección, comité) cuando se autoriza una excepción?
