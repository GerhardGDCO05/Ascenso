## Context

`MS-POS` ya implementa el módulo Postulantes (HU 1.1 y 1.2) con arquitectura hexagonal: `Postulante` (agregado) con `MiembroFamiliar` (entidad hija), handlers CQRS-Lite sin MediatR, validadores FluentValidation, `IUsuarioActual`, `TimeProvider`, `ManejadorExcepcionesDominio` y persistencia EF Core en el esquema `postulaciones`. La HU 7.1 (RF-07) agrega el agregado `RubricaEvaluacion` (DDD V7, sección 2.3) y las reglas `RN-POS-05` (suma exacta 100.00, `422 PESOS_INVALIDOS`; bloqueo `409 RUBRICA_BLOQUEADA`) y `RN-PLT-02` (`ADMIN_FINANCIERO` jamás crea, edita ni publica: `403 PERMISO_DENEGADO`).

Estado actual observado:

- `IUsuarioActual` expone solo `EstaAutenticado` y `UsuarioId`; el JWT mapea `RoleClaimType = "roles"` y `MapInboundClaims = false`.
- `ManejadorExcepcionesDominio` traduce `DomainException` a `ProblemDetails` con extensión `codigo`; `AddAuthorization()` no define políticas.
- `MiembroFamiliar` hereda de `EntidadAuditable`, tiene constructor privado para EF, fábrica `Crear` y se persiste con FK y cascada.

## Goals / Non-Goals

**Goals:**

- Crear la rúbrica en `BORRADOR`, gestionar criterios, calcular la suma de pesos, publicar con `100.00` exacto y rechazar mutaciones tras publicar.
- Restringir las operaciones al rol `COORDINADOR_PROGRAMA` con `403 PERMISO_DENEGADO` como `ProblemDetails`.
- Reutilizar el núcleo existente sin crear núcleo nuevo ni tocar Postulantes.

**Non-Goals:**

- Bloqueo (HU 7.2), clonación (HU 7.3), `BLOQUEADA` como destino de transición, eventos de integración, Hangfire, Outbox, MediatR.
- Validar programa/convocatoria contra otros microservicios.

## Decisions

### D1. Agregado y entidad hija (patrón `Postulante` → `MiembroFamiliar`)

`RubricaEvaluacion : AggregateRoot` (que ya extiende `EntidadAuditable`) en `Domain/Rubricas/Agregados/`; `CriterioEvaluacion : EntidadAuditable` en `Domain/Rubricas/Entidades/`, con `RubricaEvaluacionId`, constructor privado para EF, fábrica `Crear` y método de dominio `Actualizar` (`Actualizar` es `internal`). Colección privada `_criterios` expuesta como `IReadOnlyCollection<CriterioEvaluacion>`; ninguna propiedad con setter público. Todas las mutaciones del agregado reciben `DateTimeOffset` y `usuarioId` (el dominio no usa reloj propio; el handler pasa `TimeProvider.GetUtcNow()`).

Operaciones del agregado: `Crear`, `AgregarCriterio`, `EditarCriterio`, `EliminarCriterio`, `Publicar`, y propiedades calculadas `SumaPesos` y `DiferenciaPara100`.

### D2. Validación y precisión de pesos y puntajes

- `nombre` (de la rúbrica y del criterio): obligatorio (no vacío tras `Trim`) y con longitud máxima de `200` caracteres.
- `descripcion` del criterio: obligatoria (no vacía tras `Trim`) y con longitud máxima de `1000` caracteres.
- `pesoPorcentual`: `decimal`, `> 0` y `<= 100`, máximo 2 decimales (se rechaza, no se redondea: `decimal.Round(valor, 2) != valor`).
- `puntajeMinimo` y `puntajeMaximo`: `decimal`, no negativos (`>= 0.00`), con máximo 2 decimales (se rechaza si tiene más de 2, no se redondea) y máximo `1000.00`, manteniendo `puntajeMinimo < puntajeMaximo`.
- La suma se calcula con `decimal`, sin conversiones a `double`. Publicación válida solo si `SumaPesos == 100.00m`. `DiferenciaPara100` (`100.00m - SumaPesos`) es positivo cuando faltan puntos (suma `< 100.00`) y negativo cuando sobran (suma `> 100.00`). Sin criterios, la suma es `0.00` y `DiferenciaPara100` es `100.00`.
- La validación se realiza dos veces: FluentValidation en Application (`400`) y guardas en el dominio (`RubricaEvaluacionInvalidaException` / `CriterioEvaluacionInvalidoException`, `400`), como en `MiembroFamiliar`.
- `PesosInvalidosException` (`PESOS_INVALIDOS`, `422`) incluye la diferencia en el mensaje: dice «faltan X» si la suma es menor que `100.00` (p. ej. «La suma de pesos es 99.99; faltan 0.01 para 100.00») y «excede por X» si la suma es mayor que `100.00` (p. ej. «La suma de pesos es 100.01; excede por 0.01 a 100.00»); además `DiferenciaPara100` está en el DTO de consulta. No se modifica el contrato del manejador de excepciones.

