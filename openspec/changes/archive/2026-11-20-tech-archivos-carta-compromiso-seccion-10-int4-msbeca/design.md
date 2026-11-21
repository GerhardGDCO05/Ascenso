## Context

La sección 10 se implementó sobre las secciones 1–9 de `MS-BEC`. Su objetivo fue completar la gestión de archivos del otorgamiento: almacenamiento local y Firebase/Google Cloud Storage, generación determinista de la carta y de la plantilla del compromiso, integración de la carta en la emisión, validación del PDF firmado y persistencia exclusiva de metadatos. Las secciones 11–22 continúan en el cambio activo y no forman parte de este histórico.

## Decisions

### Abstracción y adaptadores de almacenamiento (10.1)

- `IAlmacenamientoArchivos` expone `SubirAsync` y `EliminarAsync` y devuelve `ArchivoAlmacenado(Url, HashSha256, TamanioBytes)`. El hash y el tamaño se calculan sobre los bytes que realmente se escriben, no sobre los metadatos que informe el proveedor.
- `AlmacenamientoLocalArchivos` normaliza la ruta lógica, valida que no escape de la raíz, escribe en un archivo temporal y hace un movimiento atómico al destino. Devuelve una URI `file://` o la URL HTTP base configurada y permite eliminar de forma idempotente.
- `AlmacenamientoFirebaseArchivos` bufferiza el contenido, calcula su SHA-256 y tamaño, sube el objeto al bucket y devuelve la URL canónica. Para eliminar acepta URLs de `storage.googleapis.com`, `firebasestorage.googleapis.com` y `gs://`, y valida que el bucket coincida con el configurado.
- `AlmacenObjetosGoogleCloudStorage` encapsula el cliente oficial y considera exitosa la eliminación de un objeto inexistente (`404`). `RegistroArchivos` selecciona el adaptador según `Storage:Provider` y registra ambos generadores de PDF como singletons.

### Generación de PDF (10.2)

- `CartaOfertaPdfGenerator` usa PdfSharpCore y una fuente Liberation Sans embebida. La carta incluye fundación, estudiante, programa, periodo, cobertura, monto por periodo y fecha de vigencia.
- `PlantillaCompromisoPdfGenerator` incluye código de beca, fecha de emisión, datos del becario, condiciones económicas, condiciones de permanencia, obligaciones y bloque de firma.
- Ambos generadores fijan título, autor, asunto, creador y fechas de creación/modificación a partir de datos del dominio. `DocumentoPdfComun.Normalizar` estabiliza el prefijo del nombre de la fuente y el identificador `/ID`, de modo que los mismos datos produzcan el mismo hash.
- La fuente y su licencia se versionan en `Archivos/Recursos/`; no se depende de fuentes del sistema operativo.

### Integración de la carta en la emisión (10.3)

- `EmitirOfertaBecaHandler` reserva cupo y presupuesto, genera la carta, la sube a `becas/{becaId}/ofertas/{ofertaId}/carta-otorgamiento.pdf`, programa la expiración, crea el agregado y persiste la oferta con la URL, hash, tamaño y tipo MIME del archivo subido.
- Si la generación, la subida, la programación del job o la persistencia fallan, se ejecuta una compensación *best-effort*: se cancela el job y se eliminan la carta y la plantilla que hayan llegado a almacenarse.
- Los errores de generación se traducen a `DependenciaNoDisponibleException` con dependencias `generador-carta`, `generador-plantilla-compromiso` o `almacenamiento`, para que la API mantenga el contrato de errores.
- La oferta no se persiste si la carta no se pudo generar o subir; las pruebas verifican que no queda un archivo huérfano ni una oferta sin carta.

### Validación del compromiso firmado (10.4)

- `CargarCompromisoBecarioHandler` valida firma `%PDF-`, tipo MIME `application/pdf`, extensión `.pdf`, contenido no vacío y tamaño máximo de 5 MB antes de subir.
- La ruta es inmutable por intento: `becas/{becaId}/compromisos/{compromisoId}/intento-{n}.pdf`. El número de intento se toma del estado del compromiso, no del nombre que envía el cliente.
- El handler calcula el SHA-256 del contenido y compara el hash y el tamaño devueltos por el almacenamiento. Si no coinciden, elimina el archivo y falla con `DependenciaNoDisponibleException`.
- `CompromisoBecario.Cargar` registra URL, hash, tamaño, tipo MIME, IP, fecha, número de intento y estado `EN_REVISION`. Si el intento anterior fue rechazado, el nuevo documento no puede reutilizar el mismo hash.
- Si la persistencia falla, el archivo subido se elimina. La compensación es *best-effort* para no ocultar el error original.

### Persistencia exclusiva de metadatos (10.5)

