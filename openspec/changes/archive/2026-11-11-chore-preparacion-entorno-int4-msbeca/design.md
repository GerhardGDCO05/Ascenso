## How

Se dejó preparado el entorno de `ms-becas` con configuración local al microservicio, sin acoplar versiones ni propiedades a otros equipos.

### Configuración centralizada

- `Directory.Build.props`: `net10.0`, `ImplicitUsings`, `Nullable`, `LangVersion`, `TreatWarningsAsErrors`, análisis recomendado, `GenerateDocumentationFile`, build determinista y lock files.
- `Directory.Packages.props`: versiones centralizadas de 33 paquetes (EF Core 10.0.15, Npgsql EF 10.0.3, MassTransit 8.5.12, Hangfire 1.8.25, FluentValidation 12.2.0, Serilog 10.0.0, OpenTelemetry 1.20.x, PdfSharpCore 1.3.67, xUnit 2.9.3, Testcontainers 4.16.0, WireMock.Net 2.30.0, entre otros).
- `.editorconfig`: estilo C#, nombres, fin de línea, indentación y severidad de analizadores; exclusión puntual de `CA1711` para `CodigoEnum`.
- `global.json`: SDK `10.0.403` con `rollForward: latestPatch`.

### Paquetes y lock files

- `Domain` permanece sin dependencias externas.
- `Application` usa FluentValidation; `Infrastructure` usa EF Core, Npgsql, MassTransit, Hangfire, PdfSharpCore y Google Cloud Storage; `Api` usa OpenAPI, Serilog, OpenTelemetry y Hangfire.
- Se generaron los ocho `packages.lock.json` y se verificó que dos restores con `--locked-mode` no modifican sus hashes.

### CI y documentación

- El workflow usa `dotnet restore --locked-mode`, `dotnet build --warnaserror` y `dotnet format --verify-no-changes --no-restore`.
- El `README.md` documenta requisitos, estructura, comandos reproducibles, licencias e instalación de `dotnet-ef`.
- Se corrigieron `docs/organizacion_repositorio.md` y el nombre `MassTransit.TestFramework` en `design.md`.

### Incidencias y decisiones

- **IPv6 y NuGet:** se usó un shim temporal `LD_PRELOAD` en `/tmp/opencode/` para filtrar IPv6 durante el restore; no forma parte del repositorio.
- **MassTransit.Testing:** no existe como paquete; se usó `MassTransit.TestFramework 8.5.12`.
- **ImageSharp 1.0.4:** dependencia transitiva de PdfSharpCore con avisos de seguridad; se excluyeron `NU1902`/`NU1903` con nota en `Directory.Build.props` y README, y se dejó pendiente revisar cuando PdfSharpCore actualice.

### Validación ejecutada

```text
dotnet --version                                  → 10.0.403
dotnet ef --version                               → 10.0.14
dotnet restore --locked-mode (dos veces)          → correcto, hashes sin cambios
dotnet build --configuration Release --warnaserror → 0 advertencias, 0 errores
dotnet test --configuration Release               → 67/67 correctas
dotnet format --verify-no-changes                 → correcto
Copia limpia en /tmp/opencode/ascenso-limpio      → restore, build, test y format correctos
actionlint .github/workflows/ci-cd.yml            → OK
openspec validate bootstrap-ms-beca --strict      → cambio válido
```
