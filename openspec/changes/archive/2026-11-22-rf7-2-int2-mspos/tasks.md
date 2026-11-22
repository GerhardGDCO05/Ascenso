# Tasks

> Todas las rutas son relativas a `Apps/services/ms-postulaciones/` salvo indicación. TDD: se escribe primero la prueba en rojo. Ninguna tarea se marca `[x]` sin `dotnet test` de la solución en verde. Sin Hangfire, Outbox ni MediatR; sin tocar el módulo `Postulantes` ni otros microservicios; sin endpoint HTTP de bloqueo.

## 1. Dominio

- [x] 1.1 Crear el enum `MotivoBloqueoRubrica` (`InicioEvaluacion`, `PrimeraCalificacion`; códigos `INICIO_EVALUACION` y `PRIMERA_CALIFICACION` con `CodigoEnum`) en `Domain/Enums/` y `UsuarioSistema` (constante `Id`, `Guid` fijo no vacío) en `Domain/Comun/`; probar en `Domain.Tests` (`MotivoBloqueoRubricaTests` con los códigos y `UsuarioSistemaTests` con id no vacío) y verificar con `dotnet test`.
- [x] 1.2 Crear `RubricaNoPublicadaException` (`RUBRICA_NO_PUBLICADA`, `409`, `ExcepcionReglaNegocio`) y agregar a `RubricaBloqueadaException` las fábricas `Publicada()` y `Bloqueada(fecha, motivo)` conservando el constructor actual; probar en `ExcepcionesRubricaTests` el código, el `409`, el mensaje de «publicada» y el mensaje «bloqueada desde {fecha} UTC por {motivo legible}» con fecha y motivo; verificar con `dotnet test`.
- [x] 1.3 Agregar a `RubricaEvaluacion` las propiedades `MotivoBloqueo` y `FechaBloqueo` (`private set`, nulas) y el método `bool Bloquear(motivo, fecha, usuarioId)`; probar en `RubricaEvaluacionTests`: `PUBLICADA → BLOQUEADA` (motivo, fecha UTC, `Version` +1, usuario de auditoría), idempotencia desde `BLOQUEADA` (retorna `false`, sin cambios ni excepción, ni con otro motivo), rechazo desde `BORRADOR` (`RubricaNoPublicadaException`, estado intacto), motivo no definido, fecha por defecto y usuario vacío; verificar con `dotnet test`.
- [x] 1.4 Hacer que `AsegurarEstadoBorrador` lance `RubricaBloqueadaException.Publicada()` o `.Bloqueada(...)` según el estado; probar la inmutabilidad en `BLOQUEADA` y `PUBLICADA` (agregar, editar, eliminar criterio y `Publicar` rechazados con el mensaje correcto, rúbrica sin cambios); migrar las pruebas de 7.1 que creaban `BLOQUEADA` por reflexión para usar `Bloquear`, conservando todas las pruebas de inmutabilidad; verificar con `dotnet test`.

## 2. Aplicación

- [x] 2.1 Agregar `MotivoBloqueo` (`string?`, código), `FechaBloqueo` y `SoloLectura` a `RubricaDto` y llenarlos en `RubricaMapeador`; probar en `RubricaMapeadorTests` los tres estados (`BORRADOR`: `soloLectura = false` y nulos; `PUBLICADA`: `true` y nulos; `BLOQUEADA`: `true`, motivo y fecha) y ajustar las pruebas existentes que construyen el DTO; verificar con `dotnet test`.
- [x] 2.2 Crear `BloquearRubricaCommand(RubricaId, Motivo)` y `BloquearRubricaCommandHandler` (flujo de `design.md` D4, actor `UsuarioSistema`, `TimeProvider`, `CancellationToken`); probar en `Application.Tests` (`BloquearRubricaCommandHandlerTests`): éxito (guarda una vez y devuelve DTO bloqueado), rúbrica inexistente (`RubricaNoEncontradaException`, sin guardar), ya bloqueada (idempotente, sin llamar a `GuardarCambiosAsync`), desde `BORRADOR` (`RubricaNoPublicadaException`, sin llamar a `GuardarCambiosAsync`), concurrencia simulada (`GuardarCambiosAsync` lanza `ExcepcionConflictoConcurrencia`, la relectura devuelve `BLOQUEADA` → éxito sin error), conflicto con relectura no bloqueada (se propaga) y que ante cualquier error no se llama a `GuardarCambiosAsync` de más; verificar con `dotnet test`.
- [x] 2.3 Registrar el handler en `Application/DependencyInjection.cs`; probar en `InyeccionDependenciasTests` que se resuelve y verificar con `dotnet test`.

