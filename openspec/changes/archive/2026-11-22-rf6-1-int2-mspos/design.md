# Design

## Context

Motivación y alcance en `proposal.md`. Estado observado en `Apps/services/ms-postulaciones`:

- Patrón de referencia: `RubricaEvaluacion : AggregateRoot` (hereda `EntidadAuditable`, `Version` como token de concurrencia) con entidad hija `CriterioEvaluacion`; handlers sin MediatR (`HandleAsync`) con `IUsuarioActual`, repositorio, `IUnitOfWork`, `IValidator<T>` y `TimeProvider`; `PostulacionesDbContext.GuardarCambiosAsync` traduce `DbUpdateConcurrencyException` a `ExcepcionConflictoConcurrencia` (`409`) y `TraductorExcepcionesPersistencia` traduce índices únicos a excepciones de dominio.
- `ManejadorExcepcionesDominio` traduce `DomainException` a `ProblemDetails` con `codigo`; `ArgumentException` produce `400 SOLICITUD_INVALIDA`; FluentValidation produce `400`.
- Seguridad: JWT de Keycloak con claim `roles`; `PoliticasAutorizacionRubricas` y `ManejadorResultadoAutorizacionRubricas` (`403 PERMISO_DENEGADO`, hoy solo para las políticas de rúbricas).
- `docs/` (PRD RF-06, DDD V7 §2.3.B, RN-POS-04, RN-POS-06, RN-PLT-02, matriz de roles) define `ComiteEvaluador` (`id`, `programaBecaId`, `convocatoriaId`, `nombre`, `jefaturaUsuarioId`, `fechaConformacion` `DateOnly`, `conformadoPorUsuarioId`, `activo`) y `MiembroComite` (`id`, `comiteEvaluadorId`, `usuarioEvaluadorId`, `tipo: TipoEvaluador`, `rol: RolMiembroComite`, `fechaAsignacion`, `fechaDesvinculacion`, `activo`), relación `ComiteEvaluador 1 *-- 1..* MiembroComite`. **No** define los valores de `TipoEvaluador` ni `RolMiembroComite`, ni el texto de la HU 6.1 ni su catálogo de permisos por endpoint.
- No hay Hangfire registrado en `ms-postulaciones` (aunque `Directory.Packages.props` lo declara) y la HU 6.1 no exige ejecución en segundo plano: todo es síncrono. No se toca el `packages.lock.json`, por lo que `dotnet restore --locked-mode` no se ve afectado.

## Goals / Non-Goals

**Goals:**

- Agregado `ComiteEvaluador` con miembros internos y externos, jefatura, desvinculación con historial y unicidad por programa y convocatoria, con invariantes en el dominio y reforzadas por la base de datos.
- API REST v1 con `ProblemDetails` y autorización estricta por rol.

**Non-Goals:**

- `AsignacionEvaluacion` y balanceo (HU 6.2), excepción de evaluador único (HU 6.3), `ConflictoInteres` (RF-39), mínimo de 2 evaluadores por postulación (RN-POS-04, se evalúa al asignar), cambio de jefatura, desactivación del comité, validación cruzada con otros microservicios, Hangfire, Outbox, MediatR, eventos de integración.

## Decisions

### D1. Enums (valores propuestos) en `Domain/Enums/` con `CodigoEnum`

| Enum | Valor C# | Código persistido |
|---|---|---|
| `TipoEvaluador` | `Interno` | `INTERNO` |
| `TipoEvaluador` | `Externo` | `EXTERNO` |
| `RolMiembroComite` | `Jefatura` | `JEFATURA` |
| `RolMiembroComite` | `Evaluador` | `EVALUADOR` |

RF-06 y el DDD solo nombran los tipos (`interno`/`externo`) y el campo `jefaturaUsuarioId`; el enum de rol se deriva de «jefatura del comité» (RF-10) y del rol `MIEMBRO_COMITE` (`EVALUADOR`). Se persisten como `VARCHAR` con `ComoCodigoEnum`. Alternativa descartada: un booleano `esExterno` (no extensible ni coherente con el DDD).

