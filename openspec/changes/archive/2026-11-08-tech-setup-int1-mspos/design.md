## How

Se replica exactamente la organización de `Apps/services/ms-becas/` renombrando el contexto de `Becas` a `Postulaciones`.

### Estructura de proyectos

```
Apps/services/ms-postulaciones/
├── Ascenso.Postulaciones.sln
├── Ascenso.Postulaciones.Domain/
│   ├── Ascenso.Postulaciones.Domain.csproj
│   ├── Comun/
│   ├── Enums/
│   ├── Excepciones/
│   ├── Ports/
│   ├── Transiciones/
│   ├── ValueObjects/
│   ├── Entities/
│   ├── Exceptions/
│   └── Repositories/
├── Ascenso.Postulaciones.Application/
│   ├── Ascenso.Postulaciones.Application.csproj
│   ├── Common/
│   ├── DTOs/
│   ├── Interfaces/
│   └── UseCases/
├── Ascenso.Postulaciones.Infrastructure/
│   ├── Ascenso.Postulaciones.Infrastructure.csproj
│   ├── Adapters/
│   ├── Configurations/
│   └── Persistence/
├── Ascenso.Postulaciones.Api/
│   ├── Ascenso.Postulaciones.Api.csproj
│   ├── appsettings.json
│   ├── Program.cs
│   ├── Properties/launchSettings.json
│   └── Controllers/
└── tests/
    └── Ascenso.Postulaciones.Domain.Tests/
        ├── Ascenso.Postulaciones.Domain.Tests.csproj
        ├── Arquitectura/
        ├── Comun/
        ├── Enums/
        ├── Excepciones/
        ├── Transiciones/
        └── ValueObjects/
```

### Decisiones de diseño

- **Namespaces y carpetas en PascalCase**, file-scoped namespaces (C# 10+), tal como exige `docs/convenciones_c_y_ddd.md`.
- **TargetFramework `net10.0`** alineado con `global.json` y el SDK instalado.
- **Referencias de proyecto:** `Api` → `Application` + `Infrastructure` + `Domain`; `Infrastructure` → `Application` + `Domain`; `Application` → `Domain`; `Domain.Tests` → `Domain`.
- **Carpetas vacías** con `.gitkeep` para preservar la estructura en el control de versiones.
- **Api** usa `Microsoft.NET.Sdk.Web` y expone `AddOpenApi()`; `Application`, `Domain` e `Infrastructure` usan `Microsoft.NET.Sdk`.

### Reglas aplicadas

- No hay setters públicos en propiedades mutables de `Domain/` (aún vacío, se respetará en cambios posteriores).
- No hay entidades ni agregados en inglés en `Domain/` (aún vacío).
- Toda tarea de persistencia asíncrona incluirá `CancellationToken` cuando se implemente.
