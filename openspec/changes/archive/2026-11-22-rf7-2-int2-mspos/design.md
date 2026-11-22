# Design

## Context

Motivación y alcance en `proposal.md`. Estado observado tras la HU 7.1:

- `RubricaEvaluacion : AggregateRoot` (hereda `EntidadAuditable`, con `Version` como token de concurrencia optimista) expone `Estado`, `FechaPublicacion` y `Publicar`. `AsegurarEstadoBorrador()` lanza `RubricaBloqueadaException` (`RUBRICA_BLOQUEADA`, `409`, mensaje genérico) para cualquier mutación fuera de `BORRADOR`: la inmutabilidad ya funciona, pero ningún flujo asigna `BLOQUEADA`.
- `PostulacionesDbContext.GuardarCambiosAsync` traduce `DbUpdateConcurrencyException` a `ExcepcionConflictoConcurrencia` (`409`).
- `IRubricaRepository` ofrece `ObtenerPorIdAsync` (sin tracking) y `ObtenerParaActualizarPorIdAsync` (con tracking).
- Las políticas `CoordinadorPrograma` y `LecturaRubrica` se registran en `Program.cs` con `RequireRole`; `LecturaRubrica` hoy solo admite `COORDINADOR_PROGRAMA`. No existe `UsuarioSistema` en el repositorio.
- DDD V7: `EstadoRubrica` (`PUBLICADA → BLOQUEADA`), `RN-POS-05` y concurrencia C5.

## Goals / Non-Goals

**Goals:**

- Transición terminal `PUBLICADA → BLOQUEADA` en el dominio, con motivo y fecha, idempotente y segura bajo concurrencia.
- Mensaje de rechazo que explica el motivo (criterio 3) y consulta con indicador `soloLectura` (criterio 4).
- Lectura para `MIEMBRO_COMITE` sin ampliar la escritura.

**Non-Goals:**

- Endpoint HTTP de bloqueo, implementación de los disparadores (RF-02, HU 8.1), clonación (HU 7.3), UI (criterio 2), Hangfire, Outbox, MediatR.

## Decisions

### D1. Enum `MotivoBloqueoRubrica` (valores propuestos)

El DDD no enumera los motivos. Se proponen dos, uno por cada disparador de la regla de negocio, en `Domain/Enums/` con `CodigoEnum`:

| Valor C# | Código persistido | Disparador |
|---|---|---|
| `InicioEvaluacion` | `INICIO_EVALUACION` | Inicio formal de la fase de evaluación del ciclo |
| `PrimeraCalificacion` | `PRIMERA_CALIFICACION` | Registro de la primera puntuación |

Alternativa descartada: un motivo de texto libre (no auditable ni validable por `CHECK`). Ampliar el enum es una migración de `CHECK` si aparecen nuevos disparadores.

### D2. Método de dominio `Bloquear`

`public bool Bloquear(MotivoBloqueoRubrica motivo, DateTimeOffset fecha, Guid usuarioId)` en `RubricaEvaluacion`:

| Estado actual | Resultado |
|---|---|
| `PUBLICADA` | `Estado = Bloqueada`, `MotivoBloqueo = motivo`, `FechaBloqueo = fecha` (UTC), `ActualizarAuditoria` (incrementa `Version`); retorna `true` |
| `BLOQUEADA` | Sin cambios (ni auditoría ni versión); retorna `false`, sin excepción |
| `BORRADOR` | Lanza `RubricaNoPublicadaException` |

Valida que `motivo` esté definido (`Enum.IsDefined`) y que la fecha no sea `default`. El retorno `bool` (en lugar de `void`) permite al handler saber si hubo cambio efectivo y evitar `GuardarCambios` innecesarios; no altera la firma lógica pedida. `MotivoBloqueo` y `FechaBloqueo` tienen `private set` y son nulos fuera de `BLOQUEADA`.

`AsegurarEstadoBorrador()` pasa a producir el mensaje explicativo: `RubricaBloqueadaException.Publicada()` si el estado es `PUBLICADA` y `RubricaBloqueadaException.Bloqueada(fecha, motivo)` si es `BLOQUEADA`.

### D3. Excepciones

| Excepción | Código | HTTP | Mensaje |
|---|---|---|---|
| `RubricaNoPublicadaException` (nueva, `ExcepcionReglaNegocio`) | `RUBRICA_NO_PUBLICADA` | 409 | «Solo una rúbrica publicada puede bloquearse; la rúbrica está en borrador.» |
| `RubricaBloqueadaException` (modificada) | `RUBRICA_BLOQUEADA` | 409 | Publicada: «La rúbrica está publicada y no admite modificaciones; los cambios requieren una rúbrica nueva.» Bloqueada: «La rúbrica está bloqueada desde {fecha:yyyy-MM-dd HH:mm:ss} UTC por {motivo legible}; los cambios requieren una rúbrica nueva.» |

Motivos legibles: «inicio de la etapa de evaluación» y «registro de la primera calificación». El constructor público actual (`string? mensaje = null`) se conserva para no romper pruebas ni código de 7.1; se agregan las fábricas estáticas `Publicada()` y `Bloqueada(...)`. El contrato HTTP no cambia: el `ManejadorExcepcionesDominio` ya traduce `DomainException` a `ProblemDetails` con `codigo`.