### D2. Agregado `ComiteEvaluador` y entidad `MiembroComite`

`Domain/Comites/Agregados/ComiteEvaluador.cs : AggregateRoot` y `Domain/Comites/Entidades/MiembroComite.cs : EntidadAuditable`, ambos con constructor privado, fábricas y setters privados (regla del repositorio: ninguna propiedad mutable con setter público). Colección privada `_miembros` expuesta como `IReadOnlyCollection<MiembroComite>`.

| Miembro | Comportamiento |
|---|---|
| `static Crear(programaBecaId, convocatoriaId, nombre, jefaturaUsuarioId, fechaCreacion, usuarioConformaId)` | Valida identificadores no vacíos y nombre (recortado, obligatorio, máx. 200: `LongitudMaximaNombre`); `FechaConformacion = DateOnly` de `fechaCreacion` en UTC; `ConformadoPorUsuarioId = usuarioConformaId`; `Activo = true`; agrega la jefatura como `MiembroComite` (`Interno`, `Jefatura`). Lanza `ComiteEvaluadorInvalidoException`. |
| `MiembroComite AgregarMiembro(usuarioEvaluadorId, tipo, fecha, usuarioId)` | Valida `usuarioEvaluadorId` no vacío y `tipo` definido; rechaza un usuario ya activo (`MiembroComiteDuplicadoException`); crea con `Evaluador`; `ActualizarAuditoria` (incrementa `Version`). |
| `bool DesvincularMiembro(miembroId, fecha, usuarioId)` | `MiembroComiteNoEncontradoException` si no pertenece al comité; `JefaturaNoDesvinculableException` si es `Jefatura`; si ya está inactivo retorna `false` sin cambios; si no, `Activo = false`, `FechaDesvinculacion`, auditoría, retorna `true`. |

Invariantes del agregado: al menos un miembro activo (la jefatura no se desvincula), exactamente una jefatura activa, sin duplicados activos por usuario. Las fechas de negocio (`fechaConformacion`, `fechaAsignacion`, `fechaDesvinculacion`) son `DateOnly` en UTC derivado de `TimeProvider`, como en el DDD; la auditoría usa `DateTimeOffset` (UTC).

Supuestos señalados: (a) la jefatura es `INTERNO` y miembro del comité (RF-10 la ubica en la Fundación; el DDD guarda `jefaturaUsuarioId` aparte de `MiembroComite`); (b) el comité nace `activo` y 6.1 no lo desactiva (el campo existe por el DDD); (c) el `rol` de un miembro agregado después es siempre `EVALUADOR`.

### D3. Excepciones de dominio (todas `ExcepcionReglaNegocio`, mensajes en español)

| Excepción | Código | HTTP |
|---|---|---|
| `ComiteEvaluadorInvalidoException` | `COMITE_INVALIDO` | 422 |
| `ComiteDuplicadoException` | `COMITE_DUPLICADO` | 409 |
| `ComiteNoEncontradoException` | `COMITE_NO_ENCONTRADO` | 404 |
| `MiembroComiteDuplicadoException` | `MIEMBRO_COMITE_DUPLICADO` | 409 |
| `MiembroComiteNoEncontradoException` | `MIEMBRO_COMITE_NO_ENCONTRADO` | 404 |
| `JefaturaNoDesvinculableException` | `JEFATURA_NO_DESVINCULABLE` | 422 |

Cada una expone `CodigoError` y acepta mensaje opcional, como `RubricaDuplicadaException`. Argumentos no válidos de bajo nivel (`Enum.IsDefined`, `Guid.Empty` en actor) lanzan `ArgumentException` (traducido a `400 SOLICITUD_INVALIDA`), como `Bloquear` en 7.2. Los datos de entrada se validan antes con FluentValidation (`400`).

