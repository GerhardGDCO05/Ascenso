# Bitácora de Ejecución: `rf7-1-int2-mspos` (2026-11-21)

- **Cambio OpenSpec:** `openspec/changes/rf7-1-int2-mspos`
- **Requerimiento:** `RF-07` — Configuración de rúbricas de evaluación (`HU 7.1`)
- **Microservicio:** `MS-POS` (`Apps/services/ms-postulaciones/`)
- **Integrante:** Integrante 2 (`int2`)

## Resumen de Implementación

1. **Dominio (`Ascenso.Postulaciones.Domain`):**
   - Enumeración `EstadoRubrica` (`BORRADOR`, `PUBLICADA`, `BLOQUEADA`) con `CodigoEnum`.
   - Entidad `CriterioEvaluacion : EntidadAuditable` y agregado `RubricaEvaluacion : AggregateRoot` con fábrica `Crear` (nace en `BORRADOR`), gestión de criterios (`AgregarCriterio`, `EditarCriterio`, `EliminarCriterio` sin renumerar `orden`), cálculo decimal `SumaPesos` y `DiferenciaPara100` (`100.00m - SumaPesos`), transición `Publicar` con validación exacta a `100.00` e inmutabilidad tras publicar o bloquear.
   - Excepciones de dominio: `RubricaEvaluacionInvalidaException` (400), `CriterioEvaluacionInvalidoException` (400), `PesosInvalidosException` (`PESOS_INVALIDOS`, 422), `RubricaBloqueadaException` (`RUBRICA_BLOQUEADA`, 409), `RubricaDuplicadaException` (`RUBRICA_DUPLICADA`, 409), `RubricaNoEncontradaException` (404) y `CriterioNoEncontradoException` (404).
   - Puerto `IRubricaRepository` en `Domain/Interfaces/`.

2. **Aplicación (`Ascenso.Postulaciones.Application`):**
   - Comandos CQRS-Lite con validadores FluentValidation y handlers: `CrearRubricaCommand`, `AgregarCriterioCommand`, `EditarCriterioCommand`, `EliminarCriterioCommand`, `PublicarRubricaCommand` y consulta `ObtenerRubricaQuery`.
   - DTOs `RubricaDto`, `CriterioEvaluacionDto` y `RubricaMapeador` (incluyendo `sumaPesos` y `diferenciaPara100`).
   - Registro en `Application/DependencyInjection.cs`.

3. **Infraestructura (`Ascenso.Postulaciones.Infrastructure`):**
   - Configuraciones EF Core `RubricaEvaluacionConfiguracion` y `CriterioEvaluacionConfiguracion` en esquema `postulaciones` (tablas `rubricas` y `criterios_evaluacion`, restricciones `CHECK`, FK en cascada e índices únicos `ux_rubricas_programa_convocatoria` y `ux_criterios_evaluacion_rubrica_orden`).
   - `RubricaRepository` con `Include` de criterios y tracking solo en consultas de actualización; traducción de conflictos de unicidad y concurrencia (`409`).
   - Migración `20261121225821_RubricasEvaluacion` y pruebas de integración contra PostgreSQL real con Testcontainers.

4. **API (`Ascenso.Postulaciones.Api`):**
   - Políticas de autorización `CoordinadorPrograma` y `LecturaRubrica` (`COORDINADOR_PROGRAMA` sobre claim `roles`) con `IAuthorizationMiddlewareResultHandler` que responde `403` `ProblemDetails` (`PERMISO_DENEGADO`) solo para esas políticas, sin alterar Postulantes.
   - `RubricasController` v1 (`/api/v1/rubricas`): `POST /` (201), `GET /{id}` (200), `POST /{id}/criterios` (201), `PUT /{id}/criterios/{criterioId}` (200), `DELETE /{id}/criterios/{criterioId}` (200) y `POST /{id}/publicar` (200), documentados en Swagger.

## Evidencia Real de Verificación

- **Restore bloqueado:**
  - Comando: `dotnet restore Ascenso.Postulaciones.sln --locked-mode` (desde `Apps/services/ms-postulaciones`)
  - Resultado: `exit code 0`, 8 proyectos restaurados correctamente (`Domain`, `Application`, `Infrastructure`, `Api`, `Domain.Tests`, `Application.Tests`, `ArchitectureTests`, `IntegrationTests`).

- **Compilación Release:**
  - Comando: `dotnet build Ascenso.Postulaciones.sln --configuration Release` (desde `Apps/services/ms-postulaciones`)
  - Resultado: `Compilación correcta. 0 Advertencia(s), 0 Errores` (`00:00:06.40`).

- **Suite de pruebas (`dotnet test Ascenso.Postulaciones.sln`):**
  - Comando: `dotnet test Ascenso.Postulaciones.sln` (desde `Apps/services/ms-postulaciones`)
  - Resultados:
    - `Ascenso.Postulaciones.Domain.Tests`: **184/184** superadas (`0` errores, `0` omitidas, `46 ms`).
    - `Ascenso.Postulaciones.Application.Tests`: **141/141** superadas (`0` errores, `0` omitidas, `158 ms`).
    - `Ascenso.Postulaciones.ArchitectureTests`: **11/11** superadas (`0` errores, `0` omitidas, `504 ms`).
    - `Ascenso.Postulaciones.IntegrationTests`: **119/119** superadas (`0` errores, `0` omitidas, `8 s`; incluye pruebas de modelo, migración, API y PostgreSQL real con Testcontainers).
    - **Total: 455/455** superadas (`0` fallidas, `0` omitidas).

- **Formato:**
  - Verificación inicial: `dotnet format Ascenso.Postulaciones.sln --verify-no-changes` detectó formato pendiente (`ENDOFLINE`, `CHARSET`, `IMPORTS` e `IDE1006` en `20261121225821_RubricasEvaluacion.cs`).
  - Corrección: se ejecutó `dotnet format Ascenso.Postulaciones.sln --no-restore` y se renombraron los campos privados estáticos `_columnasIndiceCriteriosRubricaOrden` y `_columnasIndiceRubricasProgramaConvocatoria` en `20261121225821_RubricasEvaluacion.cs`.
  - Re-verificación: `dotnet format Ascenso.Postulaciones.sln --verify-no-changes` -> `exit code 0`, sin cambios pendientes.

- **Validación OpenSpec:**
  - Comando: `openspec validate rf7-1-int2-mspos --strict` (desde la raíz del repositorio)
  - Resultado: `Change 'rf7-1-int2-mspos' is valid`.
  - Comando: `openspec validate --specs` (desde la raíz del repositorio)
  - Resultado: `6 passed, 0 failed (6 items)`.

## Deuda Técnica Registrada

- **Validación externa de `programaBecaId` y `convocatoriaId`:** actualmente solo se valida que sean `Guid` no vacíos y únicos por combinación; la validación contra `MS-PRG` y convocatorias queda pendiente para integración posterior.
- **Alcance por programa del coordinador:** hoy se valida el rol `COORDINADOR_PROGRAMA` en el borde HTTP; la pertenencia al programa desde `MS-PLT` queda pendiente.
- **Consultas adicionales y HUs restantes de `RF-07`:** consulta por `(programaBecaId, convocatoriaId)`, listado paginado, bloqueo (`HU 7.2`) y clonación (`HU 7.3`) quedan fuera de esta HU.

## Seguimiento de Workflow

- Archivar el cambio cuando la revisión esté aprobada y sincronizar el delta hacia `openspec/specs/ms-pos/rubricas/spec.md`.
- Mantener `RF-07` sin marcar como completo en los roadmaps hasta cubrir las HU 7.2 y 7.3.
