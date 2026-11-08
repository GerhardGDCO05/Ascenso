# Ascenso

Base del proyecto Ascenso. Incluye el entorno de desarrollo, el frontend en React y los microservicios en .NET organizados bajo `Apps/services`.

## Organización del repositorio

- `Apps/services/<microservicio>/`: código y pruebas de cada microservicio.
- `openspec/specs/<microservicio>/`: especificaciones duraderas por microservicio.
- `openspec/changes/`: cambios activos; históricos en `openspec/changes/archive/<fecha-hora>/`.
- `docs/`: contexto general (PRD, DDD, convenciones y organización).
- `openspec/roadmaps/`: planificación general por integrante y microservicio.
- `.github/workflows/ci-cd.yml`: descubre automáticamente las soluciones de `Apps/services/` y ejecuta build + pruebas de cada microservicio.

La estructura completa y las reglas para incorporar nuevos microservicios están en [`docs/organizacion_repositorio.md`](docs/organizacion_repositorio.md). El trabajo activo se desarrolla en `ms-beca`; `ms-pos` y `ms-plt` son contexto de otros equipos.

## Entorno de desarrollo

| Herramienta | Versión |
| :--- | :--- |
| .NET SDK | 10.0.403 (instalación local en `~/.dotnet`) |
| Entity Framework Core Tools | 10.0.14 |
| Node.js | 24.19.0 |
| npm | 11.17.0 |
| Docker | 29.7.2 |
| Docker Compose | 5.5.0 |

El SDK de .NET y `dotnet-ef` se cargan automáticamente en terminales nuevas mediante `~/.bashrc` y `~/.profile`. La versión del SDK está fijada en `global.json`.