### D4. Puerto `IComiteEvaluadorRepository` (`Domain/Interfaces/`)

`ObtenerPorIdAsync` (sin tracking), `ObtenerParaActualizarPorIdAsync` (con tracking), `ObtenerPorProgramaYConvocatoriaAsync` (sin tracking), `ExistePorProgramaYConvocatoriaAsync` y `AgregarAsync`; todos con `CancellationToken`. Misma forma que `IRubricaRepository`. Las consultas incluyen los miembros ordenados por `fechaAsignacion` y `id`.

### D5. Aplicación (CQRS sin MediatR, síncrona)

Carpeta `Application/Comites/` con `Comandos/<Caso>/`, `Consultas/<Caso>/` y `DTOs/`:

| Caso | Entrada | Flujo |
|---|---|---|
| `CrearComiteEvaluadorCommand(ProgramaBecaId, ConvocatoriaId, Nombre, JefaturaUsuarioId)` | Validador | Validar → `ObtenerUsuarioIdRequerido` → `ExistePorProgramaYConvocatoria` (si existe, `ComiteDuplicadoException`) → `Crear` → `AgregarAsync` → `GuardarCambiosAsync`; la condición de carrera la resuelve el índice único (traducido a `COMITE_DUPLICADO`). |
| `AgregarMiembroComiteCommand(ComiteId, UsuarioEvaluadorId, Tipo)` | Validador (`Tipo` definido, ids no vacíos) | `ObtenerParaActualizarPorIdAsync` (`ComiteNoEncontradoException` si `null`) → `AgregarMiembro` → `GuardarCambiosAsync`. |
| `DesvincularMiembroComiteCommand(ComiteId, MiembroId)` | Validador | Igual; si `DesvincularMiembro` retorna `false`, responde el DTO sin guardar. |
| `ObtenerComiteEvaluadorQuery(ComiteId)` y `ObtenerComitePorProgramaQuery(ProgramaBecaId, ConvocatoriaId)` | — | Repositorio sin tracking; `ComiteNoEncontradoException` si no existe. Devuelven un único comité, por lo que no requieren paginación. |

DTOs (`sealed record`): `ComiteEvaluadorDto` (id, programa, convocatoria, nombre, jefatura, `fechaConformacion`, `conformadoPorUsuarioId`, `activo`, `version`, `miembros`) y `MiembroComiteDto` (id, `usuarioEvaluadorId`, `tipo` y `rol` como código, `fechaAsignacion`, `fechaDesvinculacion`, `activo`); `ComiteMapeador` con `ADto()` siguiendo `RubricaMapeador`. Handlers `sealed`, registrados en `Application/DependencyInjection.cs` como `AddScoped` (los validadores ya los registra `AddValidatorsFromAssembly`).

**Concurrencia.** Dos altas o bajas simultáneas sobre el mismo comité compiten por `Version`: la segunda recibe `ExcepcionConflictoConcurrencia` (`409`) y el cliente reintenta. Las membresías nuevas se agregan con clave asignada por el cliente (`UuidV7`): igual que `CriterioEvaluacion`, se configura `ValueGeneratedNever()` para que EF las inserte como `Added` y no como `Modified`.

**Hangfire:** no se usa (ver Context).

### D6. Persistencia (EF Core, esquema `postulaciones`)

`ComiteEvaluadorConfiguracion` y `MiembroComiteConfiguracion` en `Infrastructure/Persistence/Configuraciones/`, con `ConfiguracionPersistencia.ConfigurarEntidadAuditable` (auditoría `timestamptz`, `Version` como concurrencia). Dos `DbSet` nuevos en `PostulacionesDbContext`.

`postulaciones.comites_evaluadores`: `programa_beca_id uuid`, `convocatoria_id uuid`, `nombre varchar(200)`, `jefatura_usuario_id uuid`, `fecha_conformacion date`, `conformado_por_usuario_id uuid`, `activo boolean`. Índice único `ux_comites_programa_convocatoria (programa_beca_id, convocatoria_id)`.

