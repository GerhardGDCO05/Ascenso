## Why

El repositorio Ascenso cuenta ya con el microservicio `ms-becas` implementado bajo arquitectura hexagonal y DDD en `Apps/services/ms-becas/`, pero no existe una contraparte para el microservicio de postulaciones (`ms-postulaciones`). Para que el equipo pueda comenzar a modelar el dominio de postulaciones, es necesario disponer de la estructura de carpetas y proyectos .NET base, idéntica a la de `ms-becas`, antes de escribir cualquier caso de uso o entidad.

## What Changes

- Crear la carpeta `Apps/services/ms-postulaciones/` con la solución .NET `Ascenso.Postulaciones.sln`.
- Replicar la arquitectura hexagonal estricta de `ms-becas`: proyectos `Domain`, `Application`, `Infrastructure` y `Api`, más el proyecto de pruebas `tests/Ascenso.Postulaciones.Domain.Tests`.
- Añadir las carpetas vacías (`.gitkeep`) de cada capa y subcapa conforme a las convenciones de C#/DDD del proyecto.
- Configurar referencias entre proyectos y targets `net10.0` alineados con `global.json`.

**Fuera de alcance de este cambio:** implementación de entidades, casos de uso, controladores, adaptadores de persistencia, migraciones EF Core o cualquier código de negocio. Solo se crea la esqueleto de carpetas y archivos de proyecto.

## Capabilities

### New Capabilities

- `ms-postulaciones/carpetas_estructura`: esqueleto de carpetas y proyectos del microservicio de postulaciones, listo para recibir el modelo de dominio en cambios posteriores.

### Modified Capabilities

- Ninguna.

## Impact

- **Código nuevo:** solución `Ascenso.Postulaciones.sln` y proyectos en `Apps/services/ms-postulaciones/`; no se modifica código existente.
- **Infraestructura:** ninguna (solo archivos de proyecto y carpetas vacías).
- **Integraciones:** ninguna en este corte.
