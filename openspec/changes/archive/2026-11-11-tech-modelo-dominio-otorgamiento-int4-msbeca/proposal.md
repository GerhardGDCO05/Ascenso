## Why

El shared kernel de `MS-BEC` no incluía los agregados de negocio del otorgamiento. Sin `Beca`, `OfertaBeca`, `CompromisoBecario` e `HistorialEstadoBeca`, no era posible implementar los casos de uso ni las reglas de los RF-15, RF-16 y RF-17, y el dominio carecía de la máquina de estados y las invariantes monetarias exigidas por la especificación maestra.

## What Changes

- Implementar la raíz `Beca` con fábrica `CrearOtorgada`, máquina de estados, snapshot de cobertura, activación, suspensión, renovación, conclusión y revocación.
- Implementar `OfertaBeca` con vencimiento a 15 días, hash de carta, montos y respuestas terminales.
- Implementar `CompromisoBecario` con carga, revisión, aprobación, rechazo y control de intentos.
- Implementar `HistorialEstadoBeca` como registro inmutable append-only.
- Implementar `CalculadoraMontoOferta`, `PoliticaNoDobleBeneficio` y `ContextoOperacion`.
- Agregar los eventos de dominio del otorgamiento.
- Agregar las pruebas de agregados, transiciones, políticas, cálculo y eventos.
- Marcar las tareas `3.1` a `3.11` de `bootstrap-ms-beca`.

**Fuera de alcance:** persistencia, capa de aplicación, API e integraciones externas.

## Capabilities

### New Capabilities

- Ninguna. El comportamiento queda cubierto por la especificación duradera `ms-beca` y se implementa en un cambio técnico posterior.

### Modified Capabilities

- Ninguna.

## Impact

- **Código:** `Domain/Otorgamiento/` y nuevas pruebas en `tests/Ascenso.Becas.Domain.Tests/Otorgamiento/`.
- **Dependencias:** ninguna nueva; `Domain` sigue siendo C# puro.
- **Validación:** 146/146 pruebas correctas, build sin advertencias y formato correcto.
