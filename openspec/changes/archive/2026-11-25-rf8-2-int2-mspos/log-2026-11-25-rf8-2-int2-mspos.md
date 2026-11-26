# Bitácora de implementación — `rf8-2-int2-mspos` (HU 8.2)

- **Fecha:** 2026-11-25
- **Cambio OpenSpec:** `rf8-2-int2-mspos`
- **Microservicio:** `Apps/services/ms-postulaciones` (`MS-POS`)
- **Alcance:** HU 8.2 — Visibilidad post-cierre entre coevaluadores y validación del cierre del periodo de revisión (cierre completo de **RF-08**).

---

## 1. Evidencia real de verificación integral

Todos los comandos se ejecutaron desde `Apps/services/ms-postulaciones/` (y la raíz del repositorio para `openspec validate`):

1. **Restauración bloqueada (`5.1`):**
   - Comando: `dotnet restore Ascenso.Postulaciones.sln --locked-mode`
   - Resultado: **Código de salida `0`**. Todos los proyectos (`Domain`, `Application`, `Infrastructure`, `Api`, `Domain.Tests`, `Application.Tests`, `ArchitectureTests`, `IntegrationTests`) restaurados sin modificaciones en `packages.lock.json` (confirmado con `git status --short "*packages.lock.json"` vacío).

2. **Compilación Release (`5.2`):**
   - Comando: `dotnet build Ascenso.Postulaciones.sln --configuration Release`
   - Resultado: **Compilación correcta — `0 Advertencia(s)`, `0 Errores`** (tiempo `00:00:07.02`).

3. **Suite completa de pruebas (`5.3`):**
   - Comando: `dotnet test Ascenso.Postulaciones.sln`
   - Resultado: **1,143 pruebas superadas, 0 fallidas, 0 omitidas** (código de salida `0`):
     - `Ascenso.Postulaciones.Domain.Tests`: **472** superadas (`0` errores, `0` omitidas, `131 ms`)
     - `Ascenso.Postulaciones.Application.Tests`: **360** superadas (`0` errores, `0` omitidas, `334 ms`)
     - `Ascenso.Postulaciones.ArchitectureTests`: **11** superadas (`0` errores, `0` omitidas, `269 ms`) — sin modificaciones
     - `Ascenso.Postulaciones.IntegrationTests`: **300** superadas (`0` errores, `0` omitidas, `8 s`) — incluye pruebas reales contra PostgreSQL con Testcontainers (`EvaluacionCoevaluadoresPostgreSqlTests` y `EvaluacionPostgreSqlTests`).

4. **Formato de código acotado (`5.4`):**
   - Lista exacta obtenida con `git diff --name-only develop...HEAD -- "*.cs"` (50 archivos `.cs` nuevos o modificados en la rama).
   - Comando: `dotnet format Ascenso.Postulaciones.sln --verify-no-changes --no-restore --include <50 rutas relativas a Apps/services/ms-postulaciones/>`
   - Resultado: **Código de salida `0`**, salida limpia sin cambios pendientes ni archivos ajenos modificados.

5. **Validación de modelo EF Core y OpenSpec (`5.5`):**
   - Comando: `dotnet ef migrations has-pending-model-changes --project Ascenso.Postulaciones.Infrastructure --startup-project Ascenso.Postulaciones.Api --no-build`
   - Resultado: **`No changes have been made to the model since the last migration.`** (código de salida `0`).
   - **Confirmación explícita:** **No se creó ninguna migración EF Core vacía**, dado que HU 8.2 no modifica tablas, columnas, restricciones ni índices (las consultas de solo lectura reutilizan `ux_asignaciones_postulacion_evaluador_vigente` e `ix_evaluaciones_postulacion_estado`).
   - Comando: `openspec validate rf8-2-int2-mspos --strict` → **`Change 'rf8-2-int2-mspos' is valid`** (código `0`).
   - Comando: `openspec validate --specs` → **`Totals: 10 passed, 0 failed (10 items)`** (código `0`).

---

## 2. Lista exhaustiva de ajustes mínimos de constructores/DI en pruebas previas

Para incorporar `IConvocatoriaEvaluacionClient` en los handlers de escritura de HU 8.1 y aislar `IConvocatoriaEvaluacionClient` / `IConsultaEvaluacionesCoevaluadoresRepository` en el contenedor de pruebas sin alterar ninguna aserción de negocio previa, se aplicaron únicamente los siguientes ajustes mecánicos:

