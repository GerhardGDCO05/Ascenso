# Design

## Context

Motivación y alcance en `proposal.md`. Estado observado en `develop` (`Apps/services/ms-postulaciones`):

- **`Postulacion` y `ResultadoConsolidadoRanking` no existen.** El DDD V7 ubica `excepcionEvaluadorUnico`, `justificacionEvaluadorUnico` y `autorizadorExcepcionId` en `ResultadoConsolidadoRanking` (RF-09), que se crea al consolidar. HU 6.3 debe poder autorizar la excepción **antes** de que haya evaluaciones, por lo que necesita su propio respaldo de persistencia. `postulacionId` se referencia solo por id, sin FK ni validación de existencia o de estado `EN_EVALUACION`.
- `docs/` (PRD RF-06; DDD V7 RN-POS-04 y RN-POS-06; RN-AUD-01 / RF-35) exige: mínimo 2 evaluadores salvo excepción de un coordinador **sin conflicto** con **justificación obligatoria**, y trazabilidad sin edición silenciosa. **No** define un texto formal de la HU 6.3, un endpoint, límites de longitud de la justificación ni una revocación. No exige ejecución en segundo plano.
- Código vigente: `ServicioAsignacionEvaluadores.Asignar(...)` (estática, pura) con `MinimoEvaluadoresVigentes = 2` y la comprobación `vigentes.Count + nuevos.Count < Minimo`; `AsignarEvaluadoresPostulacionCommandHandler` (sealed, `HandleAsync`, `IUsuarioActual`, `IUnitOfWork`, `TimeProvider`); `AsignacionesEvaluacionController` con política `GestionAsignacionesEvaluacion` (`COORDINADOR_PROGRAMA`); `ManejadorResultadoAutorizacionRubricas` que ya produce `403 PERMISO_DENEGADO` para esa política; `TraductorExcepcionesPersistencia`; `Domain/Comun` con `EntidadInmutable` (`Id`, `FechaCreacion` UTC, `UsuarioAutorId`, `CorrelacionId`; sin operaciones de modificación) aún sin usos en `ms-postulaciones`.
- Hangfire no está registrado en `ms-postulaciones` y HU 6.3 no lo exige: ejecución síncrona; no se toca `packages.lock.json` ni `dotnet restore --locked-mode`.

## Goals / Non-Goals

**Goals:**

- Persistir la autorización de excepción con quién, cuándo (UTC) y justificación obligatoria, de forma inmutable y única por postulación.
- Relajar el mínimo de 2 a 1 evaluador **solo** para la postulación con excepción, sin alterar el resto de las reglas de HU 6.2 ni sus pruebas de dominio.
- API REST v1 con autorización estricta (`COORDINADOR_PROGRAMA`) y contrato de integración claro para RF-39 y RF-09.

**Non-Goals:**

- Verificación real del conflicto de interés del coordinador (RF-39), `Evaluacion` (RF-08), `ResultadoConsolidadoRanking` (RF-09), entidad `Postulacion`, revocación de la excepción, auditoría en `MS-AUD`, notificaciones, Hangfire, Outbox, MediatR y eventos.

## Decisions

### D1. Entidad `AutorizacionExcepcionEvaluadorUnico` (append-only)

`Domain/Asignaciones/Entidades/AutorizacionExcepcionEvaluadorUnico.cs : EntidadInmutable`, constructor privado para EF, fábrica estática y propiedades sin setter público (`ArchitectureTests/EncapsulamientoTests`).

| Miembro | Origen / regla |
|---|---|
| `Id` | `UuidV7.Nuevo()` |
| `PostulacionId` | no vacío (`AsignacionEvaluacionInvalidaException` `ASIGNACION_INVALIDA` si es `Guid.Empty`) |
| `Justificacion` | `string` recortada (`Trim`), 10 a 1000 caracteres; si no, `JustificacionExcepcionInvalidaException` |
| `UsuarioAutorId` (se expone como `UsuarioAutorizadorId` semántico en DTO) | usuario autenticado, `Guid.Empty` → `ArgumentException` |
| `FechaCreacion` (DTO `fechaAutorizacion`) | `TimeProvider.GetUtcNow()` convertida a UTC por la base |
| `CorrelacionId` | `UuidV7.Nuevo()` generado por el handler: el servicio no tiene aún infraestructura de correlación (pregunta abierta 4) |

