## How

Se completó la tarea `1.3` creando las suites de prueba que faltaban y ordenando las pruebas de arquitectura en su proyecto natural.

### Proyectos nuevos

- `tests/Ascenso.Becas.Application.Tests/`: `Ascenso.Becas.Application.Tests.csproj` y `Configuracion/EnsambladoAplicacionTests.cs`.
- `tests/Ascenso.Becas.ArchitectureTests/`: `Ascenso.Becas.ArchitectureTests.csproj`, `Arquitectura/DependenciasEntreCapasTests.cs` y `Arquitectura/DomainIndependenceTests.cs`.
- `tests/Ascenso.Becas.IntegrationTests/`: `Ascenso.Becas.IntegrationTests.csproj` y `Configuracion/EnsambladoApiTests.cs`.

Los tres proyectos usan la misma configuración de xUnit que `Domain.Tests`: `Microsoft.NET.Test.Sdk 17.14.1`, `xunit 2.9.3`, `xunit.runner.visualstudio 3.1.4` y `coverlet.collector 6.0.4`.

### Solución y arquitectura

- `Ascenso.Becas.sln` contiene los cuatro proyectos de prueba anidados bajo `tests`.
- `DomainIndependenceTests` se movió a `ArchitectureTests`.
- `DependenciasEntreCapasTests` verifica que `Domain` no referencia capas superiores, que `Application` no referencia `Infrastructure` ni `Api`, y que `Infrastructure` no referencia `Api`.

### Validación ejecutada

```text
dotnet restore Ascenso.Becas.sln                          → correcto (NU1900 por red)
dotnet build --configuration Release --warnaserror        → 0 advertencias, 0 errores
Ascenso.Becas.Domain.Tests                                → 57/57 correctas
Ascenso.Becas.Application.Tests                           →  1/1 correcta
Ascenso.Becas.ArchitectureTests                           →  8/8 correctas
Ascenso.Becas.IntegrationTests                            →  1/1 correcta
Total                                                     → 67/67 correctas
dotnet format --verify-no-changes                         → correcto
```

La advertencia `NU1900` provino de la falta de acceso al índice de vulnerabilidades de NuGet y no del código; se excluyó explícitamente en la compilación.
