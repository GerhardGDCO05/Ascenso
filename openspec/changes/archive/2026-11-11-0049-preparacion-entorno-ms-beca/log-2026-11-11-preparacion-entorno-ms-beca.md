# Registro de Auditoría — Tasks 1.4 a 1.9: preparación del entorno de MS-BECA

- **Fecha y hora de ejecución:** 11 de noviembre de 2026, 00:49 (UTC-4)
- **Rama local:** `Gerhard`
- **Alcance:** completar las tareas `1.4`, `1.5`, `1.6`, `1.7`, `1.8` y `1.9` de `openspec/changes/bootstrap-ms-beca/tasks.md`.
- **Trabajo local:** sin `git push` ni modificación de ramas remotas.

## 1. Objetivo

Dejar preparado el entorno del microservicio `ms-becas` con SDK fijado, configuración centralizada, versiones de paquetes administradas, lock files, análisis de código, formato, README reproducible y `dotnet-ef` instalado. La configuración se ubicó dentro de `Apps/services/ms-becas/` para no imponer propiedades ni versiones a `ms-pos` ni a `ms-plt`.

## 2. Cambios realizados

### Configuración centralizada de `ms-becas`

- `Apps/services/ms-becas/Directory.Build.props`
  - `net10.0`, `ImplicitUsings`, `Nullable`, `LangVersion`.
  - `TreatWarningsAsErrors=true`.
  - `AnalysisLevel=latest-recommended`, `AnalysisMode=Recommended`, `EnforceCodeStyleInBuild=true`.
  - `GenerateDocumentationFile=true` y `NoWarn=CS1591` para habilitar `IDE0005`.
  - `Deterministic=true`, `RestorePackagesWithLockFile=true`, `RestoreLockedMode` y `ContinuousIntegrationBuild` cuando `CI=true`.
  - Exclusión documentada de `NU1900` (auditoría sin red) y de `NU1902`/`NU1903` (ver incidencia de ImageSharp).
- `Apps/services/ms-becas/Directory.Packages.props`
  - Versiones centralizadas de los 33 paquetes utilizados.
  - EF Core `10.0.15`; Npgsql EF `10.0.3`; MassTransit `8.5.12`; Hangfire `1.8.25`; Hangfire.PostgreSql `1.21.1`; FluentValidation `12.2.0`; Serilog.AspNetCore `10.0.0`; OpenTelemetry `1.20.x`; PdfSharpCore `1.3.67`; Google.Cloud.Storage.V1 `5.0.0`; xUnit `2.9.3`; FluentAssertions `7.2.2`; NSubstitute `6.2.0`; NetArchTest.Rules `1.3.2`; Testcontainers `4.16.0`; WireMock.Net `2.30.0`; MassTransit.TestFramework `8.5.12`.
- `Apps/services/ms-becas/.editorconfig`
  - Fin de línea, indentación, estilo C#, nombres y severidad de analizadores.
  - Se agregó `GenerateDocumentationFile` para `IDE0005` y una exclusión puntual de `CA1711` para `CodigoEnum` (el nombre del tipo es intencional).
- `global.json` en la raíz: SDK `10.0.403` con `rollForward: latestPatch`.
- `.gitignore` en la raíz: ya existía y cubre artefactos de build, entornos y archivos de usuario.

### Proyectos y paquetes

- Los ocho `.csproj` usan `PackageReference` sin versión y heredan `Directory.Build.props` y `Directory.Packages.props`.
- Paquetes por capa:
  - `Domain`: sin dependencias externas.
  - `Application`: FluentValidation y FluentValidation.DependencyInjectionExtensions.
  - `Infrastructure`: EF Core, Npgsql, MassTransit (incluido EF Core y RabbitMQ), Hangfire, PdfSharpCore y Google Cloud Storage.
  - `Api`: OpenAPI, Serilog, OpenTelemetry y Hangfire.AspNetCore.
  - Pruebas: xUnit, FluentAssertions, NSubstitute, NetArchTest, Testcontainers, WireMock.Net y MassTransit.TestFramework.
- Se generaron los ocho `packages.lock.json` (uno por proyecto).

### CI

- `.github/workflows/ci-cd.yml` mantiene el descubrimiento general de soluciones.
- Restore con `--locked-mode`.
- Build con `--warnaserror`.
- Nuevo paso de `dotnet format --verify-no-changes --no-restore`.

### Documentación

- `README.md`:
  - Requisitos, estructura, comandos reproducibles y `dotnet-ef`.
  - Configuración centralizada y lock files.
  - Licencias de MassTransit y PdfSharpCore.
  - Nota sobre `SixLabors.ImageSharp 1.0.4`, dependencia transitiva de PdfSharpCore con avisos de seguridad conocidos.
- `docs/organizacion_repositorio.md`:
  - Se retiró la configuración específica de `ms-becas`.
  - Se dejó una regla general para cualquier microservicio y la sección “Situación de MS-BECA” como contexto.
