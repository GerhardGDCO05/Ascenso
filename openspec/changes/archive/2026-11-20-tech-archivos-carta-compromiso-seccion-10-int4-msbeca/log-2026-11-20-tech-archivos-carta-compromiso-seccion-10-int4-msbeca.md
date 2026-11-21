# Registro de Auditoría — Sección 10: Archivos, carta y compromiso

- **Fecha y hora de ejecución:** 20 de noviembre de 2026, 21:15 (UTC-4)
- **Rama:** `Gerhard`
- **Commit de la sección:** `46c3fa3` (`seccion 10 task.md`), que contiene la implementación base, la migración, parte de las pruebas y la actualización provisional de roadmaps. Las pruebas de la plantilla, los ajustes finales de persistencia y la alineación de `tasks.md` quedaron sin commit.
- **Alcance:** tareas `10.1` a `10.6` del cambio `tech-bootstrap-int4-msbeca`.
- **Microservicio intervenido:** solo `Apps/services/ms-becas/`; los demás servicios quedaron intactos.
- **Trabajo local:** este histórico se crea dentro de `openspec/changes/archive/` y queda sin commit. No se archivó el cambio activo ni se sincronizaron specs principales.
- **Estado del cambio activo:** 91/184 tareas; secciones 1–10 completas y 93 tareas pendientes en las secciones 11–22. El cambio **no** se movió a `archive/`.

## 1. Objetivo

Cerrar la gestión de archivos del otorgamiento de `MS-BEC`: abstracción de almacenamiento con adaptadores local y Firebase/Google Cloud Storage, generación determinista de la carta y de la plantilla del compromiso, integración de la carta en la emisión, validación del PDF firmado y persistencia exclusiva de metadatos, sin guardar binarios en PostgreSQL.

## 2. Cambios realizados

### 2.1 Abstracción y almacenamiento local (10.1)

- `IAlmacenamientoArchivos` expone `SubirAsync` y `EliminarAsync`; `ArchivoAlmacenado` devuelve URL, SHA-256 y tamaño.
- `AlmacenamientoLocalArchivos` normaliza la ruta lógica, impide que escape de la raíz, escribe en un archivo temporal y hace un movimiento atómico al destino.
- Calcula el hash y el tamaño mientras copia el contenido, y devuelve una URI `file://` o la URL HTTP/HTTPS base configurada.
- La eliminación local es idempotente y valida que la URL pertenezca al almacenamiento configurado.

### 2.2 Firebase y Google Cloud Storage (10.1)

- `AlmacenamientoFirebaseArchivos` bufferiza el contenido, calcula SHA-256 y tamaño, sube el objeto y devuelve la URL canónica.
- Acepta URLs de `storage.googleapis.com`, `firebasestorage.googleapis.com` y `gs://`; valida que el bucket de la URL coincida con el configurado.
- `AlmacenObjetosGoogleCloudStorage` encapsula el cliente oficial y trata como exitosa la eliminación de un objeto inexistente (`404`).
- `RegistroArchivos` selecciona el adaptador según `Storage:Provider` y registra `ICartaOfertaPdfGenerator`, `IPlantillaCompromisoPdfGenerator` y `IAlmacenamientoArchivos`.
- `OpcionesAlmacenamiento` lee `Storage:Provider`, `Storage:Local:*` y `Storage:Firebase:*`, con valores por defecto para desarrollo.

### 2.3 Generación de la carta (10.2)

- `CartaOfertaPdfGenerator` usa PdfSharpCore y Liberation Sans embebida.
- La carta incluye fundación, estudiante, programa, periodo, cobertura, monto por periodo y fecha de vigencia.
- Los metadatos del PDF se fijan con datos del dominio y `DocumentoPdfComun.Normalizar` estabiliza el prefijo de la fuente y el identificador `/ID` para que el mismo contenido produzca el mismo hash.
- La fuente y su licencia se versionan en `Archivos/Recursos/`.

### 2.4 Integración de la carta en la emisión (10.3)

