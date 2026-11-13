## How

Se separó la organización general del repositorio de la situación particular de `ms-beca`, sin alterar el código ni las especificaciones de otros equipos.

### Documento general de organización

Se creó `docs/organizacion_repositorio.md` con:

- Estructura de alto nivel (`Apps/services/`, `docs/`, `openspec/`, `frontend/`, `.github/`).
- Tabla de los cinco microservicios y sus rutas de código y especificación.
- Ubicación de especificaciones, cambios, históricos, roadmaps y pipelines.
- Reglas de aislamiento por microservicio y de no duplicación documental.
- Procedimiento para incorporar un nuevo microservicio.
- Situación actual de `ms-beca` como área de trabajo activa.

### Convenciones generalizadas

- La estructura de proyectos dejó de estar fijada solo a `ms-becas`.
- Se muestra `Apps/services/` con `ms-becas` como ejemplo y `ms-<otro>/` como plantilla.
- Se aclara que cada equipo reemplaza `Becas` por el contexto de su microservicio.

### Configuración de OpenSpec y README

- `openspec/config.yaml` incorpora `docs/organizacion_repositorio.md` como contexto.
- La estructura hexagonal se describe como `Apps/services/<microservicio>/` y `Ascenso.<Contexto>.*`.
- Se agrega la sección de organización por microservicio y se corrigen las referencias a los roadmaps.
- El `README.md` enlaza la guía general e indica que el trabajo activo está en `ms-beca`.

### Validación ejecutada

```text
python3 yaml.safe_load(openspec/config.yaml)          → YAML OK
python3 yaml.safe_load(.github/workflows/ci-cd.yml)   → YAML OK
openspec validate bootstrap-ms-beca --strict           → cambio válido
dotnet test --configuration Release                    → 59/59 correctas
git diff --check                                       → sin errores
```
