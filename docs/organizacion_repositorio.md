# Organización General del Repositorio — Plataforma Ascenso

Este documento describe la estructura compartida por todos los microservicios de Ascenso. La organización es general: cada equipo trabaja en la carpeta de su microservicio y reutiliza las mismas convenciones, sin mezclar código ni especificaciones de contextos ajenos.

## 1. Estructura de Alto Nivel

```text
Ascenso/
├── Apps/
│   └── services/
│       ├── ms-becas/             # MS-BECA — Ascenso.Becas (equipo de otorgamiento)
│       ├── ms-postulaciones/     # MS-POS — Ascenso.Postulaciones (equipo de admisión)
│       ├── ms-plt/               # MS-PLT — Ascenso.Plataforma (pendiente)
│       ├── ms-prg/               # MS-PRG — Ascenso.Programas (pendiente)
│       └── ms-aud/               # MS-AUD — Ascenso.Auditoria (pendiente)
├── docs/                         # Contexto general y documentación compartida
├── frontend/                     # Aplicación web
├── openspec/
│   ├── config.yaml               # Contexto y reglas de OpenSpec
│   ├── specs/                    # Especificaciones duraderas, una carpeta por microservicio
│   ├── changes/                  # Cambios activos
│   │   └── archive/              # Históricos con fecha, cambio, integrante y microservicio
│   └── roadmaps/                 # Planificación general por integrante y microservicio
└── .github/workflows/            # Pipelines de integración continua
```

## 2. Microservicios de la Plataforma

| Microservicio | Carpeta de código | Solución .NET | Especificación duradera |
| :--- | :--- | :--- | :--- |
| **MS-PLT** | `Apps/services/ms-plt/` | `Ascenso.Plataforma.sln` | `openspec/specs/ms-plt/spec_plt.md` (contexto) |
| **MS-PRG** | `Apps/services/ms-prg/` | `Ascenso.Programas.sln` | `openspec/specs/ms-prg/spec_prg.md` |
| **MS-POS** | `Apps/services/ms-postulaciones/` | `Ascenso.Postulaciones.sln` | `openspec/specs/ms-pos/spec_pos.md` |
| **MS-BECA** | `Apps/services/ms-becas/` | `Ascenso.Becas.sln` | `openspec/specs/ms-beca/spec_beca.md` |
| **MS-AUD** | `Apps/services/ms-aud/` | `Ascenso.Auditoria.sln` | `openspec/specs/ms-aud/spec_aud.md` (pendiente) |

Estado actual:

- `ms-becas` y `ms-postulaciones` cuentan con estructura de código y equipos activos.
- `ms-plt`, `ms-prg` y `ms-aud` se incorporarán cuando sus equipos creen sus carpetas, soluciones y especificaciones.
- Cada equipo es responsable de su carpeta; los cambios que afecten a otro microservicio se coordinan con su equipo y se documentan en el cambio correspondiente.

## 3. Dónde Va Cada Cosa

| Contenido | Ubicación | Regla |
| :--- | :--- | :--- |
| Contexto general, PRD, DDD y convenciones | `docs/` | Fuente única compartida; no duplicar en `openspec/specs/`. |
| Especificaciones duraderas | `openspec/specs/<microservicio>/` | Una carpeta por microservicio; archivo principal `spec_<código>.md`. |
| Cambios activos | `openspec/changes/[ID-Cambio]-[Integrante]-[Microservicio]/` | Solo el microservicio afectado. |
| Históricos de sesión | `openspec/changes/archive/YYYY-MM-DD-[ID-Cambio]-[Integrante]-[Microservicio]/` | Sin hora `HHMM`; incluye `.openspec.yaml`, `proposal.md`, `design.md`, `tasks.md` y `log-...md`. |
| Planificación general | `openspec/roadmaps/` | Roadmaps por integrante y por microservicio. |
| Código y pruebas | `Apps/services/<microservicio>/` | Cuatro proyectos hexagonales más los proyectos de prueba. |
| Pipeline | `.github/workflows/ci-cd.yml` | Descubre todas las soluciones bajo `Apps/services/` y ejecuta build + pruebas por microservicio. |

### 3.1 Convención de nombres de cambios

- `[ID-Cambio]`: `rfXX` para una historia de negocio (por ejemplo, `rf15`) o uno de los identificadores técnicos `tech`, `test`, `chore` o `fix`, con un sufijo descriptivo corto cuando haga falta para evitar colisiones (por ejemplo, `tech-shared-kernel`).
- `[Integrante]`: identificador del responsable según `openspec/roadmaps/por_integrante.md` (`int1`, `int2`, `int3`, `int4` o `int5`).
- `[Microservicio]`: código compacto del microservicio (`msbeca`, `mspos`, `msplt`, `msprg`, `msaud`).
- Ejemplo: `2026-11-08-tech-setup-int1-mspos`.
- Ejemplo de histórico: `2026-11-07-tech-shared-kernel-int4-msbeca`.
- Ejemplo de cambio activo: `tech-bootstrap-int4-msbeca`.

## 4. Reglas de Convivencia

1. **Aislamiento por microservicio:** cada equipo modifica su carpeta de código, su especificación y su configuración. Los cambios que crucen límites se coordinan con el equipo responsable.
2. **Especificaciones separadas:** cada microservicio mantiene su propia carpeta en `openspec/specs/`; no se mezclan escenarios de contextos distintos.
3. **Contexto general único:** el PRD, la explicación del DDD y las convenciones viven en `docs/` y se referencian desde `openspec/config.yaml`.
4. **Cambios trazables:** los cambios se nombran `[ID-Cambio]-[Integrante]-[Microservicio]` y los históricos se archivan como `YYYY-MM-DD-[ID-Cambio]-[Integrante]-[Microservicio]`, sin hora.
5. **Trabajo activo:** cada equipo desarrolla en su microservicio; el trabajo compartido se documenta en `docs/` y en los roadmaps.
6. **Sin duplicación:** si un documento general ya existe en `docs/`, no se copia en `openspec/`.
7. **Archivado completo:** antes de archivar un cambio se verifica que tenga `.openspec.yaml`, `proposal.md`, `design.md`, `tasks.md` con todas las tareas en `[x]` y su bitácora `log-...md`.

## 5. Cómo Incorporar un Nuevo Microservicio

1. Crear `Apps/services/<microservicio>/` con su solución y los cuatro proyectos hexagonales.
2. Crear `openspec/specs/<microservicio>/spec_<código>.md` con el contexto y las invariantes del dominio.
3. Crear el cambio activo en `openspec/changes/[ID-Cambio]-[Integrante]-[Microservicio]/`.
4. Definir la configuración del microservicio dentro de su carpeta (`Directory.Build.props`, `Directory.Packages.props` y `.editorconfig`) para no acoplar versiones ni reglas con otros equipos.
5. Generar los `packages.lock.json` y verificar que `dotnet restore --locked-mode` funciona.
6. Verificar que el workflow general de CI detecte la nueva solución. No hace falta editar `.github/workflows/ci-cd.yml`: descubre automáticamente los archivos `.sln` y `.slnx` bajo `Apps/services/`.
7. Actualizar los roadmaps generales si el microservicio asume nuevos RF.

## 6. Situación de MS-BECA

`ms-beca` es uno de los microservicios activos. Su estructura es:

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

El cambio activo es `tech-bootstrap-int4-msbeca` y la especificación duradera vive en `openspec/specs/ms-beca/spec_beca.md`.
