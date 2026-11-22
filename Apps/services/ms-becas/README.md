# Desarrollo local de MS-BEC

Ejecuta esta guía desde la raíz del repositorio. Requiere el .NET SDK fijado
por `global.json`, Docker con el complemento Compose, `make`, `jq`, `curl` y
`dotnet-ef`.

## Preparar el entorno

```bash
cp .env.example .env
```

Reemplaza los valores `replace_with_a_local_password` por claves locales
distintas. `.env` está excluido de Git; no guardes credenciales reales en el
repositorio. Los valores incluidos en el realm y las cuentas de prueba son
exclusivos para desarrollo local.

Un solo comando crea la base `ascenso_becas`, levanta PostgreSQL, RabbitMQ y
Keycloak, aplica las migraciones y comprueba que Keycloak cargó el realm:

```bash
make dev-up
```

Puertos predeterminados:

| Servicio | URL/puerto local |
| :--- | :--- |
| PostgreSQL | `localhost:5433` |
| RabbitMQ | `localhost:5673` |
| RabbitMQ Management | <http://localhost:15673> |
| Keycloak | <http://localhost:8080> |
| Stubs locales de MS-POS/MS-PRG | <http://localhost:8082> |
| Firebase Storage Emulator (opcional) | <http://localhost:9199> |

El usuario y contraseña de RabbitMQ, así como la contraseña inicial del
administrador de Keycloak, se toman de `.env`.

Variables principales de `.env`:

| Variables | Propósito |
| :--- | :--- |
| `POSTGRES_DB`, `POSTGRES_USER`, `POSTGRES_PASSWORD`, `POSTGRES_PORT` | Base local y puerto publicado de PostgreSQL. |
| `RABBITMQ_DEFAULT_USER`, `RABBITMQ_DEFAULT_PASS`, `RABBITMQ_PORT`, `RABBITMQ_MANAGEMENT_PORT` | Credenciales y puertos de RabbitMQ. |
| `KEYCLOAK_ADMIN`, `KEYCLOAK_ADMIN_PASSWORD`, `KEYCLOAK_PORT` | Administrador inicial y puerto de Keycloak. |
| `DEV_STUBS_BIND_ADDRESS`, `DEV_STUBS_PORT` | Dirección y puerto local de WireMock para simular MS-POS y MS-PRG. |
| `STORAGE_PROVIDER`, `FIREBASE_STORAGE_BUCKET` | Selección del proveedor y bucket cuando se usa Firebase Storage. |
| `FIREBASE_STORAGE_USE_EMULATOR`, `FIREBASE_STORAGE_URL_BASE` | Selección y endpoint del emulador local; no son necesarios para Cloud Storage. |
| `FIREBASE_STORAGE_CREDENTIALS_PATH` | Ruta externa a credenciales de servicio; déjala vacía para ADC. Nunca guardes la clave en Git. |
| `API_PORT` | Puerto publicado cuando se usa el perfil Docker `api`. |
| `*_BIND_ADDRESS` | Dirección local donde se publican los puertos; por defecto `127.0.0.1`. |

## Ejecutar y probar

Para ejecutar la API en el host, conserva la terminal abierta:

```bash
make run
```

La API estará disponible en <http://localhost:5120>. También puedes ejecutar
la imagen en Compose en <http://localhost:8081>:

```bash
docker compose --profile api --profile firebase-emulator up --build --wait -d api
```

El segundo perfil solo se necesita cuando `FIREBASE_STORAGE_USE_EMULATOR=true`.
Si seleccionas `Local` o Cloud Storage, usa `docker compose --profile api ...`
sin el perfil del emulador.

Comprobaciones de salud:

```bash
curl --fail http://localhost:5120/health/live
curl --fail http://localhost:5120/health/ready
```

Usa `8081` en lugar de `5120` cuando ejecutes la API en Compose. Los comandos
habituales, desde la raíz, son:

```bash
make up       # dependencias locales
make migrate  # dependencias y migraciones
make test     # pruebas de dominio, aplicación, arquitectura e integración
make format   # aplica el formato de .NET
make down     # detiene servicios, incluidos los del perfil api
```