`postulaciones.miembros_comite`: `comite_evaluador_id uuid` (FK `fk_miembros_comite_comites` con cascada), `usuario_evaluador_id uuid`, `tipo varchar(10)`, `rol varchar(15)`, `fecha_asignacion date`, `fecha_desvinculacion date` nulable, `activo boolean`.

| Restricción | Definición |
|---|---|
| `ck_miembros_comite_tipo_valido` | `tipo IN ('INTERNO','EXTERNO')` (generada con `ValoresSql<TipoEvaluador>()`) |
| `ck_miembros_comite_rol_valido` | `rol IN ('JEFATURA','EVALUADOR')` |
| `ck_miembros_comite_desvinculacion_coherente` | `(activo AND fecha_desvinculacion IS NULL) OR (NOT activo AND fecha_desvinculacion IS NOT NULL AND fecha_desvinculacion >= fecha_asignacion)` |
| `ck_miembros_comite_jefatura_interna` | `rol <> 'JEFATURA' OR tipo = 'INTERNO'` |
| `ux_miembros_comite_usuario_activo` | índice único parcial `(comite_evaluador_id, usuario_evaluador_id) WHERE activo` |
| `ux_miembros_comite_jefatura_activa` | índice único parcial `(comite_evaluador_id) WHERE activo AND rol = 'JEFATURA'` |

`TraductorExcepcionesPersistencia` agrega las constantes de índice y mapea `ux_comites_programa_convocatoria → ComiteDuplicadoException` y `ux_miembros_comite_usuario_activo → MiembroComiteDuplicadoException` (el resto conserva la ruta existente). `AgregarInfraestructuraPostulaciones` registra `IComiteEvaluadorRepository`.

No hay FK a programas, convocatorias ni usuarios (otros microservicios; consistente con `rubricas`). La migración nueva (`ComitesEvaluadores`) se genera en una **tarea separada justo antes de las pruebas con PostgreSQL real** y tras generarla se ejecuta `dotnet format Ascenso.Postulaciones.sln --no-restore --include <rutas>` (nunca sin `--include`). Rollback: `dotnet ef database update BloqueoRubrica` (tablas nuevas sin datos previos que preservar).

### D7. API y seguridad

`Api/Controllers/v1/ComitesEvaluadoresController.cs`, ruta `api/v1/comites-evaluadores`, con anotaciones `ProducesResponseType`, `ProblemDetails` y documentación XML/Swagger:

| Método y ruta | Caso | Éxito | Errores |
|---|---|---|---|
| `POST /` | Crear | `201` + `ComiteEvaluadorDto` (`Location` → `GET /{id}`) | `400`, `401`, `403`, `409 COMITE_DUPLICADO`, `422` |
| `GET /{id:guid}` | Obtener | `200` | `401`, `403`, `404` |
| `GET /programas/{programaBecaId:guid}/convocatorias/{convocatoriaId:guid}` | Obtener por programa y convocatoria | `200` | `401`, `403`, `404` |
| `POST /{id:guid}/miembros` | Agregar | `201` + comité | `400`, `401`, `403`, `404`, `409 MIEMBRO_COMITE_DUPLICADO` |
| `DELETE /{id:guid}/miembros/{miembroId:guid}` | Desvincular | `200` + comité | `401`, `403`, `404`, `422 JEFATURA_NO_DESVINCULABLE` |

El `tipo` llega como código (`INTERNO`/`EXTERNO`); el controlador lo convierte con `CodigoEnum.DesdeCodigo<TipoEvaluador>` y un valor inválido produce `400 SOLICITUD_INVALIDA` mediante el manejador existente (el validador cubre además el enum ya tipado).