- **`tests/Ascenso.Postulaciones.Application.Tests/Evaluaciones/Comandos/GuardarBorradorEvaluacionCommandHandlerTests.cs`** (`+11` líneas): inyección de un sustituto NSubstitute por defecto de `IConvocatoriaEvaluacionClient` con `VentanaRevisionConvocatoria` abierta alrededor de `_ahora`.
- **`tests/Ascenso.Postulaciones.Application.Tests/Evaluaciones/Comandos/EnviarEvaluacionDefinitivaCommandHandlerTests.cs`** (`+11` líneas): inyección de un sustituto NSubstitute por defecto de `IConvocatoriaEvaluacionClient` con `VentanaRevisionConvocatoria` abierta alrededor de `_ahora`.
- **`tests/Ascenso.Postulaciones.IntegrationTests/Persistencia/EvaluacionPostgreSqlTests.cs`** (`+19` líneas): provisión del doble de `IConvocatoriaEvaluacionClient` con ventana abierta al instanciar los handlers contra PostgreSQL real.
- **`tests/Ascenso.Postulaciones.Application.Tests/Configuracion/InyeccionDependenciasTests.cs`** (`+11` líneas): registro de dobles requeridos y verificación de resolución DI de `ObtenerEvaluacionesCoevaluadoresPostulacionQueryHandler`, su validador y los handlers con `IConvocatoriaEvaluacionClient` e `IConsultaEvaluacionesCoevaluadoresRepository`.
- **`tests/Ascenso.Postulaciones.IntegrationTests/EnsambladoApiTests.cs`** (`+2` líneas): registro de los dobles mínimos para los nuevos puertos en la prueba de ensamblado de la API.
- **Fábricas `WebApplicationFactory` en suites de controladores y seguridad previas** (`+4` líneas en cada archivo para aislar `IConvocatoriaEvaluacionClient` / `IConsultaEvaluacionesCoevaluadoresRepository` en `ConfigureTestServices`, manteniendo intactas todas las aserciones previas):
  - `tests/Ascenso.Postulaciones.IntegrationTests/Controllers/AsignacionesEvaluacionControllerTests.cs`
  - `tests/Ascenso.Postulaciones.IntegrationTests/Controllers/ComitesEvaluadoresControllerTests.cs`
  - `tests/Ascenso.Postulaciones.IntegrationTests/Controllers/ExcepcionEvaluadorUnicoControllerTests.cs`
  - `tests/Ascenso.Postulaciones.IntegrationTests/Controllers/PostulantesControllerTests.cs`
  - `tests/Ascenso.Postulaciones.IntegrationTests/Controllers/RubricasControllerTests.cs`
  - `tests/Ascenso.Postulaciones.IntegrationTests/Seguridad/AutorizacionAsignacionesTests.cs`
  - `tests/Ascenso.Postulaciones.IntegrationTests/Seguridad/AutorizacionComitesTests.cs`
  - `tests/Ascenso.Postulaciones.IntegrationTests/Seguridad/AutorizacionExcepcionEvaluadorUnicoTests.cs`
  - `tests/Ascenso.Postulaciones.IntegrationTests/Seguridad/AutorizacionRubricasTests.cs`
  - *(Asimismo, `EvaluacionesControllerTests.cs`, `AutorizacionEvaluacionesTests.cs` y `SwaggerApiTests.cs` incorporaron el aislamiento DI y las nuevas pruebas específicas de HU 8.2).*

---

## 3. Preguntas abiertas heredadas de `design.md`

1. **Anonimato de pares tras el cierre:** el diseño actual omite `EvaluadorId` y `AsignacionEvaluacionId` en `EvaluacionCoevaluadorDto` para maximizar privacidad y evitar sondeo de rutas `/asignaciones/{id}` ajenas; confirmar si el comité requiere ver el identificador o nombre del coevaluador una vez cerrado el periodo de revisión.
2. **Token S2S (`convocatorias:leer`) entre `MS-POS` y `MS-PRG`:** coordinar con el equipo de plataforma (Integrante 1 / Keycloak) el `DelegatingHandler` de Client Credentials compartido para `PeriodoAcademicoClient`, `CatalogoAcademicoClient` y `ConvocatoriaEvaluacionClient`.
3. **Evaluador asignado que no envió su evaluación antes del cierre:** el diseño actual permite la lectura post-cierre a todo `MIEMBRO_COMITE` con asignación vigente (distinta de `REASIGNADA_POR_CONFLICTO`) porque la ventana de escritura ya cerró; confirmar si se desea exigir además que la propia evaluación del solicitante esté en `ENVIADA_DEFINITIVA` o que su membresía en el comité siga `Activo == true`.
4. **Visibilidad del `COORDINADOR_PROGRAMA` antes del cierre (heredada de HU 8.1):** confirmar si `GET /api/v1/evaluaciones/postulaciones/{postulacionId}` mantiene acceso inmediato a evaluaciones `ENVIADA_DEFINITIVA` para supervisión del coordinador o si en una iteración futura también deberá esperar al cierre del periodo de revisión.
5. **Cierre manual anticipado del periodo de revisión:** confirmar si en el futuro `MS-PRG` incorporará un cierre explícito independiente de `fechaCierreEvaluacion` (encapsulable sin ruptura dentro de `ConvocatoriaEvaluacionClient` y `VentanaRevisionConvocatoria`).