- `EmitirOfertaBecaHandler` reserva cupo/presupuesto, genera la carta, la sube a `becas/{becaId}/ofertas/{ofertaId}/carta-otorgamiento.pdf`, programa la expiración y crea el agregado.
- La oferta persiste la URL, el hash, el tamaño y el tipo MIME del archivo realmente subido.
- Si falla la generación, la subida, la programación del job o la persistencia, la compensación cancela el job y elimina la carta y la plantilla que se hayan subido.
- Los errores se traducen a `DependenciaNoDisponibleException` con dependencias `generador-carta`, `generador-plantilla-compromiso` o `almacenamiento`.
- Las pruebas verifican que un fallo de subida no persiste la oferta y que un hash o tamaño inválido elimina el archivo y falla cerrado.

### 2.5 Validación del compromiso firmado (10.4)

- `CargarCompromisoBecarioHandler` valida firma `%PDF-`, tipo MIME `application/pdf`, extensión `.pdf`, contenido no vacío y tamaño máximo de 5 MB.
- La ruta es inmutable por intento: `becas/{becaId}/compromisos/{compromisoId}/intento-{n}.pdf`.
- El handler calcula el SHA-256 del contenido y compara el hash y el tamaño devueltos por el almacenamiento; si no coinciden, elimina el archivo y falla con `DependenciaNoDisponibleException`.
- `CompromisoBecario.Cargar` registra URL, hash, tamaño, tipo MIME, IP, fecha, número de intento y estado `EN_REVISION`; si el intento anterior fue rechazado, no permite reutilizar el mismo hash.
- Si falla la persistencia, el archivo subido se elimina de forma *best-effort*.

### 2.6 Persistencia exclusiva de metadatos (10.5)

- `OfertaBeca` persiste `url_carta_pdf_firebase`, `hash_carta_sha256`, `tipo_mime_carta_pdf` y `tamanio_carta_pdf_bytes`.
- `CompromisoBecario` persiste `url_plantilla_generada_firebase`, `url_pdf_firmado_firebase`, `hash_documento_sha256`, `tipo_mime_documento` y `tamanio_documento_bytes`.
- La migración `20261120025408_MetadatosArchivos` agrega las columnas de tipo MIME y tamaño y las restricciones `ck_oferta_beca_carta_metadatos_validos` y `ck_compromiso_becario_tamanio_valido`.
- La columna `url_plantilla_generada_firebase` ya existía en el modelo; la sección 10 la usa y la expone en la consulta.
- `ModeloPersistenciaTests` verifica que no existan propiedades `byte[]` ni columnas `bytea` en las entidades del dominio y que los metadatos requeridos estén mapeados.

### 2.7 Plantilla del compromiso (10.6)

- Al emitir una oferta, si `ProyeccionOtorgamiento.Programa.RequiereCompromisoFirmado` es verdadero, se genera la plantilla con las condiciones del programa y de la beca.
- La plantilla se sube a `becas/{becaId}/compromisos/plantilla-compromiso.pdf` y se guarda en `CompromisoBecario.UrlPlantillaGeneradaFirebase`.
- `Beca.CrearOtorgada` exige la URL de la plantilla cuando el programa requiere compromiso; si falta o está vacía, la creación falla.
- La plantilla incluye código de beca, fecha de emisión, datos del becario, cobertura, monto, duración, condiciones de permanencia, obligaciones y bloque de firma.
- `CompromisoBecarioDto.UrlPlantillaGenerada` se proyecta en `GET /api/v1/becas/{becaId}` para que el becario obtenga la URL de la plantilla correcta.
- Las pruebas cubren la generación y subida de la plantilla, la ausencia de plantilla cuando el programa no la exige, la compensación de carta y plantilla si falla la persistencia y los fallos del generador o de la subida.

### 2.8 Pruebas y ajustes de soporte

- Se agregó `GeneradorPlantillaFalso` y se amplió `AlmacenamientoArchivosFalso` con resultados y errores por ruta.
- Se actualizaron las pruebas de dominio, handlers, persistencia, controladores, arquitectura y API para incluir los nuevos metadatos y la plantilla.
- `OfertasBecaApiFactory` sustituye también `IPlantillaCompromisoPdfGenerator` en las pruebas de API.
- Se actualizaron `DatosPrueba`, `PruebasOtorgamiento`, `EscenarioOtorgamiento`, `DatosAplicacion` y los índices únicos para reflejar las columnas nuevas.

### 2.9 Configuración y arranque

- `Program.cs` registra `AgregarArchivos(builder.Configuration)` antes de la mensajería y Hangfire.
- `appsettings.json` incorpora la sección `Storage` con proveedor `Local`, ruta base `almacenamiento` y espacio para bucket, credenciales y URL base de Firebase.
- La API puede arrancar en desarrollo con el adaptador local; las pruebas de API sustituyen los adaptadores por dobles.

