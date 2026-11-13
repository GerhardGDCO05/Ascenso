## How

Se aplicó un criterio estricto: una tarea solo se marca si el entregable y la verificación descritos en ella están presentes y se ejecutaron.

### Tareas verificadas

- `12.10` — Validaciones locales de calidad:
  - `dotnet build --configuration Release --warnaserror` → 0 advertencias, 0 errores.
  - `dotnet test --configuration Release` → 59/59 correctas.
  - `dotnet format --verify-no-changes` → correcto.
- `14.4` — Validación de OpenSpec:
  - `openspec validate bootstrap-ms-beca --strict` → cambio válido.

### Tareas revisadas y no marcadas

- `1.3`: faltaban `Application.Tests`, `ArchitectureTests` e `IntegrationTests`.
- `1.4` a `1.9`: faltaban configuración centralizada, lock files, paquetes base y documentación reproducible.
- `3.1` a `3.11`: no existían los agregados de otorgamiento.
- `4.x` a `11.x`: persistencia, seguridad, aplicación, integraciones, API, Hangfire y observabilidad pendientes.
- `12.1` a `12.9` y `12.11`: dependían de suites y funcionalidades inexistentes.
- `13.x`: sin compose, Keycloak, Makefile, Dockerfile ni stubs.
- `14.1` a `14.3` y `14.5`: sin contratos publicados ni revisión de alcance con otros equipos.

### Resultado

```text
Antes:   9/114 tareas completadas
Después: 11/114 tareas completadas
Nuevas marcas: 12.10 y 14.4
```

### Validación ejecutada

```text
dotnet build --configuration Release --warnaserror  → 0 advertencias, 0 errores
dotnet test --configuration Release                 → 59/59 correctas
dotnet format --verify-no-changes                   → correcto
openspec validate bootstrap-ms-beca --strict        → cambio válido
```
