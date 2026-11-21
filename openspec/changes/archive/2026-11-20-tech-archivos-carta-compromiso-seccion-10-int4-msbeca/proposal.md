## Why

La sección 10 del cambio `tech-bootstrap-int4-msbeca` cierra la gestión de archivos de `MS-BEC`: almacenamiento local y Firebase/Google Cloud Storage, generación determinista de la carta de otorgamiento, integración de la carta en la emisión, validación del compromiso firmado y persistencia exclusiva de metadatos. Sin esta sección, la oferta no podía entregar una carta descargable ni inicializar la plantilla del compromiso que el becario debe firmar.

Este histórico documenta y conserva el trabajo de la sección 10 **sin archivar el cambio activo**, que todavía tiene pendientes las secciones 11–22. Los deltas de `ms-beca/fundacion-servicio` y `ms-beca/otorgamiento-beca` permanecen en el cambio activo y no se sincronizan aquí.

## What Changes

- Implementar `IAlmacenamientoArchivos` con un adaptador local para desarrollo y un adaptador Firebase/Google Cloud Storage para producción; ambos calculan SHA-256 y tamaño sobre los bytes escritos, devuelven una URL y permiten eliminar el archivo de forma idempotente.
- Implementar `ICartaOfertaPdfGenerator` con PdfSharpCore, fuente Liberation Sans embebida, metadatos deterministas y una plantilla con fundación, estudiante, programa, periodo, cobertura, monto y vigencia.
- Integrar la generación y subida de la carta en `EmitirOfertaBecaHandler`; la oferta solo se persiste con la URL, hash, tamaño y tipo MIME del archivo realmente subido.
- Implementar la validación del compromiso firmado: PDF con firma `%PDF-`, tipo MIME y extensión correctos, máximo 5 MB, hash SHA-256, ruta inmutable por intento y rechazo de un nuevo intento con el mismo hash de un documento rechazado.
- Persistir únicamente metadatos de archivos en PostgreSQL (`url`, `hash`, `tamaño` y `tipoMime`) y agregar la migración `20261120025408_MetadatosArchivos` con sus restricciones.
- Generar y almacenar la plantilla del compromiso en `urlPlantillaGeneradaFirebase` cuando el programa exige compromiso firmado; la ruta es `becas/{becaId}/compromisos/plantilla-compromiso.pdf` y se expone al becario en la consulta de la beca.
- Compensar la carta, la plantilla y el job de expiración si falla la persistencia o cualquiera de las etapas posteriores a la subida.

## Capabilities

### New Capabilities

- Ninguna. La sección 10 implementa adaptadores de archivos y completa capacidades ya definidas en los deltas del cambio activo.

### Modified Capabilities

- Ninguna en `openspec/specs/`. Los deltas de `ms-beca/fundacion-servicio` y `ms-beca/otorgamiento-beca` siguen perteneciendo al cambio activo `tech-bootstrap-int4-msbeca` y no se sincronizan en este histórico.

## Impact

- Código afectado: `Apps/services/ms-becas/Ascenso.Becas.Infrastructure/Archivos/`, `Ascenso.Becas.Domain/Ports/`, `Ascenso.Becas.Domain/Otorgamiento/`, `Ascenso.Becas.Application/Otorgamiento/Comandos/EmitirOfertaBeca/`, `Ascenso.Becas.Application/Otorgamiento/Comandos/CargarCompromisoBecario/`, `Ascenso.Becas.Infrastructure/Persistence/`, `Ascenso.Becas.Api/Program.cs` y `appsettings.json`.
- Base de datos: nueva migración `20261120025408_MetadatosArchivos` que agrega `tipo_mime_carta_pdf`, `tamanio_carta_pdf_bytes`, `tipo_mime_documento` y `tamanio_documento_bytes`, más restricciones de integridad. No se agregan columnas binarias.
- Configuración: nueva sección `Storage` con proveedor `Local` o `Firebase`, ruta base local, bucket, credenciales y URL base opcional.
- Pruebas: 61 pruebas nuevas; la suite completa quedó en 607/607 (Domain 138, Application 101, Architecture 8, Integration 360).
- Fuera de alcance: secciones 11–22 del cambio activo, deudas de contrato 15.1–15.7, sincronización de specs principales, actualización de roadmaps y commit de este histórico.
