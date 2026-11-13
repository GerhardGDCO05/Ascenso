## Why

El cambio `bootstrap-ms-beca` solo tenía el proyecto `Ascenso.Becas.Domain.Tests`. Faltaban las suites de aplicación, arquitectura e integración exigidas por la tarea `1.3`, y la prueba de independencia del dominio estaba ubicada en el proyecto equivocado.

## What Changes

- Crear `Ascenso.Becas.Application.Tests`, `Ascenso.Becas.ArchitectureTests` y `Ascenso.Becas.IntegrationTests`.
- Agregar los cuatro proyectos de prueba a `Ascenso.Becas.sln` bajo la carpeta de solución `tests`.
- Mover `DomainIndependenceTests` a `ArchitectureTests`.
- Agregar `DependenciasEntreCapasTests` para verificar las referencias permitidas entre capas.
- Actualizar `docs/organizacion_repositorio.md` con la responsabilidad de cada suite.
- Verificar que `dotnet test` descubre los cuatro ensamblados.

**Fuera de alcance:** implementar pruebas de negocio de las features 3 y 4 en adelante.

## Capabilities

### New Capabilities

- Ninguna. Es infraestructura de pruebas y arquitectura.

### Modified Capabilities

- Ninguna.

## Impact

- **Código:** tres proyectos nuevos en `Apps/services/ms-becas/tests/` y actualización de `Ascenso.Becas.sln`.
- **Documentación:** `docs/organizacion_repositorio.md`.
- **Validación:** 67/67 pruebas correctas (57 dominio, 1 aplicación, 8 arquitectura, 1 integración), build sin advertencias y formato correcto.
