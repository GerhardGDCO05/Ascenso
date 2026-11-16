# Configuración faltante de `MS-PRG` en `MS-BEC`

Este documento describe la métrica, la alerta y el procedimiento operativo para
resolver una emisión de oferta bloqueada con
`422 CONFIGURACION_PROGRAMA_NO_DISPONIBLE`.

## 1. Señal observable

`MS-BEC` expone el medidor `Ascenso.Becas` con el contador:

```text
becas.configuracion_faltante{recurso="programa|costo_matricula|convenio"}
```

Cada vez que una emisión se bloquea porque la proyección local de `MS-PRG` no
existe o no está vigente, el contador se incrementa en una unidad con la
etiqueta `recurso`:

| `recurso` | Causa |
| :--- | :--- |
| `programa` | No existe `ProgramaBecaRef`, el programa no está `ACTIVO`/`CONFIGURADO`, su duración es inválida o no fue posible calcular un monto positivo. |
| `costo_matricula` | No existe un `CostoMatriculaRef` vigente para la carrera y el periodo, o su estado no es `VIGENTE`. |
| `convenio` | No existe `ConvenioRef` para la universidad aliada, su estado no es `VIGENTE` o su `FechaFin` ya pasó. |

Con el exportador de OpenTelemetry para Prometheus, el nombre se normaliza a
`becas_configuracion_faltante_total`.

## 2. Regla de alerta

```yaml
groups:
  - name: ms-bec-configuracion
    rules:
      - alert: BecasConfiguracionPrgFaltante
        expr: |
          sum by (recurso) (
            increase(becas_configuracion_faltante_total{
              recurso=~"programa|costo_matricula|convenio"
            }[10m])
          ) > 0
        for: 5m
        labels:
          severity: warning
          servicio: ms-bec
        annotations:
          summary: >-
            MS-BEC bloqueó emisiones por configuración faltante de MS-PRG
            (recurso={{ $labels.recurso }}).
          description: >-
            Revisar la proyección local y solicitar a MS-PRG la reemisión del
            evento correspondiente. Ver docs/operacion/configuracion-prg.md.
```

La alerta es de advertencia porque la emisión se bloquea de forma segura
(*fail-closed*): no se crea una beca, no se reserva presupuesto y no se publica
ningún evento.

## 3. Diagnóstico

1. Confirmar el `recurso` de la alerta y la ventana temporal.
2. Buscar en los logs la solicitud rechazada con
   `Codigo=CONFIGURACION_PROGRAMA_NO_DISPONIBLE` y su `correlationId`.
3. Consultar la proyección local con el identificador del programa, la carrera,
   el periodo y la universidad que reporta la respuesta de `MS-POS`:

```sql
SELECT programa_beca_id, estado, duracion_periodos,
       ultimo_evento_id, occurred_at, fecha_ultima_actualizacion
FROM becas.programa_beca_ref
WHERE programa_beca_id = :programaBecaId;

SELECT costo_matricula_referencia_id, estado, numero_version,
       fecha_vigencia_desde, fecha_vigencia_hasta,
       ultimo_evento_id, occurred_at, fecha_ultima_actualizacion
FROM becas.costo_matricula_ref
WHERE carrera_id = :carreraId
  AND periodo_academico_id = :periodoAcademicoId;

SELECT convenio_id, estado, fecha_fin,
       ultimo_evento_id, occurred_at, fecha_ultima_actualizacion
FROM becas.convenio_ref
WHERE universidad_aliada_id = :universidadAliadaId;
```

Si la fila no existe, `MS-BEC` nunca recibió el evento. Si existe pero está
desactualizada, el evento llegó antes que la corrección en `MS-PRG`.

## 4. Procedimiento de reemisión desde `MS-PRG`

1. Abrir un caso con el equipo de `MS-PRG` indicando:
   - `programaBecaId`, `carreraId`, `periodoAcademicoId` y
     `universidadAliadaId` afectados;
   - el `ultimo_evento_id` y `occurred_at` de la proyección local;
   - el `correlationId` de la emisión bloqueada;
   - el `recurso` que reporta la métrica.
2. Solicitar que `MS-PRG` corrija la configuración de origen y publique un
   **evento nuevo** en el intercambio correspondiente:
   - `Ascenso.Programas:ProgramaBecaConfigurado`
   - `Ascenso.Programas:CostoMatriculaActualizado`
   - `Ascenso.Programas:ConvenioUniversidadRegistrado`
   - `Ascenso.Programas:ConvenioUniversidadVencido`
3. El evento debe cumplir:
   - `eventId` nuevo y distinto de cualquier intento anterior;
   - el mismo `aggregateId` de la configuración corregida;
   - `occurredAt` posterior al `occurred_at` guardado en la proyección local;
   - el payload completo documentado en `design.md`, sin datos personales.
4. Verificar en RabbitMQ que el mensaje llegó a la cola de `MS-BEC`:
   - `ms-bec.programa-beca-configurado`
   - `ms-bec.costo-matricula-actualizado`
   - `ms-bec.convenio-universidad-registrado`
   - `ms-bec.convenio-universidad-vencido`
5. Confirmar que la proyección local cambió: el `ultimo_evento_id` y
   `occurred_at` deben corresponder al evento nuevo y
   `fecha_ultima_actualizacion` debe haberse movido.

Un evento con `occurredAt` anterior o igual al último aplicado se ignora de
forma deliberada para evitar retrocesos. En ese caso se debe solicitar otra
reemisión con una marca temporal posterior.

## 5. Reintento de la emisión

Con la proyección actualizada, repetir la emisión desde la interfaz o con la
misma `Idempotency-Key` de la solicitud original. El middleware de idempotencia
devuelve la respuesta previa si la operación ya se había completado; si la
emisión había fallado antes de persistir, se crea una única oferta nueva.

No se debe:

- insertar filas manualmente en `programa_beca_ref`, `costo_matricula_ref` o
  `convenio_ref`;
- desactivar la validación de configuración;
- reutilizar un `eventId` ya procesado.

## 6. Cierre del incidente

1. Confirmar que el contador deja de incrementarse para el `recurso`.
2. Verificar que la emisión responde `201` y que se publicó
   `OfertaBecaEmitida`.
3. Registrar en el caso el `eventId` reemitido y el `correlationId` del
   reintento exitoso.
