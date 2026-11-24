# Design

## Context

Motivación y alcance en `proposal.md`. Estado observado en `develop` (`Apps/services/ms-postulaciones`):

- **`Postulacion` no existe.** Solo hay `Postulante` (con `MiembroFamiliar`), `RubricaEvaluacion` (+`CriterioEvaluacion`) y `ComiteEvaluador` (+`MiembroComite`, HU 6.1). `postulacionId` se referencia, como en el DDD, solo por identificador, sin FK ni validación de existencia o de estado `EN_EVALUACION`.
- Patrón de referencia (HU 6.1): agregados `AggregateRoot` (`EntidadAuditable`, `Version` como token de concurrencia), constructor privado, fábricas, setters privados (`ArchitectureTests/EncapsulamientoTests` prohíbe setters públicos y colecciones mutables públicas), handlers `sealed` sin MediatR con `HandleAsync`, `IUsuarioActual`, repositorio, `IUnitOfWork`, `IValidator<T>` y `TimeProvider`; `PostulacionesDbContext.GuardarCambiosAsync` traduce `DbUpdateConcurrencyException` a `ExcepcionConflictoConcurrencia` (`409`) y `TraductorExcepcionesPersistencia` traduce índices únicos a excepciones de dominio; `ManejadorExcepcionesDominio` produce `ProblemDetails` con `codigo`.
- Seguridad: JWT de Keycloak con claim `roles`; `PoliticasAutorizacionRubricas.RolCoordinadorPrograma`, `PoliticasAutorizacionComites` y `ManejadorResultadoAutorizacionRubricas` (único `IAuthorizationMiddlewareResultHandler`, ya ampliado en HU 6.1 para comités).
- `docs/` (PRD RF-06, DDD V7 §2.3.B, estado `EstadoAsignacionEvaluacion`, RN-POS-04, RN-POS-06, matriz de roles) define `AsignacionEvaluacion` (`id`, `comiteEvaluadorId`, `convocatoriaId`, `postulacionId`, `miembroComiteId`, `usuarioEvaluadorId`, `fechaAsignacion` y `fechaLimiteEvaluacion` como `DateTimeOffset`, `estado`) con relaciones `ComiteEvaluador 1 <-- 0..* AsignacionEvaluacion`, `MiembroComite 1 <-- 0..* AsignacionEvaluacion` y `AsignacionEvaluacion 1 -- 0..1 Evaluacion`. **No** define un texto formal de la HU 6.2, ni la fórmula de balanceo, ni el valor ni el origen de `fechaLimiteEvaluacion`, ni un endpoint concreto. No exige ejecución en segundo plano.
- Hangfire no está registrado en `ms-postulaciones` (aunque `Directory.Packages.props` lo declara) y la HU 6.2 no lo exige: ejecución síncrona; no se toca `packages.lock.json` ni `dotnet restore --locked-mode`.

## Goals / Non-Goals

**Goals:**

- Agregado `AsignacionEvaluacion` y regla de «al menos 2 evaluadores vigentes distintos por postulación» (RN-POS-04) garantizada de forma atómica.
- Evaluador siempre miembro activo del comité; sin duplicado vigente (también bajo concurrencia); selección balanceada opcional.
- Consultas para coordinador y para el evaluador (evaluación ciega), API REST v1 con autorización estricta.

**Non-Goals:**

- HU 6.3 (excepción de evaluador único), RF-39 (conflictos, reasignación), RF-08 (`Evaluacion`, transiciones `EN_PROGRESO`/`COMPLETADA`), entidad `Postulacion` y su máquina de estados, notificaciones (RF-31), cancelar o reemplazar asignaciones manualmente, cambios en HU 6.1, Hangfire, Outbox, MediatR y eventos.

## Decisions

### D1. Enum `EstadoAsignacionEvaluacion` (`Domain/Enums/`, con `CodigoEnum`)

| Valor C# | Código persistido |
|---|---|
| `Pendiente` | `PENDIENTE` |
| `EnProgreso` | `EN_PROGRESO` |
| `Completada` | `COMPLETADA` |
| `ReasignadaPorConflicto` | `REASIGNADA_POR_CONFLICTO` |