## 3. Persistencia (modelo EF Core)

- [x] 3.1 Configurar en `RubricaEvaluacionConfiguracion` las columnas `motivo_bloqueo` (`VARCHAR(30)`, nulable, `ComoCodigoEnum`) y `fecha_bloqueo` (`timestamptz`, nulable) y los `CHECK` `ck_rubricas_motivo_bloqueo_valido` y `ck_rubricas_bloqueo_coherente` de `design.md` D8; probar en `IntegrationTests` (pruebas de modelo, sin PostgreSQL real) columnas, tipos, nulabilidad y definición de ambos `CHECK`, y que `RubricaRepository` conserva y recarga el bloqueo; verificar con `dotnet test`.

## 4. API

- [x] 4.1 Agregar `RolMiembroComite` a `PoliticasAutorizacionRubricas` y registrar en `Program.cs` `LecturaRubrica` con `COORDINADOR_PROGRAMA` y `MIEMBRO_COMITE`, manteniendo `CoordinadorPrograma` solo con `COORDINADOR_PROGRAMA`; actualizar `AutorizacionRubricasTests` (el caso de lectura de `MIEMBRO_COMITE` pasa de `403` a `200`; su escritura sigue en `403` con `PERMISO_DENEGADO`; `ADMIN_FINANCIERO` sigue en `403` en lectura y escritura; Postulantes sin cambios); verificar con `dotnet test`.
- [x] 4.2 Probar en `RubricasControllerTests` que `GET /api/v1/rubricas/{id}` devuelve `soloLectura`, `motivoBloqueo` y `fechaBloqueo` para rúbricas en `BORRADOR`, `PUBLICADA` y `BLOQUEADA`, que un `MIEMBRO_COMITE` lee (`200`) una rúbrica bloqueada y no escribe (`403` en `POST /` y en las rutas de criterios y publicar), y que las modificaciones sobre una rúbrica bloqueada devuelven `409 RUBRICA_BLOQUEADA` con el mensaje de fecha y motivo (criterio 3); confirmar que no existe ninguna ruta de bloqueo y ampliar `SwaggerApiTests` con los campos nuevos del DTO; verificar con `dotnet test`.

## 5. Migración y PostgreSQL real

- [x] 5.1 **Migración (tarea separada):** generar con `dotnet-ef` la migración `BloqueoRubrica` (columnas, `CHECK`, actualización del `ModelSnapshot`); verificar con `dotnet ef migrations has-pending-model-changes`, con una prueba (patrón `MigracionRubricasEvaluacionTests`) de que el script contiene las columnas y ambos `CHECK`, y con `dotnet test`; luego ejecutar `dotnet format Ascenso.Postulaciones.sln --no-restore --include <migración, Designer, snapshot y demás archivos nuevos o modificados hasta aquí>` (nunca sin `--include`) y verificar que `git diff --stat` no muestra archivos previos ajenos al cambio.
- [x] 5.2 Agregar a `RubricaPostgreSqlTests` (Testcontainers, `PostgreSqlFixture` con migraciones aplicadas): bloqueo persistido y recargado; violación de `ck_rubricas_bloqueo_coherente` (`BLOQUEADA` sin motivo o fecha; `BORRADOR` o `PUBLICADA` con ellos) y de `ck_rubricas_motivo_bloqueo_valido`; y **dos bloqueos concurrentes** con dos `DbContext` y handlers que leen antes de que cualquiera confirme (barrera): ambos terminan sin error, la fila queda `BLOQUEADA` con un solo motivo y fecha y `Version` incrementada una sola vez; verificar con `dotnet test`.