### D3. Estados

`EstadoRubrica` (`BORRADOR`, `PUBLICADA`, `BLOQUEADA`) con `CodigoEnum`, persistido como `VARCHAR` en mayúsculas. Este cambio solo transiciona `BORRADOR → PUBLICADA` y registra `FechaPublicacion`. Cualquier mutación o publicación cuando el estado no es `BORRADOR` lanza `RubricaBloqueadaException` (`RUBRICA_BLOQUEADA`, `409`), lo que ya cubre `BLOQUEADA` para la HU 7.2.

### D4. Excepciones de dominio (extienden `ExcepcionReglaNegocio`)

| Excepción | Código | HTTP |
|---|---|---|
| `RubricaEvaluacionInvalidaException` | `RUBRICA_INVALIDA` | 400 |
| `CriterioEvaluacionInvalidoException` | `CRITERIO_INVALIDO` | 400 |
| `PesosInvalidosException` | `PESOS_INVALIDOS` | 422 |
| `RubricaBloqueadaException` | `RUBRICA_BLOQUEADA` | 409 |
| `RubricaDuplicadaException` | `RUBRICA_DUPLICADA` | 409 |
| `RubricaNoEncontradaException` | `RUBRICA_NO_ENCONTRADA` | 404 |
| `CriterioNoEncontradoException` | `CRITERIO_NO_ENCONTRADO` | 404 |

### D5. Aplicación (CQRS-Lite, sin MediatR)

`Application/Rubricas/Comandos/{CrearRubrica,AgregarCriterio,EditarCriterio,EliminarCriterio,PublicarRubrica}` y `Consultas/ObtenerRubrica`, cada uno con `Command|Query`, `Handler` y, cuando aplica, `Validator`. `DTOs/`: `RubricaDto`, `CriterioEvaluacionDto`, `RubricaMapeador`. Los handlers siguen el patrón de `RegistrarPostulanteCommandHandler`: validar, resolver usuario con `ObtenerUsuarioIdRequerido()`, cargar con tracking mediante `IRubricaRepository`, ejecutar la operación de dominio con `TimeProvider`, `IUnitOfWork.GuardarCambiosAsync` y devolver DTO. Todo método asíncrono propaga `CancellationToken`. Puerto `IRubricaRepository` en `Domain/Interfaces/` junto a `IPostulanteRepository`: `ObtenerPorIdAsync`, `ObtenerParaActualizarPorIdAsync` (con criterios y tracking), `ExistePorProgramaYConvocatoriaAsync`, `AgregarAsync`.

La eliminación de un criterio no renumera `orden`; el `orden` de un criterio nuevo es `máximo + 1` (evita colisiones con el índice único).

### D6. Autorización por rol sin alterar Postulantes

`IUsuarioActual` no expone roles y no se amplía. Se definen dos políticas independientes de ASP.NET Core (sobre el claim `roles`), aplicadas por acción con `[Authorize(Policy = ...)]` solo en `RubricasController`:

- `CoordinadorPrograma` (`RequireRole("COORDINADOR_PROGRAMA")`): aplicada explícitamente en las acciones de escritura (`POST /`, `POST /{id}/criterios`, `PUT /{id}/criterios/{criterioId}`, `DELETE /{id}/criterios/{criterioId}` y `POST /{id}/publicar`).
- `LecturaRubrica` (por ahora `RequireRole("COORDINADOR_PROGRAMA")`): política de lectura independiente aplicada en `GET /{id}`, de modo que habilitar roles de lectura en el futuro no afecte los permisos de escritura.

Para producir `403` con `ProblemDetails` y `codigo = PERMISO_DENEGADO` se registra un `IAuthorizationMiddlewareResultHandler` en `Api/Seguridad/` que, únicamente cuando el resultado es `Forbidden` y el endpoint tiene alguna de las políticas de rúbricas (`CoordinadorPrograma` o `LecturaRubrica`), escribe ese `ProblemDetails`; en cualquier otro caso delega en el comportamiento predeterminado. Así no cambia ningún endpoint de Postulantes (`[Authorize]` sin política: 401/403 actuales). El `401` sin token no cambia. El detalle de roles denegados (`ADMIN_FINANCIERO`, `MIEMBRO_COMITE` u otros) no se enumera: se permite solo `COORDINADOR_PROGRAMA`.

### D7. Persistencia