`static AutorizacionExcepcionEvaluadorUnico Crear(Guid postulacionId, string justificacion, DateTimeOffset fechaAutorizacion, Guid usuarioAutorizadorId, Guid correlacionId)`.

**Decisión de modelado:** se descartó (a) adelantar un `ResultadoConsolidadoRanking` parcial (pertenece a RF-09, mezclaría ranking con asignación y obligaría a crear datos de ranking falsos); (b) agregar un booleano o campos a `AsignacionEvaluacion` (son varias filas por postulación, se duplicaría la justificación y la excepción sería editable); (c) un agregado mutable con `Version` (permitiría reescritura silenciosa, contrario a RF-35). Se eligió una entidad **inmutable** de tabla propia: es trazable, única por postulación y reutilizable por RF-09, que al consolidar copiará `excepcionEvaluadorUnico = true`, `justificacionEvaluadorUnico` y `autorizadorExcepcionId` leyendo el puerto (contrato de integración abajo). No se modifica `Domain/Comun`.

### D2. Servicios de dominio

**`ServicioExcepcionEvaluadorUnico`** (`Domain/Asignaciones/Servicios/`, estática y pura): `Autorizar(Guid postulacionId, string justificacion, IReadOnlyCollection<AsignacionEvaluacion> vigentes, bool yaAutorizada, DateTimeOffset ahora, Guid usuarioAutorizadorId, Guid correlacionId)`:

1. `yaAutorizada` → `ExcepcionEvaluadorUnicoYaAutorizadaException` (`EXCEPCION_YA_AUTORIZADA`, 409).
2. Vigentes (estado distinto de `ReasignadaPorConflicto`) `>= 2` → `ExcepcionEvaluadorUnicoNoAplicableException` (`EXCEPCION_NO_APLICABLE`, 422): la excepción solo tiene sentido mientras no se cumpla RN-POS-04 (una excepción «dormida» sería una autorización sin uso y debilitaría la trazabilidad).
3. Crea la entidad (valida justificación).

**`ServicioAsignacionEvaluadores.Asignar`** recibe un **último parámetro opcional** `int minimoEvaluadores = MinimoEvaluadoresVigentes`. Las llamadas existentes de HU 6.2 compilan y se comportan igual. Se valida que `minimoEvaluadores` sea `1` o `2` (`ArgumentOutOfRangeException`, que el pipeline traduce a `400 SOLICITUD_INVALIDA`; es un error de programación, no de usuario); la comprobación pasa a `vigentes + nuevos < minimoEvaluadores`. Se añade la constante `MinimoEvaluadoresConExcepcion = 1`. Con mínimo 1 siguen vigentes todas las demás reglas (comité activo, miembro activo, sin duplicado, lista o cantidad, fecha límite) y se sigue exigiendo **al menos un** evaluador nuevo o vigente: una solicitud no puede dejar la postulación sin evaluadores.

### D3. Excepciones de dominio (todas `ExcepcionReglaNegocio` o equivalente existente, mensajes en español)

| Excepción | Código | HTTP |
|---|---|---|
| `JustificacionExcepcionInvalidaException` | `JUSTIFICACION_REQUERIDA` (RN-AUD-01) | 400 |
| `ExcepcionEvaluadorUnicoYaAutorizadaException` | `EXCEPCION_YA_AUTORIZADA` | 409 |
| `ExcepcionEvaluadorUnicoNoAplicableException` | `EXCEPCION_NO_APLICABLE` | 422 |
| `ExcepcionEvaluadorUnicoNoEncontradaException` | `EXCEPCION_NO_ENCONTRADA` | 404 (patrón de `ComiteNoEncontradoException`) |

Cada una expone `CodigoError` y acepta mensaje opcional. El `ManejadorExcepcionesDominio` existente ya produce el `ProblemDetails` con `codigo`; se verifica con pruebas de integración que no requiere cambios.

### D4. Puerto `IExcepcionEvaluadorUnicoRepository` (`Domain/Interfaces/`)

- `ObtenerPorPostulacionAsync(Guid postulacionId, CancellationToken)` → `AutorizacionExcepcionEvaluadorUnico?` (sin tracking).
- `AgregarAsync(AutorizacionExcepcionEvaluadorUnico, CancellationToken)`.

