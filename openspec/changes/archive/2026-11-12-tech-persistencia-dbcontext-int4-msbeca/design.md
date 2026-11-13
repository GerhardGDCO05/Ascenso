## How

Se creó el contexto de persistencia mínimo de `MS-BEC` dentro de `Ascenso.Becas.Infrastructure`, manteniendo `Domain` como C# puro.

### Contexto de persistencia

- `BecasDbContext` `sealed` con `HasDefaultSchema("becas")`.
- `DbSet<Beca>` → `Becas`, `DbSet<OfertaBeca>` → `OfertasBeca`, `DbSet<CompromisoBecario>` → `CompromisosBecarios`, `DbSet<HistorialEstadoBeca>` → `HistorialEstadosBeca`.
- `DbSet<ProgramaBecaRef>`, `DbSet<CostoMatriculaRef>`, `DbSet<ConvenioRef>` y `DbSet<IdempotencyRequest>`.
- `ApplyConfigurationsFromAssembly` para las configuraciones de `4.2` a `4.7`.

### Configuración mínima del modelo

- Claves primarias de historial, proyecciones e idempotencia.
- `OwnsOne` explícito para `Beca.CoberturaAplicada`, `OfertaBeca.CondicionesSnapshot`, `ProgramaBecaRef.PoliticaCobertura` y `ProgramaBecaRef.CondicionesPermanencia`.
- La semántica relacional completa queda para las tareas `4.2` a `4.5`.

### Modelos de proyección e idempotencia

- `ProgramaBecaRef`: programa, política, condiciones, acumulación, duración, estado y marcas de actualización.
- `CostoMatriculaRef`: carrera, periodo, monto, moneda, versión, vigencia y estado.
- `ConvenioRef`: universidad, convenio, estado, fecha de fin y marcas de actualización.
- `IdempotencyRequest`: endpoint, clave, hash del payload, estado, respuesta, recurso, creación y expiración.

### Decisiones de diseño

- **Persistencia en Infrastructure:** el contexto y las proyecciones no contaminan `Domain`.
- **Modelo mínimo:** `4.1` solo garantiza que el modelo EF se construye.
- **`OwnsOne` explícito:** los objetos de valor no tienen setters públicos y se mapean propiedad por propiedad.
- **Claves naturales:** las proyecciones usan su identidad natural; el historial usa `Id`.
- **Versión alineada:** la referencia directa a EF Core Relational evita que Npgsql baje la versión transitiva.

### Validación ejecutada

```text
dotnet restore --ignore-failed-sources -p:NuGetAudit=false → 8 proyectos, lock files actualizados
dotnet build -c Release --no-restore                       → 0 advertencias, 0 errores
Ascenso.Becas.Domain.Tests                                 → 136/136 correctas
Ascenso.Becas.Application.Tests                            →   1/1 correcta
Ascenso.Becas.ArchitectureTests                            →   8/8 correctas
Ascenso.Becas.IntegrationTests                             →   4/4 correctas
Total                                                      → 149/149 correctas
dotnet format --verify-no-changes                          → correcto
```