Se declaran los cuatro valores del DDD (para que el `CHECK` y la migración no cambien cuando lleguen RF-08 y RF-39), pero HU 6.2 solo crea `PENDIENTE`. «Vigente» = estado distinto de `ReasignadaPorConflicto`; «abierta» (para la carga) = `Pendiente` o `EnProgreso`. Se persiste como `VARCHAR(30)` (`REASIGNADA_POR_CONFLICTO` tiene 24 caracteres) con `ComoCodigoEnum`.

### D2. Agregado `AsignacionEvaluacion`

`Domain/Asignaciones/Agregados/AsignacionEvaluacion.cs : AggregateRoot` (constructor privado para EF, fábrica, setters privados). Se modela como **raíz independiente** (como en el DDD) y no como hijo de `ComiteEvaluador`: crece sin límite, se consulta por postulación y por evaluador, y evita bloquear el comité con `Version` en cada asignación.

| Miembro | Comportamiento |
|---|---|
| `static Crear(ComiteEvaluador comite, Guid miembroComiteId, Guid postulacionId, DateTimeOffset fechaAsignacion, DateTimeOffset fechaLimiteEvaluacion, Guid usuarioAsignaId)` | Valida `postulacionId` no vacío; comité `Activo` (`ComiteInactivoException`); `fechaLimiteEvaluacion > fechaAsignacion` (`AsignacionEvaluacionInvalidaException`); el `miembroComiteId` existe en `comite.Miembros` y está `Activo` (`EvaluadorNoMiembroActivoException`); copia `ComiteEvaluadorId`, `ConvocatoriaId` y `UsuarioEvaluadorId` desde el comité y el miembro (no del cliente); `Estado = Pendiente`. |

Propiedades de solo lectura según el DDD. No hay métodos de transición en 6.2 (RF-08/RF-39 los agregarán).

**Servicio de dominio** `Domain/Asignaciones/Servicios/ServicioAsignacionEvaluadores` (clase estática, sin dependencias externas, pura y probable): `IReadOnlyList<AsignacionEvaluacion> Asignar(ComiteEvaluador comite, Guid postulacionId, IReadOnlyCollection<Guid>? usuariosSolicitados, int? cantidad, IReadOnlyCollection<AsignacionEvaluacion> vigentes, IReadOnlyDictionary<Guid,int> cargaPorMiembro, DateTimeOffset ahora, DateTimeOffset fechaLimite, Guid usuarioAsignaId)`:

1. Si `vigentes` no está vacío y su `ComiteEvaluadorId` difiere del comité → `AsignacionEvaluacionInvalidaException`.
2. **Modo manual** (`usuariosSolicitados` con elementos): elimina repetidos; cada usuario debe tener una membresía activa del comité (`EvaluadorNoMiembroActivoException`) y no estar entre los vigentes (`AsignacionDuplicadaException`).
3. **Modo automático** (`cantidad`): candidatos = miembros activos no vigentes en la postulación, ordenados por `cargaPorMiembro` (0 si falta), `FechaAsignacion` de la membresía y `Id` (determinista); toma los `cantidad` primeros. Si hay menos → `EvaluadoresInsuficientesException`.
4. **Regla RN-POS-04:** `vigentes.Count + nuevos.Count >= 2`, si no → `EvaluadoresInsuficientesException` (`EVALUACIONES_INSUFICIENTES`). Al no existir cancelación de asignaciones en 6.2, las vigentes solo crecen y la invariante no se degrada.
5. Crea cada asignación con `AsignacionEvaluacion.Crear`.

Justificación: la regla de mínimo 2 se evalúa sobre el **resultado de la operación**, por lo que es atómica: o se insertan todas las asignaciones en una transacción o ninguna; no hay estado intermedio con un solo evaluador. Alternativa descartada: un endpoint por evaluador (dejaría postulaciones con 1 evaluador y obligaría a una validación diferida sin punto de aplicación natural). Un máximo de evaluadores no está en el DDD y no se impone (pregunta abierta). Se permiten todos los miembros activos, incluida la jefatura (pregunta abierta).

