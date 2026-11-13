# Guía para escribir especificaciones OpenSpec

Esta guía reúne las reglas para redactar especificaciones duraderas (`spec.md`) en `openspec/specs/<microservicio>/`. Sustituye al borrador `openspec/specs/ejemplo.txt`, que se retiró del directorio de especificaciones activas.

## 1. Qué debe contener una especificación duradera

Una especificación duradera describe **qué hace el negocio**, no cómo se implementa. Debe ser estable entre cambios técnicos y servir como fuente de verdad para las pruebas y para los cambios de OpenSpec.

Estructura recomendada para OpenSpec 2.0.0:

```markdown
# Specification: <Nombre del microservicio>

## Purpose

<Breve descripción del bounded context y del valor de negocio que entrega.>

## Requirements

### Requirement: <Nombre del requisito>

<Una sola frase o párrafo corto con la regla observable. Usar SHALL/MUST para obligaciones.>

#### Scenario: <Nombre del escenario>

- **GIVEN** <contexto inicial>
- **WHEN** <acción o evento>
- **THEN** <resultado observable>
- **AND** <resultado adicional, si aplica>
```

Los escenarios son la forma de capturar ejemplos, casos borde y criterios de aceptación. Si un requisito incluye muchas condiciones, moverlas a escenarios `GIVEN/WHEN/THEN` en lugar de alargar el párrafo del requisito.

## 2. Reglas de oro

1. **Sin detalles de infraestructura:** no mencionar bases de datos, tablas, controladores, rutas HTTP ni librerías en la especificación duradera. Ese detalle pertenece a `design.md` del cambio.
2. **Vocabulario ubicuo:** los términos de negocio (`Beca`, `EstadoBeca`, `PoliticaCobertura`, etc.) deben coincidir exactamente con el glosario de `docs/Explicacion_del_DDD_V7.md` y con las clases del dominio en C#.
3. **Especificaciones atómicas:** si un microservicio acumula demasiados escenarios, dividir el archivo en subarchivos dentro de `openspec/specs/<microservicio>/` (por ejemplo, `otorgamiento.md`, `desembolsos.md`) para conservar el contexto.
4. **Una carpeta por microservicio:** no mezclar escenarios de contextos distintos en `openspec/specs/`.
5. **Idioma:** redactar en español, salvo las abstracciones técnicas que el glosario defina en inglés.

## 3. Relación con los cambios

- La especificación duradera vive en `openspec/specs/<microservicio>/`.
- Los cambios activos viven en `openspec/changes/[ID-Cambio]-[Integrante]-[Microservicio]/` y contienen `proposal.md`, `design.md`, `tasks.md` y, si aplica, los deltas de especificación.
- Al archivar un cambio, los escenarios nuevos o modificados se integran en la especificación duradera. El histórico conserva la bitácora y los artefactos del cambio.
- Un cambio técnico sin cambio de comportamiento puede archivarse con `skip_specs: true` en su `.openspec.yaml`.

## 4. Plantilla mínima

```markdown
# Specification: <Nombre del microservicio>

## Purpose

<Propósito del microservicio en dos o tres líneas.>

## Requirements

### Requirement: <Primera regla de negocio>

<Descripción corta y verificable.>

#### Scenario: <Caso principal>

- **GIVEN** <contexto>
- **WHEN** <acción>
- **THEN** <resultado esperado>

#### Scenario: <Caso borde>

- **GIVEN** <contexto alternativo>
- **WHEN** <acción>
- **THEN** <resultado esperado o error de negocio>
```
