## How

Se rediseñó el workflow para que el equipo no tenga que editar el pipeline al agregar un microservicio.

### Job `discover`

- Busca automáticamente archivos `.sln` y `.slnx` bajo `Apps/services/`.
- Excluye `bin/`, `obj/` y `node_modules/`.
- Publica una matriz JSON con `solution` y `name`.
- Si no encuentra soluciones, devuelve `[]` y evita ejecutar pruebas.

### Job `build-and-test`

- Se ejecuta una vez por solución mediante `strategy.matrix`.
- Usa `fail-fast: false` para que un microservicio con fallos no cancele los demás.
- Configura el SDK según `global.json` y usa caché de NuGet.
- Ejecuta `dotnet restore`, `dotnet build --configuration Release --warnaserror` y `dotnet test --no-build --no-restore`.
- Genera un `.trx` por solución y lo publica como artefacto con nombre único basado en la ruta.

### Disparadores

- `push` a cualquier rama (`'**'`), incluida `develop`.
- Cualquier `pull_request`.
- Ejecución manual con `workflow_dispatch`.

### Validación ejecutada

```text
Descubrimiento local de soluciones                    → 1 solución encontrada
Simulación con dos microservicios y un .sln en bin/   → 2 detectadas, 1 excluida
dotnet restore / build / test                         → 0 advertencias, 59/59 correctas
actionlint .github/workflows/ci-cd.yml                → sin observaciones
openspec validate bootstrap-ms-beca --strict          → cambio válido
git diff --check                                      → sin errores
```