### D3. Excepciones de dominio (todas `ExcepcionReglaNegocio`, mensajes en español)

| Excepción | Código | HTTP |
|---|---|---|
| `AsignacionEvaluacionInvalidaException` | `ASIGNACION_INVALIDA` | 422 |
| `EvaluadoresInsuficientesException` | `EVALUACIONES_INSUFICIENTES` (código de RN-POS-04 del DDD) | 422 |
| `EvaluadorNoMiembroActivoException` | `EVALUADOR_NO_MIEMBRO_ACTIVO` | 422 |
| `ComiteInactivoException` | `COMITE_INACTIVO` | 422 |
| `AsignacionDuplicadaException` | `ASIGNACION_DUPLICADA` | 409 |

Cada una expone `CodigoError` y acepta mensaje opcional, como `RubricaDuplicadaException`. `ComiteNoEncontradoException` (HU 6.1) se reutiliza sin modificarse. Argumentos de bajo nivel (`Guid.Empty` del actor, enum indefinido) → `ArgumentException` (`400 SOLICITUD_INVALIDA`).

### D4. Puerto `IAsignacionEvaluacionRepository` (`Domain/Interfaces/`)

- `AgregarRangoAsync(IReadOnlyCollection<AsignacionEvaluacion>, ct)`.
- `ListarVigentesPorPostulacionAsync(postulacionId, ct)` (con tracking no necesario; sin tracking).
- `ListarPorPostulacionAsync(postulacionId, ct)` (todos los estados, orden `FechaAsignacion`, `Id`).
- `ObtenerCargaPorMiembroAsync(comiteId, ct)` → `IReadOnlyDictionary<Guid,int>` (asignaciones abiertas agrupadas por `miembroComiteId`; el servidor de aplicación completa con ceros).
- `ListarPorEvaluadorAsync(usuarioEvaluadorId, EstadoAsignacionEvaluacion? estado, int pagina, int tamano, ct)` → `PaginaResultado<AsignacionEvaluacion>` (record en `Domain/Interfaces/Contratos/`: `Items`, `Total`), orden `FechaLimiteEvaluacion`, `Id`.

Misma forma que `IRubricaRepository`. `IComiteEvaluadorRepository.ObtenerPorIdAsync` (HU 6.1, sin tracking, con miembros) se reutiliza tal cual: **no se modifica el puerto del comité**.

### D5. Aplicación (CQRS sin MediatR, síncrona)

Carpeta `Application/Asignaciones/` con `Comandos/<Caso>/`, `Consultas/<Caso>/` y `DTOs/`:

| Caso | Entrada | Flujo |
|---|---|---|
| `AsignarEvaluadoresPostulacionCommand(ComiteId, PostulacionId, FechaLimiteEvaluacion, UsuariosEvaluadoresIds?, CantidadEvaluadores?)` | Validador: ids no vacíos; `FechaLimiteEvaluacion` no default; exactamente uno de los dos modos (lista con ids no vacíos, o `CantidadEvaluadores >= 2`); lista y cantidad simultáneas rechazadas; en modo manual no se exige 2 aquí (lo decide el dominio con `EVALUACIONES_INSUFICIENTES`, también para repetidos) | Validar → `ObtenerUsuarioIdRequerido` → `IComiteEvaluadorRepository.ObtenerPorIdAsync` (`ComiteNoEncontradoException`) → `ListarVigentesPorPostulacion` → carga por miembro solo en modo automático → `ServicioAsignacionEvaluadores.Asignar` → `AgregarRangoAsync` → `GuardarCambiosAsync` → DTO. |
| `ObtenerAsignacionesPostulacionQuery(PostulacionId)` | Validador | `ListarPorPostulacionAsync` → lista (vacía si no hay). |
| `ObtenerCargaEvaluadoresComiteQuery(ComiteId)` | Validador | Comité sin tracking (`ComiteNoEncontradoException`) + `ObtenerCargaPorMiembro`; devuelve miembros activos (incluida carga 0). |
| `ObtenerMisAsignacionesQuery(Estado?, Pagina, Tamano)` | Validador (`Pagina >= 1`, `1 <= Tamano <= 100`, estado definido) | `ObtenerUsuarioIdRequerido` (el filtro siempre es el usuario autenticado) → `ListarPorEvaluadorAsync` → `PaginaDto`. |

