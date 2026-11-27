# Desarrollo local de MS-BEC

Ejecuta esta guía desde la raíz del repositorio. Requiere el .NET SDK fijado
por `global.json`, Docker con el complemento Compose, `make`, `jq`, `curl` y
`dotnet-ef`, además de Python 3 para generar el fixture local de Keycloak.

## Preparar el entorno

```bash
cp .env.example .env
```

Reemplaza los valores `replace_with_a_local_password` por claves locales
distintas. `.env` está excluido de Git; no guardes credenciales reales en el
repositorio. Los valores incluidos en el realm y las cuentas de prueba son
exclusivos para desarrollo local.

Un solo comando crea la base `ascenso_becas`, levanta PostgreSQL, RabbitMQ y
Keycloak, genera el fixture local desde el realm compartido, aplica las
migraciones y comprueba que Keycloak cargó el realm:

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
| `Storage__Firebase__RutaCredenciales` | Configuración futura del runtime cloud para una ruta externa de credenciales; si se omite, se usa ADC. No se configura en el entorno local. |
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
docker compose --profile api up --build --wait -d api
```

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

`make down` conserva los volúmenes de PostgreSQL, RabbitMQ y archivos locales.
Eliminar los volúmenes con `docker compose down -v` borra los datos locales.

## Emisión local de una oferta

`make dev-up` carga proyecciones de programa, matrícula y convenio para el
programa de demostración, y levanta WireMock con respuestas simuladas de
MS-POS, MS-PRG y MS-PLT. Los mappings cubren adjudicabilidad, periodo actual,
condiciones del programa, reserva, confirmación y otorgamiento para la emisión
local de una oferta. La postulación adjudicable disponible es
`77777777-7777-4777-8777-777777777777`; las respuestas y sus identificadores
están en `deploy/wiremock/mappings/`. Esta configuración es solo para
desarrollo y no cambia ni requiere los otros microservicios.

## Almacenamiento de documentos y preparación de Firebase Storage

El desarrollo local usa `STORAGE_PROVIDER=Local`: las cartas y los compromisos se guardan en el volumen local y no se solicita ninguna credencial ni se contacta Firebase o Google Cloud. Las pruebas de archivos usan adaptadores falsos y almacenamiento local; CI tampoco usa un proyecto cloud.

El código conserva el adaptador de Google Cloud Storage para una integración futura con el bucket de Firebase que ya utiliza la Fundación. Para activarlo en otro momento deberán configurarse externamente el nombre real del bucket y una identidad con permisos mínimos. No se incluyen credenciales, bucket ni conexión real en este repositorio. `STORAGE_PROVIDER=Firebase` activa ese adaptador solo cuando el entorno se configure expresamente.

Cloud Storage for Firebase requiere el plan Blaze con cuenta de facturación; Spark no permite acceso al bucket. Blaze incluye cuotas sin cargo, pero el consumo que las supere puede generar cargos. Antes de activar el proveedor cloud, el equipo debe verificar el plan, el bucket, la región, los permisos IAM y las alertas de presupuesto según la [documentación oficial](https://firebase.google.com/docs/storage/faqs-storage-changes-announced-sept-2024) y los [precios vigentes](https://firebase.google.com/pricing/).

La configuración prevista es:

```dotenv
STORAGE_PROVIDER=Firebase
FIREBASE_STORAGE_BUCKET=nombre-real-del-bucket
# La identidad se configura fuera del repositorio mediante ADC/Workload Identity.
```

En un entorno administrado puede usarse Application Default Credentials o Workload Identity sin archivo de clave. La referencia persistida del objeto es `gs://...`; la URL firmada de lectura se genera por un máximo de 15 minutos después de validar la autorización. Las Firebase Security Rules previstas para el bucket están en `deploy/firebase-storage.rules` y deniegan acceso directo de clientes; el backend se autoriza mediante IAM. Estas reglas todavía no se han publicado en Firebase.

## Tokens de prueba de Keycloak

La fuente del realm del equipo está en `deploy/keycloak/ascenso-realm.json`
desde la raíz del repositorio. `make up` genera
`deploy/keycloak/ascenso-realm.json` de MS-BEC a partir de esa fuente y
`deploy/local/keycloak-overlay.json`, que agrega únicamente datos de prueba.
Después de modificar la fuente o el overlay, vuelve a ejecutar `make up` y
recrea el contenedor local de Keycloak para reimportar el realm; Keycloak no
actualiza automáticamente un realm existente al importar de nuevo.

El realm `ascenso` local contiene usuarios `coordinador.local`,
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
audiencia incluye `ms-bec`. El coordinador local recibe además
`permissions` como arreglo con los permisos de convocatorias y la audiencia
`ascenso-api` para probar compatibilidad con los consumidores actuales.

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
El realm compartido deshabilita el flujo de contraseña directa. El overlay
local lo habilita para estos ejemplos; un usuario administrativo que todavía
no configuró TOTP debe completar el registro en el navegador antes de usar
el ejemplo `grant_type=password`. Keycloak 26 puede devolver un error del
servidor en ese flujo directo si se intenta antes del registro, sin emitir JWT.

El fixture incluye `lector-convocatorias.local` con el permiso de prueba
`convocatorias:leer` y sin rol administrativo. Su token permite comprobar que
`permissions` es un arreglo real de strings; no concede acceso a MS-BEC por
carecer de los roles que esta API exige. La contraseña local figura en el
overlay y no debe usarse fuera de desarrollo.

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