- `openspec/changes/bootstrap-ms-beca/design.md`: se corrigió el nombre del paquete de pruebas de MassTransit a `MassTransit.TestFramework` (namespace `MassTransit.Testing`).
- `openspec/changes/bootstrap-ms-beca/tasks.md`: se marcaron `1.4` a `1.9` tras verificar cada criterio.

## 3. Validación ejecutada

### SDK y herramientas

```text
dotnet --version      → 10.0.403
dotnet ef --version   → 10.0.14
```

`~/.dotnet/tools` está en `PATH` y el comando de instalación quedó documentado en el README.

### Restore y lock files

```text
dotnet restore Ascenso.Becas.sln              → correcto
dotnet restore Ascenso.Becas.sln --locked-mode → correcto (dos veces)
```

Se calcularon los hashes SHA-256 de los ocho `packages.lock.json` antes y después de los dos restores bloqueados: no hubo cambios. También se verificó en una copia limpia que el restore normal no modifica los lock files.

### Build

```text
dotnet build Ascenso.Becas.sln --configuration Release --warnaserror
→ 0 advertencias, 0 errores
```

### Pruebas

```text
Ascenso.Becas.Domain.Tests         → 57/57 correctas
Ascenso.Becas.Application.Tests    →  1/1 correcta
Ascenso.Becas.ArchitectureTests    →  8/8 correctas
Ascenso.Becas.IntegrationTests     →  1/1 correcta
Total                              → 67/67 correctas
```

### Formato y análisis

```text
dotnet format Ascenso.Becas.sln --verify-no-changes --no-restore
→ correcto (exit 0)
```

### Copia limpia

Se copió el repositorio a `/tmp/opencode/ascenso-limpio` excluyendo `.git`, `bin`, `obj` y `node_modules`. En esa copia se ejecutaron los comandos documentados del README:

```text
dotnet restore ...                 → correcto
dotnet restore ... --locked-mode   → correcto
dotnet build ... --warnaserror     → 0 advertencias, 0 errores
dotnet test ...                    → 67/67 correctas
dotnet format ...                  → exit 0
```

### Workflow y OpenSpec

```text
actionlint .github/workflows/ci-cd.yml → OK
openspec validate bootstrap-ms-beca --strict → Change 'bootstrap-ms-beca' is valid
```

También se validaron el YAML del workflow y los XML de `Directory.Build.props`, `Directory.Packages.props` y los ocho `.csproj`.

## 4. Licencias verificadas

- **MassTransit 8.5.12:** Apache-2.0.
- **PdfSharpCore 1.3.67:** MIT.
- **FluentAssertions 7.2.2:** Apache-2.0.
- **Npgsql.EntityFrameworkCore.PostgreSQL 10.0.3:** PostgreSQL License.

## 5. Incidencias y decisiones

### IPv6 y NuGet

El DNS del equipo devolvía IPv6 primero y la red no tenía salida IPv6. NuGet se quedaba esperando hasta agotar el tiempo de 100 segundos. Para completar la restauración se usó un shim temporal `LD_PRELOAD` que filtra direcciones IPv6 solo para el proceso de restore. El shim vive en `/tmp/opencode/` y no forma parte del repositorio.

### `MassTransit.Testing` no existe como paquete

El identificador `MassTransit.Testing` no existe en NuGet. El harness de pruebas está incluido en el paquete `MassTransit` (namespace `MassTransit.Testing`); el paquete oficial que agrega las bases de prueba es `MassTransit.TestFramework`. Se usó `MassTransit.TestFramework 8.5.12`.

### ImageSharp 1.0.4

`PdfSharpCore 1.3.67` fija `SixLabors.ImageSharp 1.0.4`, que tiene avisos de seguridad conocidos y no tiene parche en la línea 1.x. No se actualizó a ImageSharp 2.x/3.x porque PdfSharpCore está compilado contra la API 1.x y las versiones nuevas no garantizan compatibilidad; además, PdfSharp 6.x usa licencia AGPL-3.0. Se excluyeron `NU1902`/`NU1903` del build con una nota en `Directory.Build.props` y en el README, y queda pendiente revisar la dependencia cuando PdfSharpCore publique una actualización.

## 6. Resultado

- **Antes:** 12 de 114 tareas completadas.
- **Después:** 18 de 114 tareas completadas.
- **Tareas cerradas:** `1.4`, `1.5`, `1.6`, `1.7`, `1.8`, `1.9`.
- **Revalidada:** `12.10` (formato, build y pruebas verdes).
- **Roadmaps:** no se marcó ningún RF de MS-BEC porque todavía no hay funcionalidad de negocio completa; los RF-15 a RF-27 siguen pendientes.

## 7. Estado local

- Sin commit y sin `git push`.
- `ms-pos` y `ms-plt` intactos.
- Siguiente bloque del plan: tareas `13.x` (compose, Keycloak, Makefile, Dockerfile y pipeline específico de MS-BECA).