Sin métodos de actualización ni borrado, por diseño. No se modifica `IComiteEvaluadorRepository` ni `IAsignacionEvaluacionRepository`.

### D5. Aplicación (CQRS sin MediatR, síncrona)

Carpeta `Application/Asignaciones/`:

| Caso | Entrada | Flujo |
|---|---|---|
| `AutorizarExcepcionEvaluadorUnicoCommand(PostulacionId, Justificacion)` | Validador FluentValidation: `PostulacionId` no vacío; `Justificacion` no nula, no vacía tras `Trim`, entre 10 y 1000 | Validar → `ObtenerUsuarioIdRequerido` → `ObtenerPorPostulacionAsync` (`yaAutorizada`) → `ListarVigentesPorPostulacionAsync` → `ServicioExcepcionEvaluadorUnico.Autorizar` → `AgregarAsync` → `GuardarCambiosAsync` → DTO. La carrera por el índice único la traduce `TraductorExcepcionesPersistencia` a `409`. |
| `ObtenerExcepcionEvaluadorUnicoQuery(PostulacionId)` | Validador (`PostulacionId` no vacío) | `ObtenerPorPostulacionAsync` → DTO o `ExcepcionEvaluadorUnicoNoEncontradaException`. |
| `AsignarEvaluadoresPostulacionCommand` (existente, sin cambio de forma) | Validador: `CantidadEvaluadores >= 1` en lugar de `>= 2` (el resto igual) | El handler recibe `IExcepcionEvaluadorUnicoRepository`, calcula `minimo = existe ? 1 : 2` y lo pasa a `Asignar`. Sin excepción, `cantidad = 1` o un solo usuario sigue produciendo `EVALUACIONES_INSUFICIENTES` (el dominio decide). |

DTO `sealed record ExcepcionEvaluadorUnicoDto(Guid Id, Guid PostulacionId, string Justificacion, Guid UsuarioAutorizadorId, DateTimeOffset FechaAutorizacion)` y `ExcepcionEvaluadorUnicoMapeador.ADto()` en `Application/Asignaciones/DTOs/` (archivo propio: no se modifica `AsignacionMapeador`). Handlers `sealed`, `HandleAsync(..., CancellationToken)`, registrados como `AddScoped` en `Application/DependencyInjection.cs` (validadores ya cubiertos por `AddValidatorsFromAssembly`). **Hangfire:** no se usa.

**Cambios mínimos a pruebas de HU 6.2 (inevitables y documentados):** (1) la prueba del validador que fija `CantidadEvaluadores < 2` como inválido pasa a fijar `< 1` (el caso 1 ya es válido para el validador); la regla efectiva de 2 sin excepción se cubre ahora en handler y dominio. (2) Las pruebas del handler de asignación agregan el nuevo repositorio al constructor con un doble que devuelve `null` (sin excepción), sin cambiar sus aserciones. Las pruebas de `ServicioAsignacionEvaluadores` (HU 6.2) **no se modifican**; se agregan casos nuevos en una clase aparte.

### D6. Persistencia (EF Core, esquema `postulaciones`)

`AutorizacionExcepcionEvaluadorUnicoConfiguracion` en `Infrastructure/Persistence/Configuraciones/` (columnas explícitas: `EntidadInmutable` no usa `ConfigurarEntidadAuditable`), `DbSet` en `PostulacionesDbContext`, `ExcepcionEvaluadorUnicoRepository`.

`postulaciones.excepciones_evaluador_unico`: `id uuid` (`ValueGeneratedNever`), `postulacion_id uuid`, `justificacion varchar(1000)`, `usuario_autorizador_id uuid`, `fecha_autorizacion timestamptz`, `correlacion_id uuid`.

| Restricción | Definición |
|---|---|
| `pk` | `id` |
| `ux_excepciones_evaluador_unico_postulacion` | índice único `(postulacion_id)` |
| `ck_excepciones_justificacion_no_vacia` | `char_length(btrim(justificacion)) >= 10` |
| `trg_excepciones_evaluador_unico_inmutable` | trigger `BEFORE UPDATE OR DELETE` por fila que ejecuta `RAISE EXCEPTION` (creado con `migrationBuilder.Sql` junto con su función; se elimina en `Down`) |

