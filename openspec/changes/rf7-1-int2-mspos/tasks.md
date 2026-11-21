# Tasks: Rúbrica de Evaluación — Criterios y Pesos (`rf7-1-int2-mspos`)

> Alcance: solo HU 7.1 (RF-07). Estilo TDD: cada tarea de implementación incluye su prueba y su verificación con `dotnet test`; ninguna tarea se marca `[x]` sin pruebas en verde. Comando base: `dotnet test "Apps/services/ms-postulaciones/Ascenso.Postulaciones.sln"` (o el proyecto de pruebas correspondiente con `--filter`). Todo método asíncrono recibe y propaga `CancellationToken`. No se modifica el módulo Postulantes, `spec_pos.md` ni otros microservicios.

## 1. Dominio

- [x] 1.1 Crear `EstadoRubrica` (`BORRADOR`, `PUBLICADA`, `BLOQUEADA`) en `Domain/Enums/` con `CodigoEnum`; escribir primero la prueba de serialización de códigos y verificar con `dotnet test` de `Domain.Tests`.
- [x] 1.2 Crear las excepciones `RubricaEvaluacionInvalidaException` (400), `CriterioEvaluacionInvalidoException` (400), `PesosInvalidosException` (`PESOS_INVALIDOS`, 422, con mensaje «faltan X» si la suma es menor que `100.00` y «excede por X» si es mayor), `RubricaBloqueadaException` (`RUBRICA_BLOQUEADA`, 409), `RubricaDuplicadaException` (409), `RubricaNoEncontradaException` (404) y `CriterioNoEncontradoException` (404); probar `Codigo`, `EstadoHttp` y ambos mensajes de `PesosInvalidosException` y verificar con `dotnet test`.
- [x] 1.3 Implementar `CriterioEvaluacion : EntidadAuditable` en `Domain/Rubricas/Entidades/` (fábrica `Crear`, `Actualizar` es `internal`, constructor privado para EF, sin setters públicos); probar creación válida, rechazo de `nombre` vacío o mayor a 200 caracteres, `descripcion` vacía o mayor a 1000 caracteres, peso `0`, negativo, `> 100` o con más de 2 decimales, `puntajeMinimo` y `puntajeMaximo` no negativos (`>= 0.00`), con máximo 2 decimales, máximo `1000.00`, y `puntajeMinimo >= puntajeMaximo`; verificar con `dotnet test`.
- [x] 1.4 Implementar `RubricaEvaluacion : AggregateRoot` en `Domain/Rubricas/Agregados/` con `Crear` (nace en `BORRADOR`, valida identificadores y `nombre` obligatorio con máximo 200 caracteres), colección de solo lectura `Criterios` y `AgregarCriterio` (asigna `orden = máximo + 1`); probar nacimiento en `BORRADOR`, rechazo por nombre vacío o mayor a 200 caracteres y agregado de criterios; verificar con `dotnet test`.
- [x] 1.5 Implementar `EditarCriterio` y `EliminarCriterio` en el agregado (criterio inexistente → `CriterioNoEncontradoException`); probar edición, eliminación y rechazos; verificar con `dotnet test`.
- [x] 1.6 Implementar `SumaPesos` y `DiferenciaPara100` (`100.00m - SumaPesos`, positivo cuando faltan puntos y negativo cuando sobran) con `decimal`; probar suma con decimales (`33.33 + 33.33 + 33.34 = 100.00`), suma vacía (`0.00` / diferencia `100.00`), diferencia positiva por faltante (`9.50` con suma `90.50`) y diferencia negativa por exceso (`-0.01` con suma `100.01`); verificar con `dotnet test`.
- [x] 1.7 Implementar `Publicar`: con suma `100.00` pasa a `PUBLICADA` y registra `FechaPublicacion`; con faltante (`99.99`, mensaje con «faltan 0.01» y `DiferenciaPara100 = 0.01`), con exceso (`100.01`, mensaje con «excede por 0.01» y `DiferenciaPara100 = -0.01`) o sin criterios (`0.00`, «faltan 100.00») lanza `PESOS_INVALIDOS` (422); agregar una prueba para cada caso y verificar con `dotnet test`.
- [x] 1.8 Implementar la protección de inmutabilidad: agregar, editar, eliminar o publicar en una rúbrica `PUBLICADA` o `BLOQUEADA` lanza `RUBRICA_BLOQUEADA` (409); probar los cuatro caminos en ambos estados (la rúbrica `BLOQUEADA` se crea con un ayudante de reflexión dentro de `Domain.Tests`; prohibido añadir métodos públicos de producción para bloquear); verificar con `dotnet test`.
- [x] 1.9 Declarar el puerto `IRubricaRepository` en `Domain/Interfaces/` (`ObtenerPorIdAsync`, `ObtenerParaActualizarPorIdAsync`, `ExistePorProgramaYConvocatoriaAsync`, `AgregarAsync`); verificar que `Domain` compila y que `ArchitectureTests` siguen en verde.