## 3. Archivos principales

- `Apps/services/ms-becas/Ascenso.Becas.Infrastructure/Archivos/`: `IAlmacenObjetosNube.cs`, `AlmacenObjetosGoogleCloudStorage.cs`, `AlmacenamientoLocalArchivos.cs`, `AlmacenamientoFirebaseArchivos.cs`, `HuellaArchivo.cs`, `OpcionesAlmacenamiento.cs`, `RegistroArchivos.cs`, `CartaOfertaPdfGenerator.cs`, `PlantillaCompromisoPdfGenerator.cs`, `DocumentoPdfComun.cs`, `ResolutorFuenteCarta.cs`, `Recursos/LiberationSans-Regular.ttf` y `Recursos/LiberationSans-LICENSE.txt`.
- `Apps/services/ms-becas/Ascenso.Becas.Domain/Ports/`: `IAlmacenamientoArchivos.cs`, `ArchivoAlmacenado.cs`, `ICartaOfertaPdfGenerator.cs`, `DatosCartaOferta.cs`, `IPlantillaCompromisoPdfGenerator.cs` y `DatosPlantillaCompromiso.cs`.
- `Apps/services/ms-becas/Ascenso.Becas.Domain/Otorgamiento/`: `Agregados/Beca.cs`, `Entidades/OfertaBeca.cs`, `Entidades/CompromisoBecario.cs` y `Datos/DatosCreacionBeca.cs`.
- `Apps/services/ms-becas/Ascenso.Becas.Application/Otorgamiento/Comandos/EmitirOfertaBeca/EmitirOfertaBecaHandler.cs` y `.../CargarCompromisoBecario/CargarCompromisoBecarioHandler.cs`.
- `Apps/services/ms-becas/Ascenso.Becas.Application/Otorgamiento/Dtos/CompromisoBecarioDto.cs`.
- `Apps/services/ms-becas/Ascenso.Becas.Infrastructure/Persistence/Configuraciones/OfertaBecaConfiguracion.cs` y `CompromisoBecarioConfiguracion.cs`.
- `Apps/services/ms-becas/Ascenso.Becas.Infrastructure/Persistence/Migraciones/20261120025408_MetadatosArchivos.cs`, su `Designer` y `BecasDbContextModelSnapshot.cs`.
- `Apps/services/ms-becas/Ascenso.Becas.Infrastructure/Persistence/Consultas/ConsultasBeca.cs`.
- `Apps/services/ms-becas/Ascenso.Becas.Api/Program.cs` y `appsettings.json`.
- `Apps/services/ms-becas/tests/Ascenso.Becas.IntegrationTests/Archivos/`: `AlmacenamientoLocalArchivosTests.cs`, `AlmacenamientoFirebaseArchivosTests.cs`, `CartaOfertaPdfGeneratorTests.cs` y `PlantillaCompromisoPdfGeneratorTests.cs`.
- `Apps/services/ms-becas/tests/Ascenso.Becas.Application.Tests/Dobles/`: `AlmacenamientoArchivosFalso.cs` y `GeneradorPlantillaFalso.cs`.
- `openspec/changes/tech-bootstrap-int4-msbeca/tasks.md`: tareas `10.1` a `10.6` marcadas.
- `openspec/roadmaps/por_microservicio.md` y `openspec/roadmaps/por_integrante.md`: modificados por el commit base para marcar RF-15, RF-16 y RF-17 de forma provisional; este histórico no los vuelve a modificar.

## 4. Pruebas incorporadas

| Proyecto | Antes | Ahora | Nuevas |
| :--- | ---: | ---: | ---: |
| Domain | 136 | 138 | +2 |
| Application | 95 | 101 | +6 |
| Architecture | 8 | 8 | 0 |
| Integration | 307 | 360 | +53 |
| **Total** | **546** | **607** | **+61** |

- Cuatro archivos nuevos de `Archivos/` aportan 28 métodos de prueba: `AlmacenamientoLocalArchivosTests` (10), `AlmacenamientoFirebaseArchivosTests` (7), `CartaOfertaPdfGeneratorTests` (5) y `PlantillaCompromisoPdfGeneratorTests` (6). Los casos `[Theory]` y las pruebas de handler, dominio, persistencia y controladores completan el incremento de 61.
- La suite completa quedó en 607/607 correctas, sin omitidas: Domain 138, Application 101, Architecture 8 e Integration 360.