### D4. Caso de uso `BloquearRubricaCommand` y concurrencia

`BloquearRubricaCommand(Guid RubricaId, MotivoBloqueoRubrica Motivo)` y `BloquearRubricaCommandHandler` en `Application/Rubricas/Comandos/BloquearRubrica/`, sin MediatR (patrón de `PublicarRubricaCommandHandler`). Devuelve `RubricaDto`. Dependencias: `IRubricaRepository`, `IUnitOfWork`, `TimeProvider`. No usa `IUsuarioActual` porque el disparador es interno.

Flujo:

1. `ObtenerParaActualizarPorIdAsync`; si es `null`, `RubricaNoEncontradaException`.
2. `rubrica.Bloquear(motivo, TimeProvider.GetUtcNow(), UsuarioSistema.Id)`.
3. Si retorna `false` (ya bloqueada): responde el DTO sin llamar a `GuardarCambiosAsync`.
4. Si retorna `true`: `GuardarCambiosAsync`.
5. Si `GuardarCambiosAsync` lanza `ExcepcionConflictoConcurrencia`: se relee con `ObtenerPorIdAsync` (sin tracking, estado fresco). Si ahora está `BLOQUEADA`, otro disparador ganó: se responde su DTO (idempotencia, sin error). En cualquier otro caso se relanza el conflicto original.
6. Si `Bloquear` lanza (`RubricaNoPublicadaException`), no se llama a `GuardarCambiosAsync`.

**Mecanismo y relación con C5.** C5 prescribe el bloqueo como `UPDATE ... WHERE estado = 'PUBLICADA'`: un único cambio efectivo decidido por la base de datos. Aquí se obtiene el mismo efecto con el token de concurrencia `Version` (ya heredado de `EntidadAuditable`): al confirmar, EF Core emite `UPDATE postulaciones.rubricas SET estado='BLOQUEADA', ..., version=N+1 WHERE id=@id AND version=N`. Como una rúbrica `PUBLICADA` es inmutable, su `Version` solo cambia en la transición a `BLOQUEADA`, de modo que `version = N` equivale a `estado = 'PUBLICADA'`. De dos disparadores que leyeron `Version = N`, el segundo `UPDATE` afecta 0 filas, EF lanza `DbUpdateConcurrencyException` → `ExcepcionConflictoConcurrencia`, y el handler lo convierte en éxito idempotente tras releer. El reintento no puede hacerse con la misma instancia rastreada (estaría obsoleta), por eso la relectura usa la consulta sin tracking.

Alternativa descartada: `ExecuteUpdateAsync` con `WHERE estado = 'PUBLICADA'` literal. Es atómico, pero evita el agregado (no pasa por `Bloquear`, auditoría ni versión) y duplicaría las reglas en infraestructura. Queda como plan B si se detectara contención real. Alternativa descartada: bloqueo pesimista (`SELECT ... FOR UPDATE`): innecesario para una transición de baja frecuencia.

Esto se verifica con PostgreSQL real (Testcontainers): dos handlers con `DbContext` distintos que leen antes de que cualquiera confirme; resultado: una sola fila cambiada, un solo motivo y fecha, ambos handlers sin error.

### D5. Actor `UsuarioSistema` (núcleo compartido)

No existe en `Domain/Comun`. Se agrega el mínimo: `public static class UsuarioSistema { public static readonly Guid Id = new("00000000-0000-0000-0000-000000000001"); }`, usado como `usuarioId` de auditoría (el dominio exige un `Guid` no vacío). **Este archivo pertenece al núcleo compartido** (`Domain/Comun`): se agrega sin tocar tipos existentes y debe avisarse al equipo; si el núcleo define luego otro identificador del sistema, se reemplaza esta constante. No hay FK de usuario, por lo que no requiere datos semilla.

### D6. DTO y consulta

`RubricaDto` agrega `string? MotivoBloqueo`, `DateTimeOffset? FechaBloqueo` y `bool SoloLectura` (`Estado != Borrador`). `RubricaMapeador` los llena; el JSON queda `motivoBloqueo`, `fechaBloqueo`, `soloLectura`. Es un cambio aditivo (no rompe consumidores). `GET /api/v1/rubricas/{id}` no cambia de ruta ni de comportamiento; el indicador permite a la UI (fuera de alcance) deshabilitar la edición.

### D7. Autorización

`PoliticasAutorizacionRubricas` agrega `RolMiembroComite = "MIEMBRO_COMITE"`; `Program.cs` registra `LecturaRubrica` con `RequireRole(RolCoordinadorPrograma, RolMiembroComite)` y mantiene `CoordinadorPrograma` solo con `COORDINADOR_PROGRAMA`. `ADMIN_FINANCIERO` sigue en `403 PERMISO_DENEGADO` (mismo `ManejadorResultadoAutorizacionRubricas`). Solo `GET /{id}` usa `LecturaRubrica`; los demás endpoints conservan `CoordinadorPrograma`, por lo que no hay cambio de ruta.