## 2. Aplicación

- [ ] 2.1 Crear `RubricaDto`, `CriterioEvaluacionDto` y `RubricaMapeador` en `Application/Rubricas/DTOs/` (incluye `sumaPesos` y `diferenciaPara100`); probar el mapeo en `Application.Tests` y verificar con `dotnet test`.
- [ ] 2.2 Implementar `CrearRubricaCommand`, validador (`nombre` obligatorio y máximo 200 caracteres, identificadores no vacíos) y handler (usuario autenticado, duplicado → `409 RUBRICA_DUPLICADA`, `TimeProvider`, `IUnitOfWork`); con repositorio simulado (NSubstitute) probar éxito, validación inválida y duplicado; probar el validador; verificar con `dotnet test`.
- [ ] 2.3 Implementar `AgregarCriterioCommand`, validador y handler; probar éxito, rúbrica inexistente, `RUBRICA_BLOQUEADA` y validador (`nombre` vacío o > 200 caracteres, `descripcion` vacía o > 1000 caracteres, peso 0/negativo/`>100`/3 decimales, rango de puntaje inválido: negativo, > 1000.00, > 2 decimales o puntajeMinimo >= puntajeMaximo); verificar con `dotnet test`.
- [ ] 2.4 Implementar `EditarCriterioCommand`, validador y handler; probar éxito, rúbrica inexistente, criterio inexistente, `RUBRICA_BLOQUEADA` y validador (`nombre` vacío o > 200 caracteres, `descripcion` vacía o > 1000 caracteres, peso inválido y rango de puntaje inválido: negativo, > 1000.00, > 2 decimales o puntajeMinimo >= puntajeMaximo); verificar con `dotnet test`.
- [ ] 2.5 Implementar `EliminarCriterioCommand` y handler; probar éxito, rúbrica inexistente, criterio inexistente y `RUBRICA_BLOQUEADA`; verificar con `dotnet test`.
- [ ] 2.6 Implementar `PublicarRubricaCommand` y handler; probar éxito, rúbrica inexistente, `PESOS_INVALIDOS` y `RUBRICA_BLOQUEADA`, comprobando que no se guarda ante error; verificar con `dotnet test`.
- [ ] 2.7 Implementar `ObtenerRubricaQuery` y handler (solo lectura); probar éxito con suma y diferencia, y rúbrica inexistente; verificar con `dotnet test`.
- [ ] 2.8 Registrar los handlers en `Application/DependencyInjection.cs`; probar en `InyeccionDependenciasTests` que handlers y validadores se resuelven; verificar con `dotnet test` y que `ArchitectureTests` siguen en verde.

## 3. Persistencia

- [ ] 3.1 Crear `RubricaEvaluacionConfiguracion` (tabla `rubricas`, esquema `postulaciones`, `nombre` obligatorio `VARCHAR(200)`, estado `VARCHAR`, `CHECK` de estado, índice único `ux_rubricas_programa_convocatoria`, campo de respaldo `_criterios`); verificar con pruebas de modelo en `IntegrationTests` (sin PostgreSQL real) y `dotnet test`.
- [ ] 3.2 Crear `CriterioEvaluacionConfiguracion` (tabla `criterios_evaluacion`, FK con cascada, `nombre` obligatorio `VARCHAR(200)`, `descripcion` obligatoria `VARCHAR(1000)`, `peso_porcentual numeric(5,2)`, `puntaje_minimo` y `puntaje_maximo numeric(7,2)`, `CHECK` de peso en `(0, 100]`, `CHECK` `ck_criterios_evaluacion_puntaje_rango` (`puntaje_minimo >= 0 AND puntaje_maximo <= 1000 AND puntaje_minimo < puntaje_maximo`), `CHECK` de orden, índice único `ux_criterios_evaluacion_rubrica_orden`); verificar con pruebas de modelo y `dotnet test`.
- [ ] 3.3 Agregar `DbSet<RubricaEvaluacion>`/`DbSet<CriterioEvaluacion>` a `PostulacionesDbContext`, implementar `RubricaRepository` (`Include` de criterios; tracking solo en la consulta de actualización) y registrarlo en `Infrastructure/DependencyInjection.cs`; verificar con pruebas de repositorio o de modelo y `dotnet test`.
- [ ] 3.4 Verificar la traducción de violaciones de índice único y de concurrencia optimista (`409`) con `TraductorExcepcionesPersistencia`; probar y verificar con `dotnet test`.
- [ ] 3.5 **Migración (tarea separada):** generar la migración `RubricasEvaluacion` con `dotnet-ef`; verificar con `dotnet ef migrations has-pending-model-changes`, con una prueba de que el script contiene tablas, `CHECK`, FK e índices únicos, y con `dotnet test`.
- [ ] 3.6 Agregar pruebas de integración contra PostgreSQL real con Testcontainers (patrón `PostgreSqlFixture` de `ms-becas`), ejecutando antes la migración de 3.5 o aplicándola en el fixture: crear rúbrica, agregar criterios a una rúbrica ya persistida, eliminar criterio, publicar y recargar; verificar las violaciones de `ux_rubricas_programa_convocatoria` y `ux_criterios_evaluacion_rubrica_orden`; verificar con `dotnet test`.