## 5. Verificación ejecutada

- `dotnet test Apps/services/ms-becas/Ascenso.Becas.sln --no-restore`: 607/607 correctas.
- `dotnet build Apps/services/ms-becas/Ascenso.Becas.sln -warnaserror --no-restore`: 0 errores. Solo se emitieron avisos `NU1900` por la imposibilidad de consultar `https://api.nuget.org/v3/index.json`.
- `dotnet format Apps/services/ms-becas/Ascenso.Becas.sln --verify-no-changes --no-restore`: sin cambios de formato.
- `dotnet ef migrations has-pending-model-changes --project Ascenso.Becas.Infrastructure --startup-project Ascenso.Becas.Infrastructure --no-build`: sin cambios pendientes.
- `openspec validate --all --strict`: 7/7 elementos correctos para specs y cambios activos.
- `openspec validate --archived --strict`: 26/26 históricos correctos, incluido este registro de la sección 10.
- `git diff --check`: sin errores de espacio.
- `tasks.md`: 91/184; sección 10 completa (6/6) y 93 tareas pendientes en las secciones 11–22.

## 6. Análisis de HUs del roadmap

- **RF-15 / HU-15.1 (carta y emisión):** la sección 10 cierra la carta, la plantilla y la persistencia de metadatos. La HU sigue parcial porque las tareas 15.1–15.5 y 15.7 (otorgar la postulación, condiciones del programa, periodo vigente, ciclo de reserva, OTP y cancelación del job) están pendientes.
- **RF-17 / HU-17.1 (plantilla y carga):** la plantilla se genera al emitir, se almacena en ruta inmutable y se expone en la consulta de la beca; la carga y revisión del compromiso ya estaban implementadas. La sección 10 no cambia el estado del roadmap por sí sola.
- **RF-17 / HU-17.2 (revisión y compuerta):** no se modifica en esta sección; sigue cubierta por las secciones 3, 4, 6 y 8.
- **Roadmaps:** el commit `46c3fa3` marcó RF-15, RF-16 y RF-17 de forma provisional por la rebanada funcional. Este histórico no los vuelve a marcar ni los declara cerrados; las deudas 15.1–15.7 y la verificación integral de la sección 22 siguen pendientes.
- No se sincronizaron specs principales ni se modificó `openspec/specs/ms-beca/spec_beca.md`.

## 7. Decisiones y riesgos

- La compensación de archivos y jobs es *best-effort*: si falla la eliminación, se conserva el error original y puede quedar un objeto huérfano. La ruta por intento y los jobs idempotentes reducen el impacto.
- La ruta local se sobrescribe si se repite la misma ruta lógica; la inmutabilidad efectiva depende de que los casos de uso usen rutas con identificadores e intentos.
- La reproducibilidad del hash depende de la fuente embebida y de la normalización de PdfSharpCore. Un cambio de fuente o de versión puede cambiar los bytes del PDF.
- El PDF no se firma criptográficamente; la integridad se apoya en SHA-256 y en la revisión del coordinador.
- La plantilla se expone como URL de almacenamiento, no mediante un endpoint proxy; el control de acceso al objeto depende de la configuración del bucket.
- Firebase requiere credenciales válidas; en desarrollo se usa el adaptador local y las pruebas no dependen de la nube.
- Las pruebas de integración requieren Docker/Testcontainers y PostgreSQL real.
- El commit `46c3fa3` mezcla la implementación de la sección 10 con la actualización provisional de roadmaps y parte de la alineación de `tasks.md`; las pruebas de la plantilla y los ajustes finales siguen sin commit.

## 8. Estado y próximos pasos

- El cambio activo `tech-bootstrap-int4-msbeca` permanece en `openspec/changes/` con 91/184 tareas.
- Las secciones 1–10 están completas; la siguiente es la sección 11 (Observabilidad y operación).
- Quedan 93 tareas en las secciones 11–22.
- Este histórico no mueve ni archiva el cambio activo, no sincroniza specs y no declara cerrados RF-15, RF-16 ni RF-17.
- Quedan sin commit: las pruebas de la plantilla y los ajustes de la sección 10, la alineación de `tasks.md` con las HUs, `.gitignore` y este registro histórico.
