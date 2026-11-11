# Organización General del Repositorio — Plataforma Ascenso

Este documento describe la estructura compartida por todos los microservicios de Ascenso. La organización es general: cada equipo trabaja en la carpeta de su microservicio y reutiliza las mismas convenciones, sin mezclar código ni especificaciones de contextos ajenos.

## 1. Estructura de Alto Nivel

```text
Ascenso/
├── Apps/
│   └── services/
│       ├── ms-becas/             # MS-BECA — Ascenso.Becas (área de trabajo activa)
│       ├── ms-pos/               # MS-POS — Ascenso.Postulaciones (contexto de otro equipo)
│       ├── ms-plt/               # MS-PLT — Ascenso.Plataforma (contexto de otro equipo)
│       ├── ms-prg/               # MS-PRG — Ascenso.Programas (pendiente de su equipo)
│       └── ms-aud/               # MS-AUD — Ascenso.Auditoria (pendiente de su equipo)
├── docs/                         # Contexto general y documentación compartida
├── frontend/                     # Aplicación web
├── openspec/
│   ├── config.yaml               # Contexto y reglas de OpenSpec
│   ├── specs/                    # Especificaciones duraderas, una carpeta por microservicio
│   ├── changes/                  # Cambios activos
│   │   └── archive/              # Históricos por fecha y hora
│   └── roadmaps/                 # Planificación general por integrante y microservicio
└── .github/workflows/            # Pipelines de integración continua
```

## 2. Microservicios de la Plataforma

| Microservicio | Carpeta de código | Solución .NET | Especificación duradera |
| :--- | :--- | :--- | :--- |
| **MS-PLT** | `Apps/services/ms-plt/` | `Ascenso.Plataforma.sln` | `openspec/specs/ms-plt/spec_plt.md` (contexto) |
| **MS-PRG** | `Apps/services/ms-prg/` | `Ascenso.Programas.sln` | `openspec/specs/ms-prg/spec_prg.md` (pendiente) |
| **MS-POS** | `Apps/services/ms-pos/` | `Ascenso.Postulaciones.sln` | `openspec/specs/ms-pos/spec_pos.md` (contexto) |
| **MS-BECA** | `Apps/services/ms-becas/` | `Ascenso.Becas.sln` | `openspec/specs/ms-beca/spec_beca.md` (activo) |
| **MS-AUD** | `Apps/services/ms-aud/` | `Ascenso.Auditoria.sln` | `openspec/specs/ms-aud/spec_aud.md` (pendiente) |

Estado actual:

- `ms-beca` es el microservicio donde se desarrolla el trabajo del equipo.
- `ms-pos` y `ms-plt` existen únicamente como contexto de otros equipos; no se modifican.
- `ms-prg` y `ms-aud` se incorporarán cuando sus equipos creen sus carpetas y especificaciones.

## 3. Dónde Va Cada Cosa

| Contenido | Ubicación | Regla |
| :--- | :--- | :--- |
| Contexto general, PRD, DDD y convenciones | `docs/` | Fuente única compartida; no duplicar en `openspec/specs/`. |
| Especificaciones duraderas | `openspec/specs/<microservicio>/` | Una carpeta por microservicio; archivo principal `spec_<código>.md`. |
| Cambios activos | `openspec/changes/<acción>-<microservicio>/` | Solo el microservicio afectado. |
| Históricos de sesión | `openspec/changes/archive/<AAAA-MM-DD-HHMM>/` | Un registro por sesión o hito relevante. |
| Planificación general | `openspec/roadmaps/` | Roadmaps por integrante y por microservicio. |
| Código y pruebas | `Apps/services/<microservicio>/` | Cuatro proyectos hexagonales más los proyectos de prueba. |
| Pipeline | `.github/workflows/ci-cd.yml` | Descubre todas las soluciones bajo `Apps/services/` y ejecuta build + pruebas por microservicio. |

## 4. Reglas de Convivencia

1. **Aislamiento por microservicio:** ningún cambio de código, especificación o configuración debe modificar la carpeta de otro microservicio.
2. **Especificaciones separadas:** cada microservicio mantiene su propia carpeta en `openspec/specs/`; no se mezclan escenarios de contextos distintos.
3. **Contexto general único:** el PRD, la explicación del DDD y las convenciones viven en `docs/` y se referencian desde `openspec/config.yaml`.
4. **Cambios trazables:** los cambios se nombran `<acción>-<microservicio>` y los históricos se archivan con fecha y hora.
5. **Trabajo activo:** el equipo desarrolla sobre `ms-beca`; el resto de microservicios se consulta como contexto y se modifica solo cuando su equipo lo indique.
6. **Sin duplicación:** si un documento general ya existe en `docs/`, no se copia en `openspec/`.

## 5. Cómo Incorporar un Nuevo Microservicio

1. Crear `Apps/services/<microservicio>/` con su solución y los cuatro proyectos hexagonales.
2. Crear `openspec/specs/<microservicio>/spec_<código>.md` con el contexto y las invariantes del dominio.
3. Crear el cambio activo en `openspec/changes/<acción>-<microservicio>/`.
4. Definir la configuración del microservicio dentro de su carpeta (`Directory.Build.props`, `Directory.Packages.props` y `.editorconfig`) para no acoplar versiones ni reglas con otros equipos.
5. Generar los `packages.lock.json` y verificar que `dotnet restore --locked-mode` funciona.
6. Verificar que el workflow general de CI detecte la nueva solución. No hace falta editar `.github/workflows/ci-cd.yml`: descubre automáticamente los archivos `.sln` y `.slnx` bajo `Apps/services/`.
7. Actualizar los roadmaps generales si el microservicio asume nuevos RF.

## 6. Situación de MS-BECA

`ms-beca` es el área de trabajo activa. Su estructura es:

```text
Apps/services/ms-becas/
├── Ascenso.Becas.Domain/            # C# puro, sin dependencias externas
├── Ascenso.Becas.Application/       # Casos de uso, DTOs y puertos
├── Ascenso.Becas.Infrastructure/    # Adaptadores EF Core, mensajería, seguridad
├── Ascenso.Becas.Api/               # Controllers, middlewares y DI
└── tests/
    ├── Ascenso.Becas.Domain.Tests/        # Pruebas de dominio
    ├── Ascenso.Becas.Application.Tests/   # Pruebas de casos de uso y handlers
    ├── Ascenso.Becas.ArchitectureTests/   # Reglas de dependencia entre capas
    └── Ascenso.Becas.IntegrationTests/    # Persistencia, mensajería y API
```

El cambio activo es `bootstrap-ms-beca` y la especificación duradera vive en `openspec/specs/ms-beca/spec_beca.md`.
