# Bitácora de Cierre — `rf8-1-int2-mspos` (2026-11-25)

## Resumen

Se completó la implementación y verificación integral de la historia de usuario **HU 8.1: Registro de puntuaciones y comentarios de evaluadores sobre postulaciones asignadas** (`RF-08`) en `Apps/services/ms-postulaciones` dentro del cambio OpenSpec `rf8-1-int2-mspos` (`26/26` tareas completadas).

## Evidencia de verificación integral (Sección 6)

Todos los comandos .NET se ejecutaron desde `Apps/services/ms-postulaciones`:

1. **6.1 Restore (`dotnet restore Ascenso.Postulaciones.sln --locked-mode`):**
   - Resultado: código de salida `0` (`Ascenso.Postulaciones.Domain`, `Application`, `Infrastructure`, `Api`, `Domain.Tests`, `Application.Tests`, `ArchitectureTests`, `IntegrationTests` restaurados con `packages.lock.json` sin cambios).
   - Nota de entorno: se registraron únicamente advertencias `NU1900` por imposibilidad de consultar el índice de vulnerabilidades en `https://api.nuget.org/v3/index.json` en entorno sin salida a red.

2. **6.2 Build (`dotnet build Ascenso.Postulaciones.sln --configuration Release --no-restore`):**
   - Resultado: `Compilación correcta.`, `0 Errores`, `0` advertencias de compilación/código (las 7 advertencias reportadas corresponden únicamente a `NU1900` de auditoría NuGet sin conexión).

3. **6.3 Suite de pruebas completa (`dotnet test Ascenso.Postulaciones.sln --no-restore`):**
   - `Ascenso.Postulaciones.Domain.Tests`: **386** superadas, `0` con error, `0` omitidas (`103 ms`).
   - `Ascenso.Postulaciones.Application.Tests`: **311** superadas, `0` con error, `0` omitidas (`214 ms`).
   - `Ascenso.Postulaciones.ArchitectureTests`: **11** superadas, `0` con error, `0` omitidas (`233 ms`) — sin modificaciones.
   - `Ascenso.Postulaciones.IntegrationTests` (incluye pruebas con PostgreSQL real vía Testcontainers): **267** superadas, `0` con error, `0` omitidas (`10 s`).
   - **Total:** **975** pruebas superadas, `0` fallos, `0` omitidas.

4. **6.4 Formato (`dotnet format Ascenso.Postulaciones.sln --verify-no-changes --no-restore --include <97 rutas .cs>`):**
   - Lista exacta obtenida mediante `git diff --name-only develop...HEAD -- "*.cs"` (`97` archivos `.cs` nuevos o modificados respecto a `develop`, relativos a `Apps/services/ms-postulaciones/`).
   - Resultado: código de salida `0`, sin diferencias de formato.

5. **6.5 Validación OpenSpec:**
   - `openspec validate rf8-1-int2-mspos --strict`: `Change 'rf8-1-int2-mspos' is valid`.
   - `openspec validate --specs`: `Totals: 8 passed, 0 failed (8 items)` (previo a sincronizar `ms-pos/evaluaciones`).

6. **6.6 Roadmaps:**
   - Se actualizó `openspec/roadmaps/por_integrante.md` y `openspec/roadmaps/por_microservicio.md` agregando y marcando `- [x] HU 8.1: Registro de puntuaciones y comentarios de evaluadores sobre postulaciones asignadas (2026-11-25)` anidada bajo `RF-08`, manteniendo `RF-08` en `[ ]` por quedar pendiente HU 8.2.

## Decisiones aplicadas y ajustes mínimos de DI en pruebas previas