DTOs (`sealed record`): `AsignacionEvaluacionDto` (vista del coordinador: id, `comiteEvaluadorId`, `convocatoriaId`, `postulacionId`, `miembroComiteId`, `usuarioEvaluadorId`, `estado` como código, fechas, `version`); `AsignacionesPostulacionDto` (`postulacionId`, `asignaciones`); `CargaEvaluadorDto` (`miembroComiteId`, `usuarioEvaluadorId`, `tipo`, `rol`, `asignacionesAbiertas`); `MiAsignacionDto` (**evaluación ciega**: `asignacionEvaluacionId`, `postulacionId`, `convocatoriaId`, `estado`, `fechaAsignacion`, `fechaLimiteEvaluacion`; sin `usuarioEvaluadorId` propio redundante ni coevaluadores ni datos del postulante) y `PaginaDto<T>` (`items`, `pagina`, `tamano`, `total`). `AsignacionMapeador` con `ADto()` / `AMiAsignacionDto()` siguiendo `ComiteMapeador`. Handlers `sealed`, registrados en `Application/DependencyInjection.cs` como `AddScoped` (validadores ya registrados por `AddValidatorsFromAssembly`).

**Concurrencia:** la invariante de duplicado la resuelve el índice único parcial (D6): `TraductorExcepcionesPersistencia` lo traduce a `AsignacionDuplicadaException` (`409`). Dos coordinadores que asignan pares distintos a la misma postulación a la vez producen 4 asignaciones válidas (no hay máximo; el mínimo se cumple). Dos solicitudes automáticas simultáneas pueden leer la misma carga: solo afecta el óptimo del balanceo, no una invariante. Las nuevas asignaciones se insertan con clave del cliente (`UuidV7`), `ValueGeneratedNever()`. Modificaciones futuras sobre la misma fila compiten por `Version` (`409`).

**Hangfire:** no se usa (ver Context).

### D6. Persistencia (EF Core, esquema `postulaciones`)

`AsignacionEvaluacionConfiguracion` en `Infrastructure/Persistence/Configuraciones/` con `ConfiguracionPersistencia.ConfigurarEntidadAuditable`; nuevo `DbSet` en `PostulacionesDbContext`; `AsignacionEvaluacionRepository`.

`postulaciones.asignaciones_evaluacion`: `comite_evaluador_id uuid`, `convocatoria_id uuid`, `postulacion_id uuid`, `miembro_comite_id uuid`, `usuario_evaluador_id uuid`, `fecha_asignacion timestamptz`, `fecha_limite_evaluacion timestamptz`, `estado varchar(30)`.

| Restricción | Definición |
|---|---|
| `fk_asignaciones_comites` | FK a `comites_evaluadores(id)`, `ON DELETE RESTRICT` |
| `fk_asignaciones_miembros_comite` | FK a `miembros_comite(id)`, `ON DELETE RESTRICT` (la desvinculación de HU 6.1 es lógica, por lo que no choca) |
| `ck_asignaciones_estado_valido` | `estado IN ('PENDIENTE','EN_PROGRESO','COMPLETADA','REASIGNADA_POR_CONFLICTO')` (`ValoresSql<EstadoAsignacionEvaluacion>()`) |
| `ck_asignaciones_fecha_limite_posterior` | `fecha_limite_evaluacion > fecha_asignacion` |
| `ux_asignaciones_postulacion_evaluador_vigente` | índice único parcial `(postulacion_id, usuario_evaluador_id) WHERE estado <> 'REASIGNADA_POR_CONFLICTO'` |
| `ix_asignaciones_evaluador_estado` | `(usuario_evaluador_id, estado, fecha_limite_evaluacion)` para «mis asignaciones» |
| `ix_asignaciones_comite_estado` | `(comite_evaluador_id, estado)` para la carga |
| `ix_asignaciones_postulacion` | `(postulacion_id)` |

