## How

Se completó la capa de persistencia de `MS-BEC` y se validó contra PostgreSQL real con Testcontainers.

### Convenciones y configuraciones de EF Core

- `ConfiguracionPersistencia`: auditoría común, `uuid` para identificadores y claves foráneas, `timestamptz` para fechas, `version` como token de concurrencia.
- Enumeraciones persistidas como `varchar` en MAYÚSCULAS mediante `ACodigo()` y lectura inversa con `DesdeCodigo<TEnum>()`.
- Montos `numeric(18,2)`, porcentajes `numeric(5,2)` y listas de valores para los `CHECK`.
- Configuraciones para `Beca`, `OfertaBeca`, `CompromisoBecario`, `HistorialEstadoBeca`, `ProgramaBecaRef`, `CostoMatriculaRef`, `ConvenioRef` e `IdempotencyRequest`, con nombres `snake_case` y restricciones explícitas.

### Índices únicos parciales

- `ux_beca_estudiante_cobertura_total_activa`: una beca `ACTIVA` con cobertura `TOTAL` por estudiante.
- `ux_beca_estudiante_periodo_no_acumulable_activa`: una beca `ACTIVA` no acumulable por estudiante y periodo.
- `ux_beca_cadena_periodo`: una beca por cadena y periodo vigente.
- `ux_oferta_beca_beca`: una oferta por beca.
- `ux_beca_codigo`: código de beca único.
- `ux_compromiso_becario_beca`, `ux_idempotency_request_endpoint_clave` e `ix_historial_estado_beca_beca_id`.

### Restricciones e inmutabilidad

- `CHECK` de estados permitidos, montos no negativos, porcentajes entre 0 y 100, promedios entre 0 y 20, fechas coherentes, periodos válidos y códigos HTTP entre 100 y 599.
- Función `becas.fn_historial_estado_beca_inmutable()` y trigger `trg_historial_estado_beca_inmutable`: `INSERT` permitido; `UPDATE` y `DELETE` rechazados con SQLSTATE `23000`.

### Proyecciones, mensajería y migración

- `ActualizadorProyeccionesRef`: upsert idempotente, actualiza solo si cambia `ultimo_evento_id` y `occurred_at` no retrocede; un evento repetido o anterior devuelve 0 filas.
- Outbox/Inbox de MassTransit y Hangfire en el esquema `hangfire` con `PrepareSchemaIfNecessary`.
- Migración `20261112051643_InitialCreate` con Designer y snapshot; script SQL de 350 líneas revisado.

### Pruebas de integración

- `PostgreSqlFixture` con Testcontainers `postgres:17` y migraciones aplicadas una vez.
- Pruebas de modelo y tipos, índices únicos, `CHECK`, inmutabilidad del historial, proyecciones idempotentes, idempotencia de comandos y mensajería/Hangfire.

### Bug corregido

La lectura inversa de enums usaba `Enum.Parse<TEnum>("OTORGADA")`, pero los nombres C# son PascalCase. Se reemplazó por `DesdeCodigo<TEnum>()`, que compara contra `ACodigo()` y conserva el contrato en MAYÚSCULAS.

### Validación ejecutada

```text
dotnet build -c Release --no-restore                 → 0 advertencias, 0 errores
Ascenso.Becas.Domain.Tests                           → 136/136 correctas
Ascenso.Becas.Application.Tests                      →   1/1 correcta
Ascenso.Becas.ArchitectureTests                      →   8/8 correctas
Ascenso.Becas.IntegrationTests                       → 108/108 correctas
Total                                                → 253/253 correctas
dotnet format --verify-no-changes                    → exit 0
dotnet ef migrations has-pending-model-changes       → sin cambios pendientes
dotnet ef database update                            → base al día
dotnet ef migrations script                          → script generado y revisado
```