Sin FK a `postulaciones` ni a usuarios (otros contextos; consistente con el resto del servicio) y sin `Version`. El trigger es la defensa de RF-35 frente a ediciones directas; se acepta que un superusuario pueda eliminarlo (el control definitivo corresponde a `MS-AUD`; pregunta abierta 5). `TraductorExcepcionesPersistencia` agrega la constante del índice y mapea `ux_excepciones_evaluador_unico_postulacion → ExcepcionEvaluadorUnicoYaAutorizadaException`; el resto conserva su ruta. `AgregarInfraestructuraPostulaciones` registra `IExcepcionEvaluadorUnicoRepository`.

La migración nueva (`ExcepcionEvaluadorUnico`) se genera en una **tarea separada justo antes de las pruebas con PostgreSQL real**; tras generarla se ejecuta `dotnet format Ascenso.Postulaciones.sln --no-restore --include <rutas>` (nunca sin `--include`). Rollback: `dotnet ef database update AsignacionesEvaluacion` (tabla nueva; se pierde solo lo autorizado tras el despliegue, aceptable antes de RF-09).

### D7. API y seguridad

Se agregan al `AsignacionesEvaluacionController` existente (`api/v1/asignaciones-evaluacion`; cambio aditivo, las cuatro rutas de HU 6.2 no se modifican), con `ProducesResponseType`, `ProblemDetails` y documentación XML/Swagger:

| Método y ruta | Caso | Rol | Éxito | Errores |
|---|---|---|---|---|
| `POST /postulaciones/{postulacionId:guid}/excepcion-evaluador-unico` (cuerpo `{ "justificacion": "..." }`) | Autorizar excepción | `COORDINADOR_PROGRAMA` | `201` + `ExcepcionEvaluadorUnicoDto`, `Location` → `GET` siguiente | `400`, `401`, `403`, `409 EXCEPCION_YA_AUTORIZADA`, `422 EXCEPCION_NO_APLICABLE` |
| `GET /postulaciones/{postulacionId:guid}/excepcion-evaluador-unico` | Consultar excepción | `COORDINADOR_PROGRAMA` | `200` | `401`, `403`, `404 EXCEPCION_NO_ENCONTRADA` |

El `postulacionId` viaja en la ruta y el autorizador sale del JWT (nunca del cuerpo). Ambos endpoints reutilizan la política existente `GestionAsignacionesEvaluacion` (exactamente `COORDINADOR_PROGRAMA`, literal de `docs/`): `MIEMBRO_COMITE` y `ADMIN_FINANCIERO` reciben `403 PERMISO_DENEGADO` con el cuerpo que ya produce `ManejadorResultadoAutorizacionRubricas`. Por tanto **no** se modifican `Program.cs`, `PoliticasAutorizacionAsignaciones` ni el manejador, y los contratos de `Postulantes`, `Rubricas`, `Comites` y `Asignaciones` quedan intactos. Las pruebas de seguridad nuevas cubren los dos endpoints y las existentes (`AutorizacionRubricasTests`, `AutorizacionComitesTests`, `AutorizacionAsignacionesTests`) pasan sin modificación.

### D8. Núcleo compartido (`Domain/Comun`)

No se modifica. Se reutilizan `EntidadInmutable`, `UuidV7` e `IUsuarioActual`. Necesidad mínima descartada: no hay proveedor de correlación, por eso el handler genera el `correlacionId`.

## Contratos de integración

