# Registro de Auditoría — CI/CD general por microservicio

- **Fecha y hora de ejecución:** 8 de noviembre de 2026, 16:53 (UTC-4)
- **Rama local:** `Gerhard`
- **Alcance:** generalizar `.github/workflows/ci-cd.yml` para que detecte y pruebe automáticamente cualquier microservicio agregado o actualizado por el equipo, con vistas a integrarlo en `develop`.
- **Trabajo local:** sin `git push` ni modificación de ramas remotas.

## 1. Necesidad atendida

El workflow anterior estaba atado explícitamente a `Apps/services/ms-becas/Ascenso.Becas.sln`. Se solicitó un pipeline general que, al subirse a `develop`, ejecute las pruebas de cualquier microservicio que el equipo actualice en la rama, sin editar el archivo por cada microservicio.

## 2. Diseño del workflow general

Archivo: `.github/workflows/ci-cd.yml`

- **Nombre:** `CI - Microservicios Ascenso`.
- **Disparadores:**
  - `push` a cualquier rama (`'**'`), incluida `develop`.
  - Cualquier `pull_request`.
  - Ejecución manual con `workflow_dispatch`.
- **Job `discover`:**
  - Busca automáticamente archivos `.sln` y `.slnx` bajo `Apps/services/`.
  - Excluye `bin/`, `obj/` y `node_modules/`.
  - Publica la lista de soluciones como una matriz JSON con `solution` y `name`.
  - Si no encuentra soluciones, devuelve `[]` y evita ejecutar pruebas.
- **Job `build-and-test`:**
  - Se ejecuta una vez por cada solución descubierta mediante `strategy.matrix`.
  - Usa `fail-fast: false` para que un microservicio con fallos no cancele los demás.
  - Configura el SDK según `global.json` y usa caché de NuGet.
  - Ejecuta `dotnet restore`, `dotnet build --configuration Release --warnaserror` y `dotnet test --no-build --no-restore`.
  - Genera un archivo `.trx` por solución y lo publica como artefacto con nombre único basado en la ruta de la solución.
- **Generalidad:** al agregar un nuevo microservicio con su `.sln` o `.slnx` bajo `Apps/services/`, el pipeline lo detecta y lo prueba sin modificar el workflow.

## 3. Documentación actualizada

- `docs/organizacion_repositorio.md`:
  - La fila del pipeline ahora indica que descubre todas las soluciones y ejecuta build + pruebas por microservicio.
  - El procedimiento para incorporar un microservicio aclara que no hace falta editar el workflow.
- `README.md`: se agregó la descripción del workflow general.
- `openspec/config.yaml`: se documentó que el CI descubre automáticamente las soluciones `.sln`/`.slnx`.

## 4. Validación ejecutada

```text
Descubrimiento local de soluciones                         → 1 solución encontrada
JSON de matriz                                             → [{"solution":"Apps/services/ms-becas/Ascenso.Becas.sln","name":"Apps-services-ms-becas-Ascenso.Becas"}]
Simulación con dos microservicios y un .sln en bin/        → 2 soluciones detectadas, 1 excluida
dotnet restore Apps/services/ms-becas/Ascenso.Becas.sln    → correcto
dotnet build ... --configuration Release --warnaserror     → 0 advertencias, 0 errores
dotnet test ... --configuration Release                    → 59/59 pruebas correctas
Archivo .trx generado                                      → /tmp/opencode/ci-test-results/test-results.trx
actionlint sobre .github/workflows/ci-cd.yml               → sin observaciones
YAML de openspec/config.yaml y ci-cd.yml                   → correcto
openspec validate bootstrap-ms-beca --strict               → cambio válido
git diff --check                                           → sin errores
```

## 5. Estado local

- Cambios locales sin commit y sin `git push`.
- `ms-pos` y `ms-plt` permanecen intactos.
- El pipeline queda listo para integrarse en `develop`; a partir de ahí probará automáticamente las soluciones de todos los microservicios que se agreguen.