## 6. Verificación integral y seguimiento

> Archivos nuevos o modificados para `--include` (completar con los realmente tocados, incluida la migración con su marca de tiempo): `Ascenso.Postulaciones.Domain/Enums/MotivoBloqueoRubrica.cs`, `Ascenso.Postulaciones.Domain/Comun/UsuarioSistema.cs`, `Ascenso.Postulaciones.Domain/Excepciones/RubricaNoPublicadaException.cs`, `Ascenso.Postulaciones.Domain/Excepciones/RubricaBloqueadaException.cs`, `Ascenso.Postulaciones.Domain/Rubricas/Agregados/RubricaEvaluacion.cs`, `Ascenso.Postulaciones.Application/Rubricas/Comandos/BloquearRubrica/BloquearRubricaCommand.cs`, `Ascenso.Postulaciones.Application/Rubricas/Comandos/BloquearRubrica/BloquearRubricaCommandHandler.cs`, `Ascenso.Postulaciones.Application/Rubricas/DTOs/RubricaDto.cs`, `Ascenso.Postulaciones.Application/Rubricas/DTOs/RubricaMapeador.cs`, `Ascenso.Postulaciones.Application/DependencyInjection.cs`, `Ascenso.Postulaciones.Infrastructure/Persistence/Configuraciones/RubricaEvaluacionConfiguracion.cs`, `Ascenso.Postulaciones.Infrastructure/Persistence/Migrations/*_BloqueoRubrica.cs`, `*_BloqueoRubrica.Designer.cs`, `PostulacionesDbContextModelSnapshot.cs`, `Ascenso.Postulaciones.Api/Seguridad/PoliticasAutorizacionRubricas.cs`, `Ascenso.Postulaciones.Api/Program.cs` y los archivos de prueba nuevos o modificados bajo `tests/`.

- [x] 6.1 Ejecutar `dotnet restore Ascenso.Postulaciones.sln --locked-mode` (desde `Apps/services/ms-postulaciones`) y verificar salida sin errores.
- [x] 6.2 Ejecutar `dotnet build Ascenso.Postulaciones.sln --configuration Release` y verificar `0` advertencias y `0` errores.
- [x] 6.3 Ejecutar `dotnet test Ascenso.Postulaciones.sln` completo y verificar todas las pruebas en verde, incluidas `ArchitectureTests` sin cambios y las de Testcontainers.
- [x] 6.4 Ejecutar `dotnet format Ascenso.Postulaciones.sln --verify-no-changes --no-restore --include <rutas-de-archivos-nuevos-o-modificados>` (nunca sin `--include`) y verificar salida limpia.
- [x] 6.5 Ejecutar `openspec validate rf7-2-int2-mspos --strict` y `openspec validate --specs` y verificar que no hay hallazgos.
- [x] 6.6 Actualizar `openspec/roadmaps/por_integrante.md` y `openspec/roadmaps/por_microservicio.md` marcando únicamente `HU 7.2` anidada bajo `RF-07` (junto a `HU 7.1`) y manteniendo `RF-07` en `[ ]` hasta completar la HU 7.3; verificar leyendo ambos archivos.
- [x] 6.7 **Tarea final:** redactar la bitácora `openspec/changes/rf7-2-int2-mspos/log-2026-11-22-rf7-2-int2-mspos.md` con la evidencia real de comandos y resultados (restore, build, test, format, validación OpenSpec) y las decisiones abiertas de `design.md`.

## Workflow follow-up

- Archivar el cambio cuando la revisión del equipo esté aprobada y todas las tareas estén en `[x]` (requiere `.openspec.yaml`, `proposal.md`, `design.md`, `tasks.md` y la bitácora).
- Sincronizar el delta hacia `openspec/specs/ms-pos/rubricas/spec.md` al archivar.
- Mantener `RF-07` sin marcar como completo hasta cubrir la HU 7.3.
- Coordinar con RF-02 y HU 8.1 los disparadores descritos en `design.md` D9.