### D8. Persistencia

En `RubricaEvaluacionConfiguracion`:

- `motivo_bloqueo`: `VARCHAR(30)` nulo, mapeado con `ComoCodigoEnum` (verificar que soporta enum nulable; de lo contrario, conversión equivalente local).
- `fecha_bloqueo`: nulo. El pedido indica `timestamp`; se usa `timestamptz` (como `fecha_publicacion`) para conservar el instante UTC con `DateTimeOffset` y evitar ambigüedad de zona; se señala como decisión a confirmar.
- `ck_rubricas_motivo_bloqueo_valido`: `motivo_bloqueo IS NULL OR motivo_bloqueo IN ('INICIO_EVALUACION','PRIMERA_CALIFICACION')`.
- `ck_rubricas_bloqueo_coherente`: `(estado = 'BLOQUEADA' AND motivo_bloqueo IS NOT NULL AND fecha_bloqueo IS NOT NULL) OR (estado IN ('BORRADOR','PUBLICADA') AND motivo_bloqueo IS NULL AND fecha_bloqueo IS NULL)`.

Datos existentes: ningún flujo de 7.1 asigna `BLOQUEADA`, por lo que no hay filas que violen el `CHECK` al migrar. La migración se genera en una **tarea separada justo antes de las pruebas con PostgreSQL real**, y tras generarla se ejecuta `dotnet format Ascenso.Postulaciones.sln --no-restore --include <rutas>` (nunca sin `--include`, para no alterar saltos de línea de archivos previos). Rollback: `dotnet ef database update <migración anterior>` (columnas nulables nuevas; sin pérdida de datos previos).

### D9. Contrato de integración con los disparadores (pendiente de otros RF)

No hay endpoint HTTP. Los disparadores futuros invocarán `BloquearRubricaCommandHandler` (inyección en proceso, sin MediatR):

| Disparador | Origen | Motivo | Estado |
|---|---|---|---|
| Inicio formal de la fase de evaluación de la convocatoria | RF-02 (convocatorias) | `InicioEvaluacion` | No existe aún |
| Registro de la primera puntuación | HU 8.1 (RF-08), dentro del caso de uso de calificación | `PrimeraCalificacion` | No existe aún |

Contrato: el disparador conoce el `rubricaId` (o lo resuelve por programa y convocatoria) y puede invocar el bloqueo cuantas veces quiera: es idempotente y tolera concurrencia. Si la rúbrica está en `BORRADOR`, el handler lanza `RUBRICA_NO_PUBLICADA` y es decisión del disparador abortar su operación (no se puede evaluar sin rúbrica publicada). Para `PrimeraCalificacion`, la HU 8.1 deberá bloquear antes o en la misma operación que persiste la puntuación.

## Risks / Trade-offs

- **Disparadores inexistentes:** hasta RF-02 y HU 8.1, la rúbrica no llegará a `BLOQUEADA` en producción → Mitigación: la regla queda cubierta por pruebas de dominio, aplicación e integración y documentada como contrato (D9).
- **Misma semántica que C5 pero vía `Version`:** si otra operación futura modificara una rúbrica `PUBLICADA`, `version = N` ya no equivaldría a `estado = 'PUBLICADA'` → Mitigación: los estados no editables lo impiden (inmutabilidad); la relectura del handler solo acepta `BLOQUEADA` como éxito.
- **Misma transacción de la primera calificación (HU 8.1):** el bloqueo y la puntuación podrían necesitar compartir `IUnitOfWork` → Mitigación: el handler usa el `IUnitOfWork` del ámbito; HU 8.1 decidirá la composición.
- **`UsuarioSistema` en el núcleo compartido:** riesgo de conflicto con otro integrante → Mitigación: archivo nuevo mínimo, aviso explícito.
- **Mensaje con fecha en UTC:** el cliente debe localizar la hora si lo desea; `fechaBloqueo` en el DTO es el valor estructurado.

## Migration Plan

1. Dominio y aplicación con pruebas (TDD).
2. Configuración EF y pruebas de modelo; luego migración nueva en tarea separada y formato con `--include`.
3. Pruebas de integración con PostgreSQL real; política de lectura y DTO en la API.
4. Verificación integral, roadmaps y bitácora.
5. Rollback: revertir la migración; el resto es aditivo.

## Open Questions

1. **Disparador de inicio de evaluación (RF-02):** ¿qué componente, evento o API interna marca el inicio formal de la fase de evaluación del ciclo y cómo resuelve la rúbrica (por `rubricaId` o por programa y convocatoria)?
2. **Disparador de primera calificación (HU 8.1):** ¿el bloqueo ocurre en la misma transacción que la persistencia de la primera puntuación o antes?
3. **Alcance de lectura por comité (RF-06):** hoy cualquier `MIEMBRO_COMITE` lee cualquier rúbrica; ¿debe limitarse a los comités asignados al programa o convocatoria?
4. **Tipo de `fecha_bloqueo`:** confirmar `timestamptz` (usado aquí) frente al `timestamp` indicado en el pedido.
