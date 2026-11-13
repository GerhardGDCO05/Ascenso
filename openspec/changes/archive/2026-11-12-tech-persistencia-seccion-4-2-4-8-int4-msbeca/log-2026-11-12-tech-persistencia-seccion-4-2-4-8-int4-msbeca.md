# Registro de Auditoría — Tareas 4.2–4.8: persistencia y migraciones de MS-BECA

- **Fecha y hora de ejecución:** 12 de noviembre de 2026, 01:25 (UTC-4)
- **Rama local:** `Gerhard`
- **Alcance:** implementar y verificar las tareas `4.2` a `4.8` de `openspec/changes/bootstrap-ms-beca/tasks.md`.
- **Trabajo local:** sin `git push` ni modificación de ramas remotas.
- **Microservicio intervenido:** solo `Apps/services/ms-becas/`; `ms-pos` y `ms-plt` intactos.
- **Dependencias externas:** ninguna nueva descargada; se reutilizaron Hangfire, MassTransit, Npgsql y Testcontainers ya centralizados.

## 1. Objetivo

Completar la capa de persistencia de MS-BECA: convenciones PostgreSQL, índices únicos parciales, `CHECK` constraints, inmutabilidad del historial, proyecciones idempotentes, idempotencia de comandos y tablas de Outbox/Inbox/Hangfire. Todo se valida contra PostgreSQL real y con pruebas de integración.

## 2. Cambios realizados

### 2.1 Convenciones y configuraciones de EF Core

- `Persistence/Configuraciones/ConfiguracionPersistencia.cs`
  - Auditoría común: `id` y claves foráneas como `uuid`, fechas como `timestamptz`, `version` como token de concurrencia.
  - Enumeraciones persistidas como `varchar` en MAYÚSCULAS mediante `ACodigo()`; lectura inversa mediante `DesdeCodigo<TEnum>()`.
  - `ConfigurarMonto` usa `numeric(18,2)` y `ConfigurarPorcentaje` usa `numeric(5,2)`.
  - `ValoresSql<TEnum>()` genera las listas de valores para los `CHECK`.
- `Persistence/Configuraciones/BecaConfiguracion.cs`
- `Persistence/Configuraciones/OfertaBecaConfiguracion.cs`
- `Persistence/Configuraciones/CompromisoBecarioConfiguracion.cs`
- `Persistence/Configuraciones/HistorialEstadoBecaConfiguracion.cs`
- `Persistence/Configuraciones/ProgramaBecaRefConfiguracion.cs`
- `Persistence/Configuraciones/CostoMatriculaRefConfiguracion.cs`
- `Persistence/Configuraciones/ConvenioRefConfiguracion.cs`
- `Persistence/Configuraciones/IdempotencyRequestConfiguracion.cs`

Cada configuración define nombres `snake_case`, claves primarias, claves foráneas, índices, objetos de valor `OwnsOne` y restricciones con nombres explícitos (`pk_beca`, `fk_oferta_beca_beca`, `ix_historial_estado_beca_beca_id`, etc.).

### 2.2 Índices únicos parciales

- `ux_beca_estudiante_cobertura_total_activa`: una beca `ACTIVA` con cobertura `TOTAL` por estudiante.
- `ux_beca_estudiante_periodo_no_acumulable_activa`: una beca `ACTIVA` no acumulable por estudiante y periodo.
- `ux_beca_cadena_periodo`: una beca por cadena y periodo vigente.
- `ux_oferta_beca_beca`: una oferta por beca.
- `ux_beca_codigo`: código de beca único.

Además se agregaron `ux_compromiso_becario_beca`, `ux_idempotency_request_endpoint_clave` e `ix_historial_estado_beca_beca_id`.

### 2.3 `CHECK` constraints

- Estados permitidos en `beca`, `oferta_beca`, `compromiso_becario` y `programa_beca_ref`.
- Montos no negativos en beca, oferta, compromiso y proyecciones.
- Porcentajes entre 0 y 100 y promedios entre 0 y 20.
- Fechas coherentes: activación/conclusión posteriores al otorgamiento, vencimiento posterior a la emisión, vigencia de costos y expiración de idempotencia.
- Periodos válidos: duración mayor que cero y periodos cursados dentro de la duración.
- Códigos de estado HTTP de idempotencia entre 100 y 599.

### 2.4 Inmutabilidad del historial

- `Persistence/Migraciones/20261112051643_InitialCreate.cs`: función `becas.fn_historial_estado_beca_inmutable()` y trigger `trg_historial_estado_beca_inmutable`.
- `INSERT` permitido; `UPDATE` y `DELETE` rechazados con SQLSTATE `23000` (`integrity_constraint_violation`).

### 2.5 Proyecciones locales

- `Persistence/Proyecciones/ActualizadorProyeccionesRef.cs`
  - Upsert idempotente para `programa_beca_ref`, `costo_matricula_ref` y `convenio_ref`.
  - Actualiza solo si cambia `ultimo_evento_id` y `occurred_at` no retrocede.
  - `fecha_ultima_actualizacion` se escribe con `NOW()`.
  - Un evento repetido o anterior devuelve `0` filas afectadas.

### 2.6 Outbox/Inbox y Hangfire

- `Persistence/BecasDbContext.cs`: `AddInboxStateEntity`, `AddOutboxStateEntity` y `AddOutboxMessageEntity` en el esquema `becas`.
- `Dependencias/RegistroHangfire.cs`: almacenamiento PostgreSQL con esquema `hangfire` y `PrepareSchemaIfNecessary = true`.
- `Ascenso.Becas.Api/Program.cs`: registro de Hangfire y servidor con colas `critical` y `default`.
- `Ascenso.Becas.Api/appsettings.json`: cadena `ConnectionStrings:Becas` local.