- `OfertaBeca` persiste `url_carta_pdf_firebase`, `hash_carta_sha256`, `tipo_mime_carta_pdf` y `tamanio_carta_pdf_bytes`.
- `CompromisoBecario` persiste `url_plantilla_generada_firebase`, `url_pdf_firmado_firebase`, `hash_documento_sha256`, `tipo_mime_documento` y `tamanio_documento_bytes`.
- La migración `20261120025408_MetadatosArchivos` agrega las columnas de tipo MIME y tamaño y las restricciones `ck_oferta_beca_carta_metadatos_validos` y `ck_compromiso_becario_tamanio_valido`. La columna de la plantilla ya existía en el modelo.
- `ModeloPersistenciaTests` verifica que ninguna entidad del dominio tenga propiedades `byte[]` o columnas `bytea`, y que los metadatos requeridos estén mapeados a sus columnas.

### Plantilla del compromiso (10.6)

- Al emitir una oferta, si `ProyeccionOtorgamiento.Programa.RequiereCompromisoFirmado` es verdadero, se genera la plantilla con las condiciones del programa y de la beca y se sube a `becas/{becaId}/compromisos/plantilla-compromiso.pdf`.
- `Beca.CrearOtorgada` exige `UrlPlantillaCompromiso` cuando el programa requiere compromiso y crea el `CompromisoBecario` en estado `PENDIENTE_CARGA` con esa URL. Si la URL falta o está vacía, la creación falla.
- La consulta de la beca proyecta `CompromisoBecarioDto.UrlPlantillaGenerada`, de modo que el becario obtiene la URL de la plantilla correcta desde `GET /api/v1/becas/{becaId}`.
- La prueba de emisión verifica los datos enviados al generador, la ruta de la plantilla, los dos archivos subidos y la ausencia de eliminaciones cuando todo sale bien. Otras pruebas cubren que no se genere plantilla si el programa no la exige, que falle la emisión si el generador o la subida fallan y que se compensen carta y plantilla si falla la persistencia.

### Pruebas y ajustes de soporte

- Se agregaron dobles `GeneradorPlantillaFalso` y se amplió `AlmacenamientoArchivosFalso` para devolver resultados y errores por ruta.
- Se actualizaron las pruebas de dominio, handlers, persistencia, controladores y arquitectura para incluir los nuevos metadatos.
- `OfertasBecaApiFactory` sustituye también `IPlantillaCompromisoPdfGenerator` en las pruebas de API.
- La suite completa quedó en 607/607: Domain 138, Application 101, Architecture 8 e Integration 360.

## Risks / Trade-offs

- La compensación de archivos y jobs es *best-effort*: si falla la eliminación, el error original se conserva y puede quedar un objeto huérfano en el almacenamiento. La ruta por intento y los jobs idempotentes reducen el impacto.
- La ruta local se sobrescribe si se repite la misma ruta lógica; la inmutabilidad efectiva depende de que los casos de uso usen rutas con identificadores e intentos, como hacen la carta y el compromiso.
- La reproducibilidad del hash depende de la fuente embebida y de la normalización de metadatos de PdfSharpCore. Un cambio de fuente o de versión de la librería puede cambiar los bytes del PDF.
- El PDF no se firma criptográficamente; la firma del becario se materializa en un archivo subido y la validación de integridad se apoya en el SHA-256 y en la revisión del coordinador.
- La plantilla se expone como URL de almacenamiento; no hay un endpoint proxy de descarga. La autorización de acceso al objeto depende de la configuración del bucket.
- Firebase requiere credenciales válidas. En desarrollo se usa el adaptador local y las pruebas no dependen de la nube.
- Las pruebas de integración requieren Docker/Testcontainers y PostgreSQL real.

## Migration Plan

1. Aplicar la migración `20261120025408_MetadatosArchivos`; las columnas nuevas tienen valores por defecto seguros para las filas existentes.
2. Configurar `Storage:Provider` como `Local` en desarrollo o `Firebase` en integración/producción, con bucket, credenciales y URL base opcional.
3. Desplegar la API; verificar que `GET /api/v1/becas/{becaId}` expone `urlPlantillaGenerada` y que la emisión sube carta y plantilla a las rutas esperadas.
4. Verificar que no existan columnas binarias y que la consulta de la beca no exponga el contenido del archivo.
5. El rollback es forward-only: no se eliminan columnas ni objetos de almacenamiento durante una reversión. Si se requiere revertir, se deshabilita la generación y se conservan los metadatos ya persistidos.

## Open Questions

- Ninguna para la sección 10. Las secciones 11–22 y las deudas 15.1–15.7 continúan en el cambio activo `tech-bootstrap-int4-msbeca`.