`make down` conserva los volúmenes de PostgreSQL, RabbitMQ y Firebase Storage.
Eliminar los volúmenes con `docker compose down -v` borra los datos locales.

## Emisión local de una oferta

`make dev-up` carga proyecciones de programa, matrícula y convenio para el
programa de demostración, y levanta WireMock con respuestas simuladas de
MS-POS y MS-PRG. La postulación adjudicable disponible es
`77777777-7777-4777-8777-777777777777`; las respuestas y sus identificadores
están en `deploy/wiremock/mappings/`. Esta configuración es solo para
desarrollo y no cambia ni requiere los otros microservicios.

## Firebase Storage

MS-BEC permite seleccionar `Local` o `Firebase` mediante `STORAGE_PROVIDER`.
Cuando el proveedor es Firebase, `FIREBASE_STORAGE_USE_EMULATOR` elige entre
Firebase Storage Emulator y Cloud Storage. El emulador es una opción temporal
para desarrollo/pruebas sin facturación, no un requisito del adaptador.

El `.env.example` configura el emulador para el flujo local sin credenciales.
`make run` inicia el perfil `firebase-emulator` solo si esa opción está
seleccionada; `make test` lo inicia para las pruebas de integración. El API
contenedorizado usa el host `firebase-storage`; los procesos del host usan
`localhost`. Los datos del emulador persisten en `firebase-storage-data` y no
se comparten con otros desarrolladores.

Cloud Storage requiere que el proyecto Firebase tenga habilitado Storage
(plan Blaze); en Spark las operaciones cloud no están disponibles. Para
preparar un entorno cloud, configura externamente el bucket y la identidad:

```dotenv
STORAGE_PROVIDER=Firebase
FIREBASE_STORAGE_BUCKET=nombre-del-bucket
FIREBASE_STORAGE_USE_EMULATOR=false
FIREBASE_STORAGE_URL_BASE=
FIREBASE_STORAGE_CREDENTIALS_PATH=/ruta/externa/service-account.json
```

La ruta debe ser absoluta y apuntar a un archivo externo al repositorio. `make
run` la usa en el proceso del host; Compose la monta en modo solo lectura como
`/run/secrets/firebase-storage.json`. En un runtime administrado puede usarse
ADC/Workload Identity. No se incluyen claves en `.env.example`, imágenes ni CI.
Al seleccionar el emulador se usa `EmulatorOnly`; una falla del proveedor
seleccionado no activa otro proveedor automáticamente.

MS-BEC conserva referencias estables en PostgreSQL. Las respuestas de lectura
emiten enlaces cloud firmados por un máximo de 15 minutos, solo después de
validar propiedad o alcance con Keycloak. El emulador devuelve enlaces locales
para pruebas. Firebase Security Rules deniega acceso directo de clientes; las
operaciones del backend se autorizan mediante IAM, no mediante esas reglas.

Para configurar Firebase Console, publica estas reglas en **Storage >
Rules**. Mantienen privado el bucket y no habilitan Firebase Authentication:

```javascript
rules_version = '2';
service firebase.storage {
  match /b/{bucket}/o {
    match /{allPaths=**} {
      allow read, write: if false;
    }
  }
}
```

Concede al backend solo permisos de objetos necesarios para crear, leer y
eliminar (`storage.objects.create`, `storage.objects.get`,
`storage.objects.delete`). Si el firmador usa IAM en vez de una clave de
servicio, también necesita permiso `iam.serviceAccounts.signBlob`. Antes de
habilitar cloud, acuerda región y bucket, revisa cuotas/precios de Blaze y
configura alertas de presupuesto. CI no usa credenciales ni llama al proyecto
real; sus pruebas de archivos se ejecutan contra el emulador.

## Tokens de prueba de Keycloak

El realm `ascenso` contiene usuarios locales `coordinador.local`,
`postulante.local` y `becario.local`. Sus contraseñas de demostración están
declaradas en `deploy/keycloak/ascenso-realm.json`; no deben reutilizarse en
ningún entorno compartido o productivo.

Token de postulante:

