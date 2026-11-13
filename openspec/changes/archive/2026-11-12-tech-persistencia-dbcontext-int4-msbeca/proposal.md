## Why

Tras completar el modelo de dominio del otorgamiento, `MS-BEC` no tenía contexto de persistencia. La tarea `4.1` exigía crear `BecasDbContext` con los `DbSet` del agregado, las proyecciones locales y la idempotencia, y verificar que el contexto se construye con la cadena de PostgreSQL y el esquema `becas`.

## What Changes

- Crear `BecasDbContext` como contexto `sealed` con esquema por defecto `becas`.
- Declarar los `DbSet` de `Beca`, `OfertaBeca`, `CompromisoBecario`, `HistorialEstadoBeca`, proyecciones e idempotencia.
- Configurar las claves y los `OwnsOne` mínimos para que el modelo EF se construya.
- Crear los modelos de proyección `ProgramaBecaRef`, `CostoMatriculaRef`, `ConvenioRef` e `IdempotencyRequest`.
- Centralizar la versión de `Microsoft.EntityFrameworkCore.Relational` y eliminar la advertencia `MSB3277`.
- Agregar las pruebas de construcción del contexto y del modelo.
- Marcar la tarea `4.1` de `bootstrap-ms-beca`.

**Fuera de alcance:** configuración completa de nombres, tipos, índices, `CHECK` y trigger (tareas `4.2` a `4.5`).

## Capabilities

### New Capabilities

- Ninguna. Es infraestructura de persistencia.

### Modified Capabilities

- Ninguna.

## Impact

- **Código:** `Infrastructure/Persistence/` y `tests/Ascenso.Becas.IntegrationTests/Persistencia/`.
- **Dependencias:** referencia directa a `Microsoft.EntityFrameworkCore.Relational 10.0.15`.
- **Validación:** 149/149 pruebas correctas, build sin advertencias y formato correcto.
