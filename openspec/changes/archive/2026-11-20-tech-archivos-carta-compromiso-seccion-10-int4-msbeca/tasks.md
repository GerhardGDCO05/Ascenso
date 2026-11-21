## Tasks

- [x] 10.1 Implementar `IAlmacenamientoArchivos` con adaptador local para desarrollo y adaptador Firebase/Google Cloud Storage para producción; verificar que ambos calculan SHA-256 y devuelven URL, hash y tamaño.
- [x] 10.2 Implementar `ICartaOfertaPdfGenerator` con PdfSharpCore y una plantilla que incluya fundación, estudiante, programa, periodo, cobertura, monto y vigencia; verificar que el PDF generado tiene cabecera `%PDF` y hash reproducible.
- [x] 10.3 Integrar la generación y subida de la carta en la emisión; verificar que si la subida falla no se persiste la oferta y que si se persiste la URL/hash son los del archivo subido.
- [x] 10.4 Implementar la validación de archivos de compromiso (PDF, 5 MB, hash, intento) y la ruta inmutable en Firebase; verificar con pruebas de archivo válido, tipo inválido, tamaño excedido y reintento tras rechazo.
- [x] 10.5 Verificar que los binarios nunca se guardan en PostgreSQL y que solo se persisten `url`, `hash`, `tamaño` y `tipoMime`.
- [x] 10.6 Generar y almacenar la plantilla del compromiso (`urlPlantillaGeneradaFirebase`) con las condiciones del programa y de la beca; verificar que el becario descarga la plantilla correcta, que la ruta es inmutable y que el binario no se guarda en PostgreSQL.
- [x] Verificar build con `--warnaserror`, `dotnet format --verify-no-changes`, suite completa, migraciones sin cambios pendientes y `openspec validate --all --strict`.
- [x] Verificar la plantilla del compromiso de extremo a extremo: datos de entrada, ruta inmutable, exposición en la consulta de la beca, compensación si falla la persistencia y ausencia de columnas binarias.
- [x] Registrar este histórico de sesión sin mover ni archivar el cambio activo `tech-bootstrap-int4-msbeca`.