```bash
curl --fail-with-body --silent --show-error \
  -X POST http://localhost:8080/realms/ascenso/protocol/openid-connect/token \
  -H 'Content-Type: application/x-www-form-urlencoded' \
  --data-urlencode 'grant_type=password' \
  --data-urlencode 'scope=openid' \
  --data-urlencode 'client_id=ms-bec' \
  --data-urlencode 'username=postulante.local' \
  --data-urlencode 'password=Postulante_Becas_Local_Only_2026!' \
  | jq '{access_token, token_type, expires_in}'
```

El access token incluye `sub`, `roles`, `postulante_id` y `programa_ids`, y su
audiencia incluye `ms-bec`.

El coordinador debe registrar un autenticador TOTP una vez por cada realm local
recién creado: inicia sesión en <http://localhost:8080/realms/ascenso/account>
y sigue el paso **Mobile Authenticator Setup**. Para emitir, pide un token con
el código de seis dígitos vigente:

```bash
curl --fail-with-body --silent --show-error \
  -X POST http://localhost:8080/realms/ascenso/protocol/openid-connect/token \
  -H 'Content-Type: application/x-www-form-urlencoded' \
  --data-urlencode 'grant_type=password' \
  --data-urlencode 'scope=openid' \
  --data-urlencode 'client_id=ms-bec' \
  --data-urlencode 'username=coordinador.local' \
  --data-urlencode 'password=Coordinador_Becas_Local_Only_2026!' \
  --data-urlencode 'totp=REEMPLAZAR_CON_CODIGO_ACTUAL' \
  | jq -r '.access_token'
```

El claim `amr` se deriva de las ejecuciones de contraseña y TOTP realmente
completadas. El token del coordinador debe incluir `totp`; la API no acepta un
claim estático de segundo factor.

Con ese token se puede verificar la emisión usando la postulación de
demostración. Usa `localhost:5120` cuando la API se ejecuta con `make run`, o
`localhost:8081` con el perfil Compose:

```bash
curl --fail-with-body --silent --show-error \
  -X POST http://localhost:5120/api/v1/becas/ofertas \
  -H "Authorization: Bearer $COORDINADOR_TOKEN" \
  -H "Idempotency-Key: demo-$(date +%s)" \
  -H 'Content-Type: application/json' \
  --data '{"postulacionId":"77777777-7777-4777-8777-777777777777"}'
```

Token de servicio interno:

```bash
curl --fail-with-body --silent --show-error \
  -X POST http://localhost:8080/realms/ascenso/protocol/openid-connect/token \
  -H 'Content-Type: application/x-www-form-urlencoded' \
  --data-urlencode 'grant_type=client_credentials' \
  --data-urlencode 'client_id=ms-bec-internal' \
  --data-urlencode 'client_secret=ms_bec_internal_local_only' \
  --data-urlencode 'scope=becas.internal.read' \
  | jq '{access_token, token_type, expires_in}'
```

No publiques ni compartas los tokens generados. La configuración de
producción debe usar secretos gestionados fuera del repositorio y una política
de segundo factor real en Keycloak.

## Solución de problemas

- **Un servicio no está sano:** revisa `docker compose ps` y los logs con
  `docker compose logs --tail=100 postgres rabbitmq keycloak`.
- **Puerto ocupado:** cambia el puerto correspondiente en `.env`, ejecuta
  `make down` y vuelve a correr `make dev-up`.
- **Cambiar la contraseña de PostgreSQL no resuelve la conexión:** los
  volúmenes conservan la base y el usuario inicializados previamente. Para
  conservar los datos, restaura la contraseña que se usó al crear el volumen.
- **Realm no disponible:** revisa los logs de Keycloak y vuelve a crear su
  contenedor local con `docker compose up --force-recreate keycloak`; la
  importación del realm se ejecuta al iniciar.
- **Migración no encuentra `dotnet-ef`:** instala la versión indicada en el
  README raíz y asegúrate de que `~/.dotnet/tools` está en `PATH`.
- **Fallan las pruebas de integración:** Docker debe estar activo; estas
  pruebas crean sus propios contenedores Testcontainers.