## 4. API

- [ ] 4.1 Registrar la política de escritura `CoordinadorPrograma` y la política de lectura independiente `LecturaRubrica` (ambas por ahora con rol `COORDINADOR_PROGRAMA`, claim `roles`) y el `IAuthorizationMiddlewareResultHandler` que responde `403` con `ProblemDetails` y `codigo = PERMISO_DENEGADO` solo para esas políticas; probar con endpoint de prueba que Postulantes conserva su comportamiento y verificar con `dotnet test`.
- [ ] 4.2 Crear `RubricasController` v1 con `POST /api/v1/rubricas` (201, `[Authorize(Policy = "CoordinadorPrograma")]`) y `GET /api/v1/rubricas/{id}` (200, `[Authorize(Policy = "LecturaRubrica")]`); probar con esquema de autenticación de prueba: 201, 200, 400, 401, 403 (`ADMIN_FINANCIERO` y `MIEMBRO_COMITE`, `PERMISO_DENEGADO`), 404 y 409; verificar con `dotnet test`.
- [ ] 4.3 Agregar `POST /{id}/criterios` (201), `PUT /{id}/criterios/{criterioId}` (200) y `DELETE /{id}/criterios/{criterioId}` (200), cada acción con `[Authorize(Policy = "CoordinadorPrograma")]`; probar 201/200, 400, 401, 403 (`ADMIN_FINANCIERO` y `MIEMBRO_COMITE`), 404 y 409 `RUBRICA_BLOQUEADA`; verificar con `dotnet test`.
- [ ] 4.4 Agregar `POST /{id}/publicar` (200) con `[Authorize(Policy = "CoordinadorPrograma")]`; probar 200, 401, 403 (`ADMIN_FINANCIERO` y `MIEMBRO_COMITE`), 404, 409 `RUBRICA_BLOQUEADA` y 422 `PESOS_INVALIDOS`; verificar con `dotnet test`.
- [ ] 4.5 Verificar que Swagger documenta los endpoints y respuestas (`/swagger/v1/swagger.json`); ampliar `SwaggerApiTests` y verificar con `dotnet test`.

## 5. Verificación integral y seguimiento

- [ ] 5.1 Ejecutar `dotnet build "Apps/services/ms-postulaciones/Ascenso.Postulaciones.sln" --configuration Release` y `dotnet test` de la misma solución, verificando `0` advertencias, `ArchitectureTests` en verde sin cambios y todas las pruebas en verde.
- [ ] 5.2 Ejecutar `dotnet format "Apps/services/ms-postulaciones/Ascenso.Postulaciones.sln" --verify-no-changes --no-restore` y `dotnet restore --locked-mode`, verificando salida limpia.
- [ ] 5.3 Ejecutar `openspec validate rf7-1-int2-mspos --strict` y `openspec validate --specs`, verificando que no hay hallazgos.
- [ ] 5.4 Actualizar `openspec/roadmaps/por_integrante.md` y `openspec/roadmaps/por_microservicio.md` marcando solo la HU 7.1, sin marcar `RF-07` como completo.
- [ ] 5.5 **Tarea final:** redactar la bitácora `openspec/changes/rf7-1-int2-mspos/log-2026-11-21-rf7-1-int2-mspos.md` con la evidencia real de comandos y resultados.

## Workflow follow-up

- Archivar el cambio cuando la revisión del equipo esté aprobada y todas las tareas estén en `[x]`.
- Sincronizar el delta hacia `openspec/specs/ms-pos/rubricas/spec.md` al archivar.
- Mantener `RF-07` sin marcar como completo hasta cubrir las HU 7.2 y 7.3.
