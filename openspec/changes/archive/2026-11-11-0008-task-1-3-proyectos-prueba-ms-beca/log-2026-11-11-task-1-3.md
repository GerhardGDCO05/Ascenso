# Registro de Auditoría — Task 1.3: proyectos de prueba de MS-BECA

- **Fecha y hora de ejecución:** 11 de noviembre de 2026, 00:08 (UTC-4)
- **Rama local:** `Gerhard`
- **Alcance:** completar la tarea `1.3` de `openspec/changes/bootstrap-ms-beca/tasks.md`.
- **Trabajo local:** sin `git push` ni modificación de ramas remotas.

## 1. Objetivo

Crear `Ascenso.Becas.Domain.Tests`, `Ascenso.Becas.Application.Tests`, `Ascenso.Becas.ArchitectureTests` y `Ascenso.Becas.IntegrationTests`, agregarlos a la solución y verificar que `dotnet test` descubre los proyectos.

## 2. Cambios realizados

### Proyectos nuevos

- `Apps/services/ms-becas/tests/Ascenso.Becas.Application.Tests/`
  - `Ascenso.Becas.Application.Tests.csproj`
  - `Configuracion/EnsambladoAplicacionTests.cs`
- `Apps/services/ms-becas/tests/Ascenso.Becas.ArchitectureTests/`
  - `Ascenso.Becas.ArchitectureTests.csproj`
  - `Arquitectura/DependenciasEntreCapasTests.cs`
  - `Arquitectura/DomainIndependenceTests.cs`
- `Apps/services/ms-becas/tests/Ascenso.Becas.IntegrationTests/`
  - `Ascenso.Becas.IntegrationTests.csproj`
  - `Configuracion/EnsambladoApiTests.cs`

Los tres proyectos usan la misma configuración de xUnit que `Domain.Tests`: `Microsoft.NET.Test.Sdk 17.14.1`, `xunit 2.9.3`, `xunit.runner.visualstudio 3.1.4` y `coverlet.collector 6.0.4`.

### Solución

- `Ascenso.Becas.sln` ahora contiene los cuatro proyectos de prueba, anidados bajo la carpeta de solución `tests`.

### Pruebas de arquitectura

- `DomainIndependenceTests.cs` se movió de `Domain.Tests` a `ArchitectureTests`, que es su ubicación natural.
- Se agregó `DependenciasEntreCapasTests` para verificar que:
  - `Domain` no referencia `Application`, `Infrastructure` ni `Api`.
  - `Application` no referencia `Infrastructure` ni `Api`.
  - `Infrastructure` no referencia `Api`.

### Documentación

- `docs/organizacion_repositorio.md` ahora lista los cuatro proyectos de prueba y su responsabilidad.

## 3. Validación ejecutada

### Restauración

La restauración tardó aproximadamente 10 minutos porque `https://api.nuget.org/v3/index.json` no era accesible para consultar vulnerabilidades. Los paquetes se restauraron correctamente.

```text
dotnet restore Ascenso.Becas.sln → correcto
Advertencia NU1900                → origen de vulnerabilidades de NuGet inaccesible
```

### Compilación

```text
dotnet build Ascenso.Becas.sln --configuration Release
→ compilación correcta, 0 errores

dotnet build Ascenso.Becas.sln --configuration Release --warnaserror -p:NoWarn=NU1900
→ 0 advertencias, 0 errores
```

La única advertencia excluida es `NU1900`, causada por la falta de acceso a `nuget.org`; no proviene del código.

### Pruebas

```text
Ascenso.Becas.Domain.Tests         → 57/57 correctas
Ascenso.Becas.Application.Tests    →  1/1 correcta
Ascenso.Becas.ArchitectureTests    →  8/8 correctas
Ascenso.Becas.IntegrationTests     →  1/1 correcta
Total                              → 67/67 correctas
```

Los cuatro ensamblados aparecen en la salida de descubrimiento de `dotnet test`, por lo que la tarea `1.3` queda cumplida.

### Formato

```text
dotnet format Ascenso.Becas.sln --verify-no-changes --no-restore
→ correcto
```

## 4. Resultado

- **Antes:** 11 de 114 tareas completadas.
- **Después:** 12 de 114 tareas completadas.
- **Tarea cerrada:** `1.3`.

`12.10` se revalidó con el mismo conjunto de pruebas y formato. La salvedad es la advertencia `NU1900` de red, que no afecta al código ni a la compilación cuando se excluye explícitamente.

## 5. Estado local

- Sin commit y sin `git push`.
- `ms-pos` y `ms-plt` intactos.
- Siguiente tarea del plan: `1.4` (`Directory.Build.props`, `Directory.Packages.props`, `.editorconfig` y versiones centralizadas).