- **Modelo e invariantes DDD:** agregado raíz `Evaluacion`, entidad hija `CalificacionCriterio`, objeto de valor inmutable `DatosCalificacionCriterio`, servicio de dominio puro `ServicioCalculoPuntajeEvaluacion` (redondeo `MidpointRounding.AwayFromZero` a 2 decimales), enum `EstadoEvaluacion` (`BORRADOR`, `ENVIADA_DEFINITIVA`, `INVALIDADA_POR_CONFLICTO`) y métodos aditivos `IniciarEvaluacion` / `CompletarEvaluacion` en `AsignacionEvaluacion`.
- **Puertos ISP y concurrencia `C5`:** se crearon `IEvaluacionRepository`, `IConsultaAsignacionParaEvaluacionRepository`, `IConsultaRubricaParaEvaluacionRepository` e `IBloqueadorRubricaEvaluacion` (`BloqueadorRubricaEvaluacionAdapter` sobre `BloquearRubricaCommandHandler` de HU 7.2) sin alterar puertos previos (`IRubricaRepository`, `IComiteEvaluadorRepository`, `IAsignacionEvaluacionRepository`). El guardado del borrador confirma primero `Evaluacion` + `AsignacionEvaluacion` antes de invocar el bloqueo idempotente de rúbrica.
- **Ajustes mínimos de DI en fábricas `WebApplicationFactory` de pruebas de integración previas:** al registrar en `Infrastructure/DependencyInjection.cs` los 3 repositorios ISP nuevos (`IEvaluacionRepository`, `IConsultaAsignacionParaEvaluacionRepository`, `IConsultaRubricaParaEvaluacionRepository`), las fábricas `WebApplicationFactory` existentes que reemplazan la infraestructura por dobles en memoria requirieron añadir `RemoveAll` + `AddSingleton(Substitute.For<...>())` para esas 3 interfaces (+6 líneas por archivo) en:
  - `tests/Ascenso.Postulaciones.IntegrationTests/Controllers/PostulantesControllerTests.cs`
  - `tests/Ascenso.Postulaciones.IntegrationTests/Controllers/RubricasControllerTests.cs`
  - `tests/Ascenso.Postulaciones.IntegrationTests/Controllers/ComitesEvaluadoresControllerTests.cs`
  - `tests/Ascenso.Postulaciones.IntegrationTests/Controllers/AsignacionesEvaluacionControllerTests.cs`
  - `tests/Ascenso.Postulaciones.IntegrationTests/Controllers/ExcepcionEvaluadorUnicoControllerTests.cs`
  - `tests/Ascenso.Postulaciones.IntegrationTests/Seguridad/AutorizacionRubricasTests.cs`
  - `tests/Ascenso.Postulaciones.IntegrationTests/Seguridad/AutorizacionComitesTests.cs`
  - `tests/Ascenso.Postulaciones.IntegrationTests/Seguridad/AutorizacionAsignacionesTests.cs`
  - `tests/Ascenso.Postulaciones.IntegrationTests/Seguridad/AutorizacionExcepcionEvaluadorUnicoTests.cs`
  - Y `tests/Ascenso.Postulaciones.IntegrationTests/EnsambladoApiTests.cs` (+3 líneas para resolución de los 3 nuevos puertos).
  - No se modificó ningún archivo `.cs` de `Postulantes`, `Rubricas`, `Comites` ni otros microservicios.

## Preguntas abiertas heredadas de `design.md`

1. **Desglose formal de RF-08 en HUs:** confirmar con el equipo que HU 8.2 cubrirá la visibilidad cruzada entre coevaluadores tras el cierre formal de la convocatoria y la validación del cierre contra `Convocatoria` (dejando `RF-08` en `[ ]` al finalizar HU 8.1).
2. **Visibilidad del `COORDINADOR_PROGRAMA` antes del cierre:** confirmar si `COORDINADOR_PROGRAMA` puede ver las puntuaciones de evaluaciones `ENVIADA_DEFINITIVA` de inmediato (diseño actual en `GET /api/v1/evaluaciones/postulaciones/{postulacionId}`) o si también debe esperar al cierre del periodo de revisión (HU 8.2) viendo hasta entonces solo el estado de avance de `AsignacionEvaluacion`.
3. **Comentarios obligatorios u opcionales:** el diseño actual trata `comentario` por criterio y `comentariosGenerales` como opcionales (máx. 1000 y 2000 caracteres); confirmar si algún programa o criterio exigirá comentario obligatorio cuando se asigne un puntaje extremo.
4. **Evaluador desvinculado del comité (HU 6.1) con asignaciones abiertas:** coherente con HU 6.2, HU 8.1 valida la propiedad de la `AsignacionEvaluacion` y su estado sin volver a consultar si la membresía del comité sigue `Activo`; confirmar si al desvincular a un miembro debe bloquearse también el registro de evaluaciones.
5. **Reapertura excepcional de una evaluación enviada:** `docs/` establece inmutabilidad tras `ENVIADA_DEFINITIVA` (salvo `INVALIDADA_POR_CONFLICTO` en RF-39); confirmar que no existe ningún caso administrativo de reapertura por el coordinador.