| Contexto | Contrato propuesto (no implementado en 6.3) |
|---|---|
| **RF-39 / RN-POS-06 (conflicto del coordinador)** | RN-POS-04 exige un coordinador **sin** `ConflictoInteres`. HU 6.3 no puede verificarlo: `ConflictoInteres` no existe. Contrato: puerto futuro `IConsultaConflictoInteres.CoordinadorTieneConflictoAsync(postulacionId, usuarioId)` invocado por el handler antes de `Autorizar`; si hay conflicto, `403/422` con código a definir y facultad delegada a otro coordinador o a Dirección. Hasta entonces cualquier `COORDINADOR_PROGRAMA` puede autorizar (riesgo documentado). |
| **RF-09 (`ResultadoConsolidadoRanking`)** | Al consolidar, RF-09 consultará `IExcepcionEvaluadorUnicoRepository.ObtenerPorPostulacionAsync` y copiará `excepcionEvaluadorUnico`, `justificacionEvaluadorUnico` y `autorizadorExcepcionId`; con excepción acepta `cantidadEvaluacionesValidas = 1`. La tabla de HU 6.3 sigue siendo la fuente de verdad de la autorización. |
| **RF-08 (`Evaluacion`)** | Al enviar la única `Evaluacion` de una postulación con excepción, no exigirá una segunda. Sin excepción, el mínimo de 2 permanece. |
| **RF-35 / `MS-AUD`** | La trazabilidad local es inmutable; la publicación a `RegistroAuditoria` (cadena SHA-256) queda como contrato futuro (evento o llamada interna con `id`, `postulacionId`, `usuarioAutorizadorId`, `fechaAutorizacion`) sin Outbox. |
| **`Postulacion` (Integrante 1)** | Cuando exista, deberá validarse que la postulación esté en `EN_EVALUACION` antes de autorizar (puerto `IPostulacionConsulta` ya propuesto en HU 6.2). |
| **RF-31 / `MS-PLT`** | Notificar a Dirección o al comité de una excepción autorizada: fuera de alcance. |

## Risks / Trade-offs

- **Sin texto formal de la HU 6.3 en `docs/`:** longitudes de justificación, unicidad, no revocación y regla «solo con menos de 2 vigentes» son supuestos → preguntas abiertas.
- **Conflicto de interés no verificable:** cualquier coordinador puede autorizar hasta RF-39 → contrato documentado; riesgo aceptado.
- **Inmutable sin revocación:** una excepción autorizada por error no se puede retirar; solo se evita usarla agregando un segundo evaluador. Se reconsidera con RF-39/RF-09.
- **Trigger en PostgreSQL:** protege contra ediciones accidentales, no contra un DBA; se complementa con `MS-AUD`. Aumenta la complejidad de la migración (verificada por prueba de script y Testcontainers).
- **Ajuste del validador y del handler de HU 6.2:** riesgo de regresión → el dominio mantiene `EVALUACIONES_INSUFICIENTES` sin excepción y toda la suite de HU 6.2 debe pasar con solo los dos ajustes descritos.
- **Excepción y asignación concurrentes:** una asignación de 1 evaluador puede leer la excepción justo antes de confirmarse otra; no hay revocación, así que el estado leído no puede invalidarse (no viola invariantes).
- **Sin validación de existencia de la postulación:** pueden autorizarse excepciones para ids inexistentes (coherente con `Asignaciones`).

## Migration Plan

1. Dominio (excepciones, entidad, servicios, parámetro de mínimo, puerto) con pruebas (TDD).
2. Aplicación (comando, consulta, DTO, handler de asignación, DI) con pruebas.
3. Configuración EF, repositorio y traductor con pruebas de modelo.
4. API, seguridad y Swagger.
5. Migración nueva en tarea separada, formato con `--include` y pruebas con PostgreSQL real (Testcontainers).
6. Verificación integral, roadmaps y bitácora.
7. Rollback: revertir la migración; el resto es aditivo.

## Open Questions

1. **Texto de la HU 6.3:** ¿existe una definición con criterios de aceptación fuera de `docs/` que contradiga los escenarios derivados de RF-06 y RN-POS-04?
2. **Justificación:** ¿longitud mínima y máxima oficiales (supuesto 10 y 1000)? ¿Puede adjuntarse evidencia documental?
3. **Revocación:** ¿debe poder retirarse una excepción (con nueva justificación e historial) o es definitiva?
4. **`correlacionId`:** ¿debe provenir de un encabezado de correlación de la plataforma en lugar de generarse en el handler?
5. **Auditoría:** ¿la publicación a `MS-AUD` es síncrona (llamada interna) o por evento, y desde qué HU?
6. **Conflicto de interés:** ¿quién cubre el caso en que el único coordinador del programa tenga conflicto (delegación a Dirección, RF-39)?
7. **Momento de la autorización:** ¿puede autorizarse antes de que la postulación llegue a `EN_EVALUACION` o solo cuando esté en ese estado?
8. **Regla «menos de 2 vigentes»:** ¿se permite autorizar anticipadamente con 2 vigentes (p. ej. por una baja futura), o es correcto rechazarla con `EXCEPCION_NO_APLICABLE`?
9. **Notificación:** ¿a quién se avisa (Dirección, comité) cuando se autoriza una excepción?
