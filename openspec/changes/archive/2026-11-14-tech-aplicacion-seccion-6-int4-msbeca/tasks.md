## Tasks

- [x] 6.1 Implementar `ICommandDispatcher`/`IQueryDispatcher` y el registro por ensamblado de handlers; verificar que un comando de prueba se despacha y que un handler ausente produce un error claro.
- [x] 6.2 Crear los DTOs y validadores de `EmitirOfertaBeca`, `AceptarOfertaBeca`, `RechazarOfertaBeca`, `AnularOfertaBeca`, `CargarCompromisoBecario`, `RevisarCompromisoBecario` y `ConsultarBecasActivas`; verificar con pruebas de validación de campos obligatorios, formatos y longitudes.
- [x] 6.3 Implementar la orquestación de `EmitirOfertaBecaHandler`: validar proyecciones de programa/costo/convenio, consultar adjudicabilidad, calcular montos y solicitar la reserva; verificar con dobles de prueba los caminos exitoso, no adjudicable, configuración faltante y fallo de reserva.
- [x] 6.4 Completar `EmitirOfertaBecaHandler` con la creación del agregado, generación de carta, subida a almacenamiento, persistencia, programación del job y publicación del evento; verificar que no queda estado parcial si falla la subida o la persistencia.
- [x] 6.5 Implementar `AceptarOfertaBecaHandler` con transacción `Serializable`, bloqueo por estudiante, bloqueo de fila, validación de vencimiento, no doble beneficio y transición a `ACTIVA`; verificar con pruebas de aceptación exitosa, oferta vencida y doble beneficio.
- [x] 6.6 Implementar `RechazarOfertaBecaHandler` y `AnularOfertaBecaHandler` con transición terminal, `tipoCierre`, liberación por evento y validación de justificación; verificar con pruebas de éxito, estado inválido y justificación insuficiente.
- [x] 6.7 Implementar `ExpirarOfertaBecaHandler` reutilizable por el job y la reconciliación, con relectura y bloqueo de fila; verificar que una oferta ya respondida no cambia y que una pendiente vencida expira.
- [x] 6.8 Implementar `CargarCompromisoBecarioHandler` y `RevisarCompromisoBecarioHandler`, incluyendo validación de PDF, tamaño máximo, hash, intentos y actualización de `habilitadaParaDesembolso`; verificar con pruebas de carga, aprobación, rechazo y nueva carga.
- [x] 6.9 Implementar las consultas `ObtenerOferta`, `ObtenerBeca` y `ConsultarBecasActivas` con DTOs de lectura autorizados; verificar que no exponen datos personales ni bancarios en la respuesta interna.
- [x] 6.10 Integrar la idempotencia en el pipeline de comandos y asegurar que la fila de idempotencia se confirma o revierte junto con el cambio de estado; verificar con pruebas de reintento y de fallo transaccional.
- [x] 6.11 Implementar la coordinación de transacción y Outbox: publicar eventos antes de `SaveChanges` y confirmar estado + mensaje en el mismo commit; verificar con una prueba que un fallo de `SaveChanges` no deja evento publicado.
- [x] Agregar pruebas de integración de `UnidadDeTrabajo` para commit, rollback, nivel `ReadCommitted`, nivel `Serializable`, reutilización de la transacción de idempotencia y reversión conjunta.
- [x] Verificar build Release con `--warnaserror`, suite completa, formato, restore bloqueado, migraciones sin cambios pendientes y validación de OpenSpec.
- [x] Registrar este histórico de sesión sin mover ni archivar el cambio activo `tech-bootstrap-int4-msbeca`.
