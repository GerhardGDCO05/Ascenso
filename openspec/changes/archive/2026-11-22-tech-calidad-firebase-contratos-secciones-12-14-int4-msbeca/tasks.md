## 12. Pruebas de integración y calidad

- [x] 12.1 Ejecutar la suite de dominio y verificar que cubre todas las transiciones, invariantes, snapshots y cálculos de montos con cobertura ≥ 80%.
- [x] 12.2 Ejecutar la suite de aplicación con dobles de repositorios, clientes, reloj y almacenamiento; verificar los caminos felices y los fallos de cada handler.
- [x] 12.3 Ejecutar las pruebas de arquitectura y verificar que `Domain` no referencia infraestructura y que `Application` no referencia `Infrastructure`/`API`.
- [x] 12.4 Ejecutar las pruebas de integración de persistencia con Testcontainers PostgreSQL: migraciones, índices únicos, trigger de historial, concurrencia optimista y bloqueo por estudiante.
- [x] 12.5 Ejecutar una prueba end-to-end de API: emitir oferta con WireMock de POS/PRG, aceptar, consultar beca activa y verificar el estado final y los eventos.
- [x] 12.6 Ejecutar una prueba end-to-end de rechazo, anulación y expiración con Hangfire y verificar que se publica el evento de liberación correspondiente y que la beca queda `REVOCADA` con el `tipoCierre` correcto.
- [x] 12.7 Ejecutar una prueba de concurrencia con dos aceptaciones simultáneas del mismo estudiante y verificar que solo una gana y la otra recibe `409 DOBLE_BENEFICIO`.
- [x] 12.8 Ejecutar una prueba de mensajería con RabbitMQ/Testcontainers: evento en Outbox, entrega, duplicado e Inbox; verificar que no hay efectos duplicados.
- [x] 12.9 Ejecutar pruebas de seguridad: token sin 2FA, sin `postulante_id`, sin `programa_ids`, servicio interno sin token y postulante operando sobre oferta ajena; verificar `401`/`403`.
- [x] 12.10 Ejecutar `dotnet format --verify-no-changes`, `dotnet build -warnaserror` y `dotnet test` sobre toda la solución; verificar que el pipeline local queda verde.
- [x] 12.11 Verificar la cobertura global y el umbral de 80% en `Domain` + `Application`; ajustar pruebas si el reporte queda por debajo.

## 13. Entorno local, contenedores y CI

- [x] 13.1 Crear `compose.yaml` con PostgreSQL 17, RabbitMQ 4 (management), Keycloak 26 y un perfil opcional para la API; verificar con `docker compose config` y `docker compose up -d` que los healthchecks quedan sanos.
- [x] 13.2 Crear `deploy/keycloak/ascenso-realm.json` con realm, clientes `ms-bec` y `ms-bec-internal`, roles base, usuarios de prueba y mappers de claims; verificar la obtención de un token con `curl` y que incluye `roles`, `postulante_id` y `programa_ids`.
- [x] 13.3 Crear `Makefile` con `up`, `down`, `migrate`, `run`, `test` y `format`, y `.env.example` sin secretos; verificar cada objetivo en una copia limpia.
- [x] 13.4 Crear el `Dockerfile` multi-stage con usuario no root, variables de entorno y `HEALTHCHECK`; verificar `docker build` y un arranque contra el compose local.
- [x] 13.5 Crear el pipeline de GitHub Actions con restore bloqueado, build `-warnaserror`, formato, pruebas unitarias/arquitectura, pruebas de integración con Docker, cobertura y build de imagen; verificar que el workflow es válido y que falla si se rompe una regla de arquitectura.
- [x] 13.6 Crear un script de arranque limpio que cree la base, aplique migraciones y cargue el realm de Keycloak; verificar que un entorno nuevo queda operativo con un solo comando.
- [x] 13.7 Documentar en el README el flujo de desarrollo, las variables de entorno, la obtención de tokens y la solución de problemas comunes; verificar que un integrante nuevo puede levantar el servicio siguiendo solo la documentación.
- [x] 13.8 Crear un servicio `dev-stubs` en Compose (WireMock o API mínima) que simule `MS-POS` y `MS-PRG`, con datos semilla de postulación adjudicable y respuesta de reserva; verificar que el flujo de emisión funciona de punta a punta sin depender de los microservicios reales.
- [x] 13.9 Configurar Firebase Storage Emulator como opción de almacenamiento local, con persistencia en volumen y endpoint accesible por los microservicios; conectar MS-BEC mediante el adaptador Firebase y verificar que las cartas y compromisos se guardan y se leen sin servicios cloud.
- [x] 13.10 Documentar la topología y configuración del emulador local y verificar que su uso no requiere credenciales de GCP ni realiza llamadas cloud; la selección entre proveedores y la integración cloud se completan en las tareas siguientes de esta sección.
- [x] 13.11 Completar la selección explícita por entorno entre Firebase Cloud Storage y el emulador, con credenciales externas, sin secretos en `.env.example` ni fallback automático; agregar pruebas de configuración/inicio y documentar las opciones, el límite Spark/Blaze y la selección del emulador como alternativa, no como requisito.
- [x] 13.12 Extender el puerto/adaptador de archivos para conservar referencias estables y resolver enlaces de lectura: URLs firmadas cloud limitadas al objeto y a 15 minutos como máximo, y URLs locales cuando se seleccione el emulador; agregar pruebas de expiración, identidad del objeto y no persistencia del enlace firmado.
- [x] 13.13 Actualizar las respuestas de cartas y compromisos para resolver enlaces solo después de validar JWT Keycloak y propiedad/alcance; agregar pruebas que aseguren que una petición no autorizada no recibe enlace, y documentar el comportamiento de la API.
- [x] 13.14 Mantener reglas Firebase que nieguen acceso directo de clientes y documentar reglas para Firebase Console, IAM mínimo, cuotas/riesgo de facturación y configuración externa; verificar la denegación con una prueba reproducible contra las reglas del emulador.
- [x] 13.15 Verificar el flujo integrado de carga, lectura y eliminación en la opción de emulador, ejecutar las pruebas de enlaces cloud con firmador de prueba y confirmar que CI no usa credenciales ni contacta proyectos reales; ejecutar `make test`, build y validación estricta OpenSpec sin cambios en otros microservicios.

## 14. Documentación de contratos y cierre del cambio

- [x] 14.1 Publicar la especificación OpenAPI de los endpoints públicos e internos y los contratos de eventos (AsyncAPI o Markdown) en `docs/`; verificar que coinciden con las rutas y DTOs implementados.
- [x] 14.4 Ejecutar `openspec validate tech-bootstrap-int4-msbeca --strict` y corregir cualquier observación; verificar que el cambio queda listo para aplicar.
- [x] 14.5 Verificar que este cambio no implemente desembolsos, seguimiento, renovación ni revocación definitiva, y que el alcance completo de `MS-BEC` quede planificado y trazado en las secciones 15–22.

> 14.2 y 14.3 no se incluyen como tareas cerradas en este histórico: siguen pendientes en el cambio activo hasta que los equipos propietarios validen los contratos.

## Registro del histórico parcial

- [x] Registrar este histórico de las tareas completadas de las secciones 12–14 sin mover ni archivar el cambio activo `tech-bootstrap-int4-msbeca`.
- [x] Mantener 14.2–14.3 y las secciones 15–22 en el cambio activo; conservar allí los deltas de especificación sin sincronizarlos.