### 2.7 Migración

- `Persistence/Migraciones/20261112051643_InitialCreate.cs` + Designer + snapshot.
- `Persistence/Migraciones/.editorconfig`: excluye las migraciones generadas de analizadores de estilo y calidad.
- Script SQL generado en `/tmp/opencode/migracion-becas.sql` (350 líneas) y revisado: tablas, tipos, índices, `CHECK`, trigger y tablas de MassTransit.

### 2.8 Pruebas de integración

- `tests/Ascenso.Becas.IntegrationTests/Persistencia/PostgreSqlFixture.cs`: Testcontainers `postgres:17`, migraciones aplicadas una vez y cadena compartida por colección.
- `DatosPrueba.cs`: datos válidos y únicos para el agregado.
- `ModeloPersistenciaTests.cs`: tablas/columnas `snake_case`, tipos, enums como `varchar`, `OwnsOne`, concurrencia optimista.
- `IndicesUnicosTests.cs`: los cinco índices existen y rechazan duplicados.
- `RestriccionesCheckTests.cs`: cada `CHECK` rechaza la fila inválida con la restricción esperada.
- `HistorialInmutableTests.cs`: `INSERT` funciona; `UPDATE` y `DELETE` fallan.
- `ProyeccionesRefTests.cs`: upsert idempotente y no retroceso por evento anterior.
- `IdempotenciaPersistenciaTests.cs`: persistencia de hash/estado/respuesta/expiración y unicidad por `(endpoint, clave)`.
- `MensajeriaYHangfireTests.cs`: tablas de Outbox/Inbox utilizables y esquema Hangfire con jobs encolados.

### 2.9 Documentación

- `README.md`: se agregó la sección «Base de datos local» con la creación de `ascenso_becas`, la aplicación de migraciones, la generación del script SQL y la variable `ConnectionStrings__Becas`.

## 3. Bug corregido

La lectura inversa de enums usaba `Enum.Parse<TEnum>("OTORGADA")`, pero los nombres del enum son PascalCase (`Otorgada`), por lo que la materialización desde PostgreSQL fallaba. Se reemplazó por `ConfiguracionPersistencia.DesdeCodigo<TEnum>()`, que compara contra `ACodigo()` y conserva el contrato de códigos en MAYÚSCULAS. Las pruebas de modelo, persistencia e idempotencia cubren este caso.

## 4. Validación ejecutada

```text
dotnet build Apps/services/ms-becas/Ascenso.Becas.sln -c Release --no-restore
→ 0 advertencias, 0 errores

dotnet test Apps/services/ms-becas/Ascenso.Becas.sln -c Release --no-build --no-restore
→ Ascenso.Becas.Domain.Tests        136/136 correctas
→ Ascenso.Becas.Application.Tests     1/1   correcta
→ Ascenso.Becas.ArchitectureTests     8/8   correctas
→ Ascenso.Becas.IntegrationTests    108/108 correctas
→ Total                            253/253 correctas

dotnet format Apps/services/ms-becas/Ascenso.Becas.sln \
  --verify-no-changes --no-restore
→ exit 0

dotnet ef migrations has-pending-model-changes \
  --project Apps/services/ms-becas/Ascenso.Becas.Infrastructure \
  --startup-project Apps/services/ms-becas/Ascenso.Becas.Infrastructure \
  --configuration Release --no-build --context BecasDbContext
→ No changes have been made to the model since the last migration.

dotnet ef database update (misma configuración)
→ No migrations were applied. The database is already up to date.

dotnet ef migrations script (misma configuración)
→ /tmp/opencode/migracion-becas.sql generado y revisado
```

La base local `ascenso_becas` se creó y migró con la cadena
`Host=localhost;Port=5432;Database=ascenso_becas;Username=postgres;Password=postgres`.
Se verificaron en PostgreSQL el esquema `becas`, las columnas y tipos de las tablas del agregado, las tablas de Outbox/Inbox y el esquema `hangfire`.

Las pruebas de Testcontainers usan `postgres:17` y la imagen `testcontainers/ryuk:0.14.0` ya cacheada. La versión local de `dotnet ef` (`10.0.14`) es anterior a la de EF runtime (`10.0.15`); el aviso es no bloqueante.

## 5. Resultado

- **Antes:** 30 de 114 tareas completadas.
- **Después:** 37 de 114 tareas completadas.
- **Tareas cerradas:** `4.2`, `4.3`, `4.4`, `4.5`, `4.6`, `4.7` y `4.8`.
- **Tareas pendientes del bloque:** `4.9` a `4.12` (repositorios, bloqueos, migración en base limpia y prueba de integración desde cero).
- **Roadmaps:** no se marcó ningún RF de MS-BEC; estas tareas son infraestructura de persistencia y todavía no completan funcionalidad de negocio de extremo a extremo.
- **Capas:** `Ascenso.Becas.Domain` no incorporó EF Core, ASP.NET, MassTransit, Firebase, Keycloak ni Npgsql.
- **Estado local:** sin commit y sin `git push`; `ms-pos` y `ms-plt` intactos.

## 6. Siguiente bloque

Continuar con `4.9` (repositorios con reconstrucción del agregado), `4.10` (bloqueos pesimistas), `4.11` (migración sobre base limpia e idempotencia) y `4.12` (prueba de integración desde cero en Testcontainers). `4.11` no debe marcarse hasta repetir la validación sobre una base vacía.