Esquema `postulaciones`, tablas `rubricas` y `criterios_evaluacion`, configuraciones `RubricaEvaluacionConfiguracion` y `CriterioEvaluacionConfiguracion` con `ConfiguracionPersistencia.ConfigurarEntidadAuditable`, FK `rubrica_evaluacion_id` con borrado en cascada, acceso al campo de respaldo `_criterios`.

- Longitudes y obligatoriedad de texto: `rubricas.nombre` obligatorio `VARCHAR(200)`; `criterios_evaluacion.nombre` obligatorio `VARCHAR(200)`; `criterios_evaluacion.descripcion` obligatoria `VARCHAR(1000)`.
- `CHECK`: `ck_rubricas_estado_valido`; `ck_criterios_evaluacion_peso_rango` (`peso_porcentual > 0 AND peso_porcentual <= 100`); `ck_criterios_evaluacion_puntaje_rango` (`puntaje_minimo >= 0 AND puntaje_maximo <= 1000 AND puntaje_minimo < puntaje_maximo`); `ck_criterios_evaluacion_orden_positivo`.
- Precisión: `peso_porcentual` `numeric(5,2)`; `puntaje_minimo` y `puntaje_maximo` `numeric(7,2)`.
- Índices únicos: `ux_rubricas_programa_convocatoria` sobre (`programa_beca_id`, `convocatoria_id`); `ux_criterios_evaluacion_rubrica_orden` sobre (`rubrica_evaluacion_id`, `orden`). Índice no único por `rubrica_evaluacion_id` lo cubre el único.
- Concurrencia optimista heredada (`ExcepcionConflictoConcurrencia` → `409`).
- La migración se crea en una tarea separada al final del bloque de persistencia.
- Las pruebas de modelo no requieren PostgreSQL real (patrón `ModeloPersistenciaPostulacionesTests`); además se agregan pruebas de integración contra PostgreSQL real con Testcontainers (patrón `PostgreSqlFixture` de `ms-becas`) aplicando la migración.

### D8. API

`RubricasController` en `Api/Controllers/v1/`, ruta `api/v1/rubricas`:

| Método y ruta | Éxito | Errores |
|---|---|---|
| `POST /` | 201 | 400, 401, 403, 409 |
| `GET /{id}` | 200 | 401, 403, 404 |
| `POST /{id}/criterios` | 201 | 400, 401, 403, 404, 409 |
| `PUT /{id}/criterios/{criterioId}` | 200 | 400, 401, 403, 404, 409 |
| `DELETE /{id}/criterios/{criterioId}` | 200 (rúbrica actualizada) | 401, 403, 404, 409 |
| `POST /{id}/publicar` | 200 | 401, 403, 404, 409, 422 |

No hay listados en esta HU; cuando se agreguen deberán paginarse.

## Risks / Trade-offs

- **Roles solo en la API:** la verificación vive en el borde HTTP porque `IUsuarioActual` no expone roles. Si un futuro caso de uso invoca los handlers fuera de HTTP, no habrá verificación; se mitiga con pruebas de endpoint y se podrá mover a un puerto cuando exista el alcance por programa.
- **Sin validación externa de programa/convocatoria:** un coordinador podría crear rúbricas para identificadores inexistentes (ver preguntas abiertas).
- **`orden` con huecos** tras eliminar: aceptable, evita transiciones inválidas del índice único.
- **Rechazo (no redondeo) de más de 2 decimales:** evita aceptar silenciosamente pesos distintos de los enviados.

## Migration Plan

1. Implementar dominio y aplicación con pruebas.
2. Configuración EF y repositorio; luego migración `RubricasEvaluacion` en tarea separada.
3. Controller y política de autorización.
4. Rollback: revertir la migración (`dotnet ef database update <anterior>`); las tablas son nuevas y no alteran las de Postulantes.

## Open Questions

1. **Validación de `programaBecaId` contra `MS-PRG`:** ¿se verificará que el programa exista y esté activo (cliente HTTP interno `/internal/v1/...`)? Hasta decidirlo, solo se exige un `Guid` no vacío.
2. **Validación de `convocatoriaId`:** RF-02 (convocatorias) aún no existe en `MS-POS`/`MS-PLT`; ¿qué fuente validará el ciclo y su estado? Hasta entonces solo se exige un `Guid` no vacío y la unicidad por programa y convocatoria.
3. **Alcance por programa del coordinador:** el requisito «Coordinador con permisos sobre el programa» depende de `MS-PLT` (asignación de rol por programa). Hoy solo se verifica el rol `COORDINADOR_PROGRAMA`, no que el coordinador pertenezca al programa.
4. **Consulta por programa y convocatoria / listado:** actualmente solo existe GET /{id}; no hay consulta por (programaBecaId, convocatoriaId) ni listado paginado, lo cual se agregará en una HU posterior.
