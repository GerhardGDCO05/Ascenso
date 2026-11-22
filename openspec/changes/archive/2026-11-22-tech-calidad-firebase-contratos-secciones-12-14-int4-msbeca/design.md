## Contexto

Las secciones 12–14 verifican la base de MS-BEC, su ejecución local/CI y los contratos necesarios para integrarse con otros microservicios. Este documento es un histórico parcial, siguiendo el precedente de la sección 11: conserva el avance cerrado sin mover el cambio activo ni sincronizar sus deltas.

## Decisiones

### Calidad y pruebas (sección 12)

- Se completaron las verificaciones de dominio, aplicación, arquitectura, persistencia, API, concurrencia, mensajería, seguridad y cobertura previstas en las tareas 12.1–12.11.
- Las verificaciones generales registradas para la sección 13 incluyeron `make test` con 648 pruebas, build Release sin advertencias, formato, configuración Compose y validación estricta OpenSpec.

### Entorno local, CI y Firebase Storage (sección 13)

- Se completó la configuración de Firebase Storage con selección explícita por entorno y emulador opcional para desarrollo/CI; no se usa fallback automático entre emulador y Cloud Storage.
- Las referencias de archivos permanecen estables. Las URLs cloud se resuelven temporalmente después de autorizar el acceso; el emulador usa URLs locales.
- Las reglas de Storage niegan el acceso directo de clientes. Las pruebas locales verifican operaciones contra el emulador sin credenciales cloud.
- La validación contra un bucket real no se realizó: depende de un proyecto con Storage habilitado, plan compatible e identidad/credenciales externas.

### Contratos (sección 14)

- Se publicó un snapshot OpenAPI generado desde el host de pruebas de MS-BEC, junto con contratos de eventos e identidad/integraciones en Markdown.
- En esta sesión, `OpenApiDocumentoTests` pasó con 4 pruebas; el JSON se validó, el exportador pasó `dotnet format --verify-no-changes` y `git diff --check` no reportó errores.
- Los contratos de identidad e integraciones se documentan como propuestas de MS-BEC. La revisión de MS-PLT y la validación de MS-POS/MS-PRG siguen pendientes; la revisión de solo lectura detectó diferencias entre algunos ejemplos y el código disponible de esos servicios.

## Alcance y continuidad

- Las tareas 12.1–12.11 y 13.1–13.15 están completas; de la sección 14 se incluyen solo 14.1, 14.4 y 14.5.
- 14.2 y 14.3 permanecen pendientes en `tech-bootstrap-int4-msbeca` hasta recibir confirmación/decisión de los equipos propietarios y reflejar los contratos acordados.
- Las secciones 15–22 permanecen en el cambio activo.
- No se modificaron otros microservicios.

## Sincronización

No se sincronizan los deltas de `ms-beca/fundacion-servicio` ni `ms-beca/otorgamiento-beca`: describen el cambio bootstrap completo y permanecen bajo `openspec/changes/tech-bootstrap-int4-msbeca/`. Este histórico no modifica las especificaciones principales.
