    # Tasks: Perfil Socioeconómico y Grupo Familiar (`rf1-2-int1-mspos`)

    ## 1. Dominio y catálogos

    - [x] 1.1 Crear `EstadoCivil`, `Parentesco` y `Ocupacion` en `Ascenso.Postulaciones.Domain/Enums/` con los valores aprobados; verificar con pruebas de `CodigoEnum` que serializan como `SOLTERO`, `PADRE`, `LABORES_HOGAR`, `NO_TRABAJA`, etc.
    - [x] 1.2 Crear las excepciones de dominio para catálogo inválido, datos socioeconómicos inválidos, miembro familiar inválido y condición laboral incoherente; verificar con pruebas que exponen el `Codigo` y `EstadoHttp` esperados.
    - [x] 1.3 Implementar `DatosMiembroFamiliar` como objeto de valor inmutable con validación de `Parentesco`, `Edad` (`0–120`), `Ocupacion` e `IngresoMensual` (`>= 0`, dos decimales); verificar con pruebas unitarias de valores válidos e inválidos.
    - [x] 1.4 Implementar `MiembroFamiliar` como entidad auditable con `Id` UUIDv7, `PostulanteId`, `Parentesco`, `Edad`, `Ocupacion`, `IngresoMensual` y constructor privado para EF Core; verificar con pruebas unitarias de creación y rechazos.
    - [x] 1.5 Ampliar `Postulante` con los campos socioeconómicos nullable, la colección de solo lectura `MiembrosFamiliares` y el método `RegistrarPerfilSocioeconomico(...)`; verificar con pruebas de cálculo con lista vacía, con familiares, transición a `COMPLETO`, coherencia laboral, rangos y actualización de auditoría/versión.
    - [x] 1.6 Actualizar `docs/Explicacion_del_DDD_V7.md` (sub-modelo 2.3.A y `RN-POS-01`) para documentar los datos laborales del postulante, la composición familiar opcional, los catálogos y la nueva regla de completitud; verificar que el texto coincide con el dominio implementado.

    ## 2. Aplicación

    - [x] 2.1 Crear `ActualizarPerfilSocioeconomicoCommand`, los comandos de familiares, `MiembroFamiliarDto` y ampliar `PostulanteDto`/`PostulanteMapeador` con los nuevos campos y cálculos; verificar que `Application` compila sin referencias a EF Core ni ASP.NET Core.
    - [x] 2.2 Implementar `ActualizarPerfilSocioeconomicoCommandValidator` con FluentValidation para obligatoriedad, catálogos, rangos, decimales, coherencia laboral y validación de cada familiar; verificar con pruebas unitarias de todos los caminos inválidos.
    - [x] 2.3 Implementar `ActualizarPerfilSocioeconomicoCommandHandler`: resolver usuario autenticado, cargar perfil con tracking, lanzar `404` si no existe, registrar la sección, guardar y devolver `PostulanteDto`; verificar con pruebas de éxito con lista vacía, con familiares, perfil inexistente y catálogo inválido.
    - [x] 2.4 Registrar el nuevo handler en `Application/DependencyInjection.cs`; verificar con una prueba de contenedor que el handler y el validador se resuelven.

    ## 3. Persistencia y migración

    - [x] 3.1 Ampliar `PostulanteConfiguracion` con las columnas nullable, conversiones de enums, restricciones `CHECK`, relación con familiares y campo de respaldo; verificar con las pruebas de modelo que las columnas, precisión, nulabilidad y restricciones son correctas.
    - [x] 3.2 Crear `MiembroFamiliarConfiguracion` para la tabla `miembros_familiares`, con PK, FK a `postulantes`, borrado en cascada, índice por `postulante_id`, auditoría y `CHECK` de catálogos, edad e ingreso; verificar con las pruebas de modelo.
    - [x] 3.3 Agregar `DbSet<MiembroFamiliar>` y ampliar `PostulanteRepository` con `Include` de familiares para lectura y un método de actualización con tracking; verificar con pruebas de repositorio o de modelo que ambas consultas incluyen la colección.
    - [x] 3.4 Generar la migración `PerfilSocioeconomicoGrupoFamiliar` con `dotnet-ef`; verificar con `dotnet ef migrations has-pending-model-changes` y con una prueba de que el script contiene la tabla, columnas, FK, índice y restricciones.
    - [x] 3.5 Verificar la concurrencia optimista en la actualización del perfil; comprobar que un conflicto de versión se traduce en `409 Conflict` mediante `ExcepcionConflictoConcurrencia`.

    ## 4. API y contrato

    - [x] 4.1 Agregar `PUT /api/v1/postulantes/mio/socioeconomico` a `PostulantesController` con `[Authorize]`, request anidado y documentación de respuestas `200`, `400`, `401`, `404`, `409` y `422`; verificar con pruebas de endpoint para cada código.
    - [x] 4.2 Ampliar la respuesta de `GET /api/v1/postulantes/mio` con los datos socioeconómicos, cálculos y familiares; verificar con pruebas de perfil completo, perfil incompleto de HU 1.1 y lista familiar vacía.
    - [x] 4.3 Verificar que el `usuarioId` se obtiene del token y que el cliente no puede actualizar el perfil de otro usuario; cubrir con una prueba de endpoint de aislamiento.
    - [x] 4.4 Verificar que Swagger documenta el nuevo endpoint y los esquemas de request/response; comprobar `/swagger/v1/swagger.json` en Development.

    ## 5. Integración con PostgreSQL local

    - [x] 5.1 Aplicar la migración en la base local `postulaciones_db` del contenedor `postgres-mspos` (puerto `5433`); verificar que la tabla y las columnas existen.
    - [x] 5.2 Ejecutar un flujo manual: crear perfil base, actualizar sección socioeconómica sin familiares, consultar perfil y comprobar `numeroIntegrantesFamilia = 1` e ingreso total igual al del postulante; limpiar los datos de prueba.
    - [x] 5.3 Ejecutar el mismo flujo con dos familiares y comprobar `numeroIntegrantesFamilia = 3`, el ingreso total y las filas de `miembros_familiares`; limpiar los datos de prueba.

    ## 6. Verificación integral y seguimiento

    - [x] 6.1 Ejecutar `dotnet build "Apps/services/ms-postulaciones/Ascenso.Postulaciones.sln" --configuration Release` y `dotnet test` de la misma solución, verificando `0` advertencias y todas las pruebas en verde.
    - [x] 6.2 Ejecutar `dotnet format "Apps/services/ms-postulaciones/Ascenso.Postulaciones.sln" --verify-no-changes --no-restore` y `dotnet restore --locked-mode`, verificando salida limpia.
    - [x] 6.3 Ejecutar `openspec validate rf1-2-int1-mspos --strict` y `openspec validate --specs`, verificando que no hay hallazgos.
    - [x] 6.4 Redactar la bitácora `openspec/changes/rf1-2-int1-mspos/log-YYYY-MM-DD-rf1-2-int1-mspos.md` con la evidencia real de comandos y resultados, y actualizar `openspec/roadmaps/por_integrante.md` y `openspec/roadmaps/por_microservicio.md` marcando solo la HU 1.2.

    ## Workflow follow-up

    - Archivar el cambio cuando la revisión del equipo esté aprobada y todas las tareas estén en `[x]`.
    - Sincronizar el delta hacia `openspec/specs/ms-pos/postulantes/spec.md` al archivar.
    - Mantener `RF-01` sin marcar como completo hasta cubrir sus demás HUs.
