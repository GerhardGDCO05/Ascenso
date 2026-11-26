# Bitácora de Ejecución: `rf7-2-int2-mspos` (2026-11-22)

- **Cambio OpenSpec:** `openspec/changes/rf7-2-int2-mspos`
- **Requerimiento:** `RF-07` — Configuración de rúbricas de evaluación (`HU 7.2`: bloqueo de rúbricas publicadas)
- **Microservicio:** `MS-POS` (`Apps/services/ms-postulaciones/`)
- **Integrante:** Integrante 2 (`int2`)

## Resumen de Implementación

1. **Dominio (`Ascenso.Postulaciones.Domain`):**
   - Enumeración `MotivoBloqueoRubrica` (`InicioEvaluacion` -> `INICIO_EVALUACION`, `PrimeraCalificacion` -> `PRIMERA_CALIFICACION`) con `CodigoEnum` en `Domain/Enums/MotivoBloqueoRubrica.cs`.
   - Actor de sistema `UsuarioSistema` (`Id` fijo no vacío) en `Domain/Comun/UsuarioSistema.cs`.
   - Excepción `RubricaNoPublicadaException` (`RUBRICA_NO_PUBLICADA`, `409`, `ExcepcionReglaNegocio`) y fábricas `Publicada()` y `Bloqueada(fecha, motivo)` en `RubricaBloqueadaException` (conservando el constructor existente).
   - Propiedades `MotivoBloqueo` y `FechaBloqueo` (`private set`, nulas fuera de `BLOQUEADA`) y método `bool Bloquear(motivo, fecha, usuarioId)` en `RubricaEvaluacion`: transición `PUBLICADA → BLOQUEADA` con incremento de `Version` y auditoría, idempotente en `BLOQUEADA` (`false`), rechazo en `BORRADOR` (`RubricaNoPublicadaException`); `AsegurarEstadoBorrador` actualizado con mensajes explicativos.

2. **Aplicación (`Ascenso.Postulaciones.Application`):**
   - `BloquearRubricaCommand(RubricaId, Motivo)` y `BloquearRubricaCommandHandler` (actor `UsuarioSistema`, `TimeProvider`, `CancellationToken`), con manejo de `ExcepcionConflictoConcurrencia` y relectura sin tracking para garantizar idempotencia bajo concurrencia.
   - `RubricaDto` y `RubricaMapeador` ampliados con `MotivoBloqueo`, `FechaBloqueo` y `SoloLectura` (`Estado != Borrador`).
   - Registro del handler en `Application/DependencyInjection.cs`.

3. **Infraestructura (`Ascenso.Postulaciones.Infrastructure`):**
   - `RubricaEvaluacionConfiguracion`: columnas `motivo_bloqueo` (`VARCHAR(30)`, nulable, `ComoCodigoEnum`) y `fecha_bloqueo` (`timestamptz`, nulable), más restricciones `CHECK` `ck_rubricas_motivo_bloqueo_valido` y `ck_rubricas_bloqueo_coherente`.
   - Migración `20261122153053_BloqueoRubrica` (`BloqueoRubrica.cs`, `BloqueoRubrica.Designer.cs` y actualización de `PostulacionesDbContextModelSnapshot.cs`).

4. **API (`Ascenso.Postulaciones.Api`):**
   - `PoliticasAutorizacionRubricas` (`RolMiembroComite`) y `Program.cs`: política `LecturaRubrica` habilitada para `COORDINADOR_PROGRAMA` y `MIEMBRO_COMITE` (solo lectura), manteniendo `CoordinadorPrograma` solo para `COORDINADOR_PROGRAMA` y sin endpoint HTTP de bloqueo.

## Evidencia Real de Verificación

- **Restore bloqueado:**
  - Comando: `dotnet restore Ascenso.Postulaciones.sln --locked-mode` (desde `Apps/services/ms-postulaciones`)
  - Resultado: `exit code 0`, 8 proyectos restaurados correctamente (`Domain`, `Application`, `Infrastructure`, `Api`, `Domain.Tests`, `Application.Tests`, `ArchitectureTests`, `IntegrationTests`).

- **Compilación Release:**
  - Comando: `dotnet build Ascenso.Postulaciones.sln --configuration Release` (desde `Apps/services/ms-postulaciones`)
  - Resultado: `Compilación correcta. 0 Advertencia(s), 0 Errores` (`00:00:06.58`).

- **Suite de pruebas (`dotnet test Ascenso.Postulaciones.sln`):**
  - Comando: `dotnet test Ascenso.Postulaciones.sln` (desde `Apps/services/ms-postulaciones`)
  - Resultados:
    - `Ascenso.Postulaciones.Domain.Tests`: **201/201** superadas (`0` errores, `0` omitidas, `80 ms`).
    - `Ascenso.Postulaciones.Application.Tests`: **152/152** superadas (`0` errores, `0` omitidas, `155 ms`).
    - `Ascenso.Postulaciones.ArchitectureTests`: **11/11** superadas (`0` errores, `0` omitidas, `456 ms`).
    - `Ascenso.Postulaciones.IntegrationTests`: **128/128** superadas (`0` errores, `0` omitidas, `4 s`; incluye pruebas de modelo, migración, API, autorización y PostgreSQL real con Testcontainers).
    - **Total: 492/492** superadas (`0` fallidas, `0` omitidas).

- **Formato (`--include` sobre los 34 archivos del cambio):**
  - Comando: `dotnet format Ascenso.Postulaciones.sln --verify-no-changes --no-restore --include <34 archivos nuevos o modificados del cambio>` (desde `Apps/services/ms-postulaciones`)
  - Resultado: `exit code 0`, salida limpia sin cambios pendientes.

- **Validación OpenSpec:**
  - Comando: `openspec validate rf7-2-int2-mspos --strict` (desde la raíz del repositorio)
  - Resultado: `Change 'rf7-2-int2-mspos' is valid`.
  - Comando: `openspec validate --specs` (desde la raíz del repositorio)
  - Resultado: `7 passed, 0 failed (7 items)`.

## Decisiones y Preguntas Abiertas (`design.md`)

1. **Disparador de inicio de evaluación (RF-02):** queda por definir qué componente, evento o API interna marca el inicio formal de la fase de evaluación del ciclo y cómo resuelve la rúbrica (por `rubricaId` o por programa y convocatoria).
2. **Disparador de primera calificación (HU 8.1):** queda por confirmar si el bloqueo ocurre en la misma transacción (`IUnitOfWork`) que la persistencia de la primera puntuación o antes.
3. **Alcance de lectura por comité (RF-06):** hoy cualquier `MIEMBRO_COMITE` lee cualquier rúbrica; queda pendiente confirmar si debe limitarse a los comités asignados al programa o convocatoria.
4. **Tipo de `fecha_bloqueo`:** se implementó `timestamptz` (alineado con `fecha_publicacion` para conservar el instante UTC con `DateTimeOffset`); queda por confirmar frente al `timestamp` indicado en el pedido.

## Seguimiento de Workflow

- Cambio listo para archivar en `openspec/changes/archive/2026-11-22-rf7-2-int2-mspos/` sincronizando el delta hacia `openspec/specs/ms-pos/rubricas/spec.md`.
- `RF-07` permanece en `[ ]` en `openspec/roadmaps/por_integrante.md` y `openspec/roadmaps/por_microservicio.md` hasta completar la `HU 7.3`.