Sin `date`: el DDD define `fechaAsignacion` y `fechaLimiteEvaluacion` como `DateTimeOffset`, por eso `timestamptz`. Sin FK a postulaciones, convocatorias ni usuarios (otros contextos; consistente con `rubricas` y `comites`). Un evaluador no puede estar duplicado a nivel de índice dentro de una misma solicitud (se deduplica antes, D2).

`TraductorExcepcionesPersistencia` agrega la constante del índice y mapea `ux_asignaciones_postulacion_evaluador_vigente → AsignacionDuplicadaException` (el resto conserva su ruta). `AgregarInfraestructuraPostulaciones` registra `IAsignacionEvaluacionRepository`.

La migración nueva (`AsignacionesEvaluacion`) se genera en una **tarea separada justo antes de las pruebas con PostgreSQL real**; tras generarla se ejecuta `dotnet format Ascenso.Postulaciones.sln --no-restore --include <rutas>` (nunca sin `--include`). Rollback: `dotnet ef database update ComitesEvaluadores` (tabla nueva sin datos previos que preservar).

### D7. API y seguridad

`Api/Controllers/v1/AsignacionesEvaluacionController.cs`, ruta `api/v1/asignaciones-evaluacion`, con `ProducesResponseType`, `ProblemDetails` y documentación XML/Swagger:

| Método y ruta | Caso | Rol | Éxito | Errores |
|---|---|---|---|---|
| `POST /` | Asignar evaluadores | `COORDINADOR_PROGRAMA` | `201` + `AsignacionesPostulacionDto` (`Location` → `GET /postulaciones/{postulacionId}`) | `400`, `401`, `403`, `404 COMITE_NO_ENCONTRADO`, `409 ASIGNACION_DUPLICADA`, `422` (`EVALUACIONES_INSUFICIENTES`, `EVALUADOR_NO_MIEMBRO_ACTIVO`, `ASIGNACION_INVALIDA`, `COMITE_INACTIVO`) |
| `GET /postulaciones/{postulacionId:guid}` | Asignaciones de la postulación | `COORDINADOR_PROGRAMA` | `200` | `401`, `403` |
| `GET /comites/{comiteId:guid}/carga` | Carga por evaluador | `COORDINADOR_PROGRAMA` | `200` | `401`, `403`, `404` |
| `GET /mis-asignaciones?estado&pagina&tamano` | Mis asignaciones | `MIEMBRO_COMITE` | `200` + `PaginaDto<MiAsignacionDto>` | `400`, `401`, `403` |

Se evalúa y decide: `MIEMBRO_COMITE` consulta **solo** las propias asignaciones (el identificador sale del JWT, nunca de la ruta); no puede asignar ni ver asignaciones ajenas. El coordinador no usa `mis-asignaciones` (`403`) para no mezclar la vista ciega con la de gestión.

Autorización: nueva clase `PoliticasAutorizacionAsignaciones` con las políticas `GestionAsignacionesEvaluacion` (exactamente `COORDINADOR_PROGRAMA`, reutilizando `PoliticasAutorizacionRubricas.RolCoordinadorPrograma`) y `ConsultaMisAsignaciones` (exactamente `MIEMBRO_COMITE`, constante literal de `docs/`). `ADMIN_FINANCIERO` recibe `403 PERMISO_DENEGADO` en todas. `ManejadorResultadoAutorizacionRubricas` se amplía de forma **mínima y aditiva** (como en HU 6.1) para reconocer las dos políticas nuevas; el cuerpo `403` de rúbricas y comités no cambia y se cubre con las pruebas existentes sin modificarlas. `Program.cs` solo agrega `AddPolicy`. No se modifican `ComitesEvaluadoresController`, `Postulantes` ni `Rubricas`.

### D8. Núcleo compartido (`Domain/Comun`)

No se requiere ningún tipo nuevo: se reutilizan `AggregateRoot`, `EntidadAuditable`, `UuidV7` e `IUsuarioActual`. No se modifica `Domain/Comun`. `PaginaResultado<T>` vive en `Domain/Interfaces/Contratos/` (junto a los contratos existentes), no en `Comun`.

## Contratos de integración

