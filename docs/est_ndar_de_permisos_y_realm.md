# Propuesta de Estándar de Permisos (PBAC) y Realm

## 1. Un solo `realm` para todo el proyecto
* Mantenemos **un único `realm`** (`ascenso-realm.json`) para toda la plataforma, de modo que usuarios, roles y sesiones vivan centralizados sin duplicar configuraciones ni romper tokens existentes.
* La fuente compartida es `deploy/keycloak/ascenso-realm.json`. El fixture de desarrollo de MS-BEC se genera desde ella con `scripts/generar-realm-local.py` y un overlay local separado. El responsable de identidad/Keycloak revisa y aplica la configuración en el entorno común; importar el fixture local no equivale a desplegar el realm del equipo.
* La configuración compartida no contiene usuarios de demostración ni secretos. Los clientes de otros microservicios se habilitan tras acordar sus audiencias, flujos y credenciales con sus responsables.

## 2. Roles dinámicos vs. Permisos en el código
* **Keycloak / `MS-PLT`:** Keycloak es el proveedor de identidad y emite los claims; MS-PLT es dueño del RBAC de dominio (`Usuario`, `Rol`, `Permiso`, `AsignacionRol`) según el DDD. La sincronización de asignaciones al realm se coordina con el responsable de identidad.
* **Microservicios:** Validan **Permisos** (`recurso:accion`, en minúsculas y sin tildes), en vez de nombres fijos de roles, para evitar redespliegues cuando aparezcan nuevos cargos.
* **Adopción gradual:** Se aplica primero en lo nuevo (como `convocatorias` en `ms-programas`) y los demás servicios migran cuando les toque, sin romper las políticas que ya están funcionando.
* El catálogo `ascenso-permissions` contiene los permisos de convocatorias como roles de cliente. El mapper del scope homónimo emite únicamente esos permisos en `permissions` como arreglo de strings; no convierte los roles de realm en permisos. Se asignan según el RBAC oficial, sin inferir privilegios a partir del nombre del cargo.
* El catálogo compartido también declara los permisos base propuestos para programas, postulaciones, evaluaciones e identidad, pero no los asigna ni activa políticas en esos MS. Al incorporar un cliente nuevo, identidad debe asociarle el scope `ascenso-permissions`, configurar su audiencia propia y comprobar que el JWT resultante satisface al consumidor. La audiencia `ascenso-api` se conserva para el consumidor actual MS-POS; `ms-bec` sigue siendo la audiencia de MS-BEC.

**Alcance de HU 2.1 en MS-PRG:** aplicar PBAC únicamente a `convocatorias:*` (`crear`, `leer`, `editar`, `publicar`). `convocatorias:cerrar` queda reservado para el job o un endpoint futuro; no habrá cierre manual en esta HU. El claim `roles` se conserva y las políticas existentes basadas en roles no se migran; `programas:*` y los demás permisos quedan para sus cambios asignados.

## 3. Formato del token JWT
Se mantiene `roles` intacto y se agrega `permissions` como un **arreglo real de strings** (no un string con JSON adentro):

```json
{
  "sub": "11111111-1111-1111-1111-111111111111",
  "roles": ["COORDINADOR_PROGRAMA"],
  "permissions": [
    "convocatorias:crear",
    "convocatorias:leer",
    "convocatorias:editar",
    "convocatorias:publicar"
  ]
}
```

## 4. Permisos base propuestos
* **`ms-programas`:** `convocatorias:crear`, `convocatorias:leer`, `convocatorias:editar`, `convocatorias:publicar`, `convocatorias:cerrar` y `programas:crear`, `programas:leer`, `programas:editar`, `programas:asignar_cupos`.
* **`ms-postulaciones`:** `postulaciones:crear`, `postulaciones:leer_propias`, `postulaciones:leer_todas`, `postulaciones:revisar_documentos`.
* **`ms-evaluaciones`:** `evaluaciones:calificar`, `evaluaciones:aprobar_beca`, `evaluaciones:rechazar_beca`.
* **`ms-iam`:** `roles:gestionar`, `usuarios:asignar_roles`.

## 5. Qué hacemos mientras tanto
* El realm compartido declara el catálogo y mapper de `permissions`; la validación PBAC ya existe para convocatorias en MS-PRG. El fixture local concede esos permisos al coordinador de demostración para probar el claim.
* Se mantienen `roles`, `postulante_id`, `programa_ids`, las audiencias `ms-bec` y `ascenso-api`, y los Service Tokens de MS-BEC. Los consumidores de otros MS deben acordar sus clientes y audiencias antes de activar el realm común.
* Los seis roles internos de RN-PLT-01 heredan la marca técnica `EXIGE_2FA`. Los flujos de navegador y direct grant del realm exigen OTP cuando el usuario tiene esa marca, incluso si aún no registró una credencial. El flujo direct grant está deshabilitado en la base compartida; el fixture local lo habilita únicamente para demostraciones. La configuración se importó en Keycloak 26 y se verificó la herencia de los seis roles; antes de aplicarla al entorno del equipo, identidad/MS-PLT debe probar el enrolamiento TOTP por navegador y su política de usuarios nuevos.

## 6. Contrato interno de periodo académico (MS-PLT)

MS-PRG consulta por identificador el periodo elegido antes de crear o publicar una convocatoria:

```http
GET /internal/v1/periodos-academicos/{id}
```

Respuesta `200 OK`:

```json
{
  "id": "22222222-2222-2222-2222-222222222222",
  "nombre": "2026-I",
  "estado": "VIGENTE",
  "fechaInicio": "2026-01-01",
  "fechaFin": "2026-06-30"
}
```

- Estados esperados: `PROGRAMADO`, `VIGENTE`, `FINALIZADO`.
- Responder `404 Not Found` cuando el identificador no exista.
- Si MS-PLT no responde o devuelve una respuesta inválida, MS-PRG falla cerrado con `503 Service Unavailable` y no crea ni publica la convocatoria.
- La URL base se configura en MS-PRG mediante `ServiciosExternos:PeriodoAcademico:UrlBase`; `TimeoutSegundos` configura el tiempo límite.
- La ruta es interna. El mecanismo de autenticación entre servicios (service token de Keycloak u otro control interno) debe confirmarse con el responsable de MS-PLT; este contrato no requiere cambiar el realm compartido.
