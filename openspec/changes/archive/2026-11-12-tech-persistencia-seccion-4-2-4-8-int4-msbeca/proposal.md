## Why

El contexto `BecasDbContext` de la tarea `4.1` era solo un esqueleto: faltaban las convenciones PostgreSQL, los índices únicos parciales, las restricciones `CHECK`, la inmutabilidad del historial, las proyecciones idempotentes, la idempotencia de comandos, las tablas de Outbox/Inbox y la migración inicial. Sin esas piezas, la persistencia no podía validarse contra PostgreSQL real ni sostener las invariantes del dominio.

## What Changes

- Implementar las convenciones y configuraciones de EF Core para las tablas del otorgamiento y las proyecciones.
- Crear los índices únicos parciales de no doble beneficio y unicidad de oferta, cadena y código.
- Crear las restricciones `CHECK` de estados, montos, porcentajes, fechas y periodos.
- Crear la función y el trigger que hacen inmutable el historial.
- Implementar el actualizador idempotente de proyecciones locales.
- Integrar Outbox/Inbox de MassTransit y Hangfire en el esquema `hangfire`.
- Generar y revisar la migración `InitialCreate` y su script SQL.
- Agregar las pruebas de integración con Testcontainers PostgreSQL 17.
- Documentar la base de datos local en el `README.md`.
- Corregir la lectura inversa de enums mediante `DesdeCodigo<TEnum>()`.
- Marcar las tareas `4.2` a `4.8` de `bootstrap-ms-beca`.

**Fuera de alcance:** repositorios, bloqueos pesimistas y migración sobre base limpia (`4.9` a `4.12`).

## Capabilities

### New Capabilities

- Ninguna. Es infraestructura de persistencia.

### Modified Capabilities

- Ninguna.

## Impact

- **Código:** `Infrastructure/Persistence/`, `Infrastructure/Dependencias/`, `Api/Program.cs` y `appsettings.json`.
- **Base de datos:** migración inicial con esquema `becas`, índices, `CHECK`, trigger y tablas de MassTransit; esquema `hangfire`.
- **Pruebas:** 108 pruebas de integración nuevas; total 253/253 correctas.
- **Documentación:** sección «Base de datos local» en el `README.md`.