| Contexto | Contrato propuesto (no implementado en 6.2) |
|---|---|
| **`Postulacion` / RF-02-RF-04 (Integrante 1)** | Cuando exista la entidad, el handler de asignación deberá consultar (puerto `IPostulacionConsulta`, ej. `ObtenerEstadoYConvocatoriaAsync(postulacionId)`) que la postulación esté en `EN_EVALUACION` y que su `convocatoriaId` coincida con la del comité (`422` con código a definir). Hasta entonces `postulacionId` es opaco. |
| **HU 6.3 (excepción de evaluador único)** | Si un coordinador sin conflicto autoriza `excepcionEvaluadorUnico` (hoy en `ResultadoConsolidadoRanking`), la regla de mínimo 2 de D2 paso 4 se relaja para esa postulación. 6.2 deja el punto de extensión: el servicio recibe `vigentes` y el mínimo es una constante (`MinimoEvaluadoresVigentes = 2`) parametrizable. |
| **RF-39 / RN-POS-06 (conflictos)** | El conflicto declarado marcará la asignación `REASIGNADA_POR_CONFLICTO` (sale de «vigente», libera el índice único) y reasignará automáticamente a otro miembro del mismo comité usando el mismo servicio (modo automático, excluyendo al declarante). Además deberá validar, al asignar, que el evaluador no tenga conflicto declarado con la postulación (hoy no verificable). |
| **RF-08 (`Evaluacion`)** | Pasará la asignación `PENDIENTE → EN_PROGRESO → COMPLETADA` y creará `Evaluacion` con `asignacionEvaluacionId`; deberá comprobar que `usuarioEvaluadorId` de la asignación coincide con el evaluador autenticado. |
| **HU 6.1 (desvinculación de miembros)** | Hoy `DesvincularMiembro` no consulta asignaciones. Contrato: al desvincular un miembro con asignaciones abiertas deberá bloquear (`409`) o reasignar (modo automático). No se implementa en 6.2 para no modificar `ComitesEvaluadores`/HU 6.1. |
| **RF-31 / `MS-PLT` (notificaciones)** | El PRD exige notificar al evaluador de nuevas asignaciones y recordar el cierre. Sin Outbox ni eventos en este servicio: queda como contrato futuro (evento o llamada interna) con `asignacionEvaluacionId`, `usuarioEvaluadorId` y `fechaLimiteEvaluacion`. |

## Risks / Trade-offs

- **HU 6.2 sin texto formal en `docs/`:** criterios derivados de RF-06, DDD y RN-POS-04 → suposiciones y preguntas abiertas para validar con el equipo antes de implementar.
- **Sin validación de la postulación (estado y convocatoria):** pueden asignarse ids inexistentes → mitigación: contrato futuro con `Postulacion`; coherente con `rubricas`/`comites`.
- **Desvinculación con asignaciones abiertas:** un evaluador desvinculado conservaría sus asignaciones y las vería en «mis asignaciones» → decisión provisional: no filtrar por membresía vigente (la asignación ya es válida) hasta resolver la pregunta 6.
- **Balanceo simple por conteo:** no pondera estado ni plazo ni tipo de evaluador; carrera entre solicitudes automáticas simultáneas puede desbalancear levemente (no viola invariantes).
- **Ampliar el manejador de autorización compartido (otra vez):** riesgo de alterar `403` previos → pruebas de regresión sin cambios (`AutorizacionRubricasTests`, `AutorizacionComitesTests`).
- **Sin cancelación de asignaciones:** una asignación errónea solo puede resolverse con RF-39 → pregunta abierta; evita estados inconsistentes con RN-POS-04.

## Migration Plan

1. Dominio (enum, excepciones, agregado, servicio, puerto) y aplicación con pruebas (TDD).
2. Configuración EF y pruebas de modelo; repositorio y traductor.
3. API, seguridad y Swagger.
4. Migración nueva en tarea separada, formato con `--include`, y pruebas con PostgreSQL real (Testcontainers).
5. Verificación integral, roadmaps y bitácora.
6. Rollback: revertir la migración; el resto es aditivo.

## Open Questions

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