Autorización: nueva clase `PoliticasAutorizacionComites` con la política `GestionComitesEvaluadores` que exige exactamente el rol `COORDINADOR_PROGRAMA` (valor literal de `docs/` y de `PoliticasAutorizacionRubricas.RolCoordinadorPrograma`, que se reutiliza). Se aplica a los cinco endpoints (también a la lectura: la nómina de evaluadores es información confidencial, RN-PLT-02/RN-PLT-05). `ADMIN_FINANCIERO` y `MIEMBRO_COMITE` reciben `403 PERMISO_DENEGADO`.

Como solo puede registrarse un `IAuthorizationMiddlewareResultHandler`, `ManejadorResultadoAutorizacionRubricas` se amplía de forma **mínima y aditiva**: `EndpointTienePoliticaDeRubricas` pasa a reconocer también `GestionComitesEvaluadores`, con detalle del `ProblemDetails` genérico para comités; el cuerpo para rúbricas no cambia (se prueba que las respuestas `403` de rúbricas son idénticas). No se modifican los controladores ni las políticas de `Postulantes` ni `Rubricas`. `Program.cs` solo agrega `AddPolicy`.

### D8. Núcleo compartido (`Domain/Comun`)

No se requiere ninguna identidad ni tipo nuevo: se reutilizan `AggregateRoot`, `EntidadAuditable`, `UuidV7` y `IUsuarioActual` (el actor siempre es un usuario autenticado; no hay disparadores del sistema). No se modifica `Domain/Comun`.

## Risks / Trade-offs

- **HU 6.1 sin texto formal en `docs/`:** criterios derivados de RF-06 y del DDD → se listan como suposiciones y preguntas abiertas para validar con el equipo antes de implementar.
- **Valores de los enums propuestos (D1) y jefatura `INTERNO`:** si el equipo define otros, cambian enum y `CHECK` en la misma migración (aún sin aplicar).
- **Ampliar el manejador de autorización compartido:** riesgo de alterar el `403` de rúbricas → mitigación: pruebas de regresión sobre `AutorizacionRubricasTests` sin cambios.
- **Sin validación cruzada de programa, convocatoria o usuario:** pueden existir comités para ids inexistentes → coherente con `rubricas`; pendiente de RF-02, MS-PRG y MS-PLT.
- **Desvincular con asignaciones vigentes:** HU 6.2 aún no existe; cuando exista deberá bloquear o reasignar → pregunta abierta.
- **Concurrencia por `Version` en altas simultáneas:** el segundo cliente recibe `409` y reintenta; aceptable por baja frecuencia.

## Migration Plan

1. Dominio y aplicación con pruebas (TDD).
2. Configuración EF y pruebas de modelo; luego migración nueva en tarea separada y formato con `--include`.
3. Pruebas de integración con PostgreSQL real; API, seguridad y Swagger.
4. Verificación integral, roadmaps y bitácora.
5. Rollback: revertir la migración; el resto es aditivo.

## Open Questions

1. **Texto de la HU 6.1:** ¿existe una definición y criterios de aceptación fuera de `docs/` que contradigan los escenarios derivados de RF-06 y el DDD?
2. **Valores de `TipoEvaluador` y `RolMiembroComite`:** ¿se confirman `INTERNO`/`EXTERNO` y `JEFATURA`/`EVALUADOR`? ¿La jefatura puede ser `EXTERNO`?
3. **Lectura por `MIEMBRO_COMITE`:** ¿puede un miembro consultar la nómina de su propio comité (hoy `403`)?
4. **Cambio de jefatura y desactivación del comité:** ¿van en 6.1 o posteriores?
5. **Desvinculación con asignaciones vigentes:** ¿se bloquea, se reasigna o se permite (se define con HU 6.2 y RF-39)?
6. **Un comité por programa y convocatoria o por programa:** el DDD tiene ambas claves; se asume una combinación única.
7. **Validación de usuario evaluador:** ¿debe verificarse en MS-PLT (rol `MIEMBRO_COMITE` o usuario externo) y cómo se registra un evaluador externo sin cuenta interna?
