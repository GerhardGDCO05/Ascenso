## Context

La sección 6 se implementó sobre el dominio, la persistencia y las preocupaciones transversales de las secciones 1–5. Su objetivo fue conectar esos bloques en casos de uso ejecutables sin acoplar `Application` a clientes HTTP, almacenamiento, PDF, Hangfire ni mensajería, que pertenecen a las secciones 7–10. Las dependencias posteriores se representaron con puertos y dobles de prueba.

## Decisions

### CQRS-Lite y despacho

- Se definieron contratos `ICommand`, `ICommand<T>`, `ICommandHandler<>`, `ICommandHandler<,>`, `IQuery<T>` e `IQueryHandler<,>`.
- `CommandDispatcher` y `QueryDispatcher` validan con FluentValidation antes de invocar el handler; si no existe handler lanzan `HandlerNoEncontradoException` con `HANDLER_NO_ENCONTRADO`.
- `RegistroAplicacion.AgregarAplicacion()` registra validadores, despachadores y handlers escaneando el ensamblado de aplicación.

### DTOs y validación

- Los DTOs de escritura y lectura exponen solo los datos necesarios para cada caso de uso.
- Las consultas internas devuelven identificadores, estados, cobertura y `permiteAcumulacion`; no incluyen nombres, documentos, cuentas, bancos, correos ni teléfonos.
- El validador de carga de compromiso tolera contenido nulo, vacío, extensión y MIME inválidos sin lanzar `NullReferenceException`.

### Unidad de trabajo e idempotencia

- `IUnitOfWork` se amplió con `NivelAislamientoTransaccion` y una sobrecarga que recibe el nivel de aislamiento.
- `UnidadDeTrabajo` reutiliza la transacción ambiental abierta por el middleware de idempotencia; solo abre una nueva cuando no existe.
- Si no existe transacción ambiental y se solicita `Serializable`, se abre con ese nivel; en caso contrario se usa `ReadCommitted`.
- Cuando la transacción es ambiental, la unidad de trabajo no confirma ni revierte: el middleware de idempotencia conserva el control del commit y del rollback.

### Emisión de oferta

- `EmitirOfertaBecaHandler` consulta adjudicabilidad a `MS-POS` y rechaza la emisión si la postulación no es adjudicable.
- Valida que existan proyecciones locales de programa, costo de matrícula y convenio, y que estén vigentes para la fecha de operación.
- Calcula el monto por periodo y el monto total con el servicio de dominio; rechaza montos no positivos.
- Solicita la reserva a `MS-PRG` con referencia `OFERTA_BECA` y acepta únicamente estados `PENDIENTE` o `CONFIRMADA`, con `ReservaId` válido, monto suficiente y al menos un cupo.
- Genera la carta PDF, la sube al almacenamiento y programa la expiración antes de la transacción local.
- Crea el agregado `Beca`, encola los eventos de dominio y persiste con Outbox.
- Si falla una etapa posterior a la subida o a la programación, elimina el archivo y cancela el job como compensación best-effort.

### Aceptación, rechazo, anulación y expiración

- La aceptación se ejecuta en transacción `Serializable`, adquiere bloqueo por estudiante y bloqueo de fila, valida vencimiento y no doble beneficio, y activa la beca.
- El rechazo y la anulación cierran la oferta y la beca con el `tipoCierre` correspondiente; la anulación exige justificación mínima.
- La expiración relee y bloquea la fila, no modifica ofertas ya respondidas y solo expira ofertas pendientes vencidas.
- Todos los handlers encolan los eventos antes de `GuardarCambiosAsync`.

### Compromiso del becario

- La carga valida MIME PDF, extensión, bytes mágicos `%PDF-`, tamaño máximo de 5 MB, hash SHA-256 e intento.
- El archivo se sube antes de la transacción local; si la persistencia falla o el hash no coincide, se elimina el archivo.
- La revisión aprueba o rechaza el compromiso bajo bloqueo y actualiza `habilitadaParaDesembolso`.

### Consultas

- `ObtenerOferta`, `ObtenerBeca` y `ConsultarBecasActivas` usan el modelo de lectura de infraestructura.
- Si el recurso no existe, los handlers lanzan `RecursoNoEncontradoException`.

### Outbox

- `IOutboxBeca` representa la publicación transaccional. En esta sección los handlers solo encolan eventos; la implementación real corresponde a la sección 7.
- Las pruebas confirman que un fallo de `SaveChanges` descarta los eventos y no deja eventos confirmados.

## Risks / Trade-offs

- La compensación de archivo y job es best-effort; si la compensación falla, se prioriza la excepción original y la reconciliación posterior debe cubrir el residuo.
- La reserva de presupuesto no se libera en esta sección porque el puerto de `MS-PRG` aún no expone esa operación; se completará con la integración real.
- El Outbox de prueba acumula eventos y confirma solo al commit; la atomicidad real se verificará en la sección 7.
- Los dobles de repositorio devuelven la misma instancia del agregado, por lo que las aserciones de eventos filtran por tipo para no confundir eventos históricos con eventos de la transición actual.

## Migration Plan

No hay migraciones nuevas. Se reutilizan las tablas de la sección 4 y no se modifica el modelo persistido.

## Open Questions

- Ninguna para la sección 6. Las secciones 7–14 continúan en el cambio activo.
