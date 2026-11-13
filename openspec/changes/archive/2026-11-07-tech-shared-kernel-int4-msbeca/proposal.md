## Why

El cambio activo `bootstrap-ms-beca` necesitaba la base táctica del dominio de `MS-BEC` antes de modelar los agregados de otorgamiento. Sin clases base, identificadores, eventos, excepciones, objetos de valor y puertos, cualquier entidad de negocio habría duplicado infraestructura transversal o habría acoplado `Domain` a frameworks externos.

## What Changes

- Implementar las clases base tácticas `EntidadAuditable`, `EntidadInmutable`, `AggregateRoot`, `ValueObject` y los eventos de dominio.
- Incorporar el generador de identificadores UUIDv7 y la abstracción temporal `IClock`.
- Crear los objetos de valor `PoliticaCobertura` y `CondicionesPermanencia` con sus validaciones.
- Crear las enumeraciones del dominio y la traducción `CodigoEnum.ACodigo()`.
- Crear el catálogo de excepciones de dominio con código y estatus HTTP.
- Definir los puertos de repositorio, unidad de trabajo, servicios externos y contratos auxiliares.
- Implementar las tablas de transición de estados de `Beca`, `OfertaBeca` y `CompromisoBecario`.
- Agregar las pruebas del shared kernel y verificar que `Domain` permanece como C# puro.

**Fuera de alcance:** agregados de negocio, persistencia, capa de aplicación, API e integraciones.

## Capabilities

### New Capabilities

- Ninguna. Es una base técnica transversal sin comportamiento de negocio completo.

### Modified Capabilities

- Ninguna.

## Impact

- **Código:** `Apps/services/ms-becas/Ascenso.Becas.Domain/` y `tests/Ascenso.Becas.Domain.Tests/`.
- **Especificaciones:** no se modifican; el histórico se archiva con `skip_specs: true`.
- **Dependencias:** ninguna nueva; `Domain` no referencia EF Core, ASP.NET Core, MassTransit, Firebase, Keycloak ni Npgsql.
- **Validación:** 59/59 pruebas correctas, build sin advertencias y `openspec validate bootstrap-ms-bec --strict` válido.
