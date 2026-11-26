# Tasks

## 1. Preparación del módulo y dependencias

- [x] 1.1 Agregar `Microsoft.AspNetCore.Authentication.JwtBearer` a `Directory.Packages.props` y referenciarlo en `Ascenso.Programas.Api.csproj`; verificar con `dotnet restore` y `dotnet build`.
- [x] 1.2 Agregar la sección `Keycloak` en `Ascenso.Programas.Api/appsettings.json` con `Authority`, `Audience` y `RequireHttpsMetadata`, sin secretos; verificar que la API arranca sin `Authority` y que una ruta protegida responde `401`.
- [x] 1.3 Documentar en `design.md` o en un archivo de contrato la ruta acordada de `GET /internal/v1/periodos-academicos/{id}` y el nombre del claim `permissions`; verificar que el contrato queda referenciado por `IPeriodoAcademicoClient` y el proveedor de políticas.
- [x] 1.4 Crear la estructura vacía `Domain/Convocatorias/{Agregados,Entidades,ObjetosValor,Eventos}` y `Application/Convocatorias/{Comandos,Consultas,DTOs,Abstracciones}`; verificar que la solución compila sin archivos temporales.

## 2. Dominio

- [x] 2.1 Crear `EstadoConvocatoria` con `BORRADOR`, `PUBLICADA`, `CERRADA` y `CANCELADA`, y las excepciones de dominio de convocatorias; verificar con pruebas unitarias de serialización/estado.
- [x] 2.2 Implementar `CriteriosElegibilidadCiclo` como objeto de valor inmutable con validación de promedio `[0.00, 20.00]`, edades `[0, 120]` con `edadMinima <= edadMaxima`, tope de ingreso `>= 0` y listas no vacías; verificar con pruebas de valores límite y valores inválidos.
- [x] 2.3 Implementar `DocumentoRequerido` como entidad hija con tipo, nombre, descripción, obligatoriedad, MIME y tamaño máximo; verificar con pruebas de creación y validación.
- [x] 2.4 Implementar el agregado `Convocatoria` con `Crear`, `Actualizar`, `AgregarDocumento`, `QuitarDocumento`, `Publicar` y `Cerrar`, sin setters públicos, con normalización de código y registro de eventos de dominio; verificar con pruebas de transiciones válidas e inválidas.
- [x] 2.5 Implementar las reglas de cronología del dominio sobre instantes normalizados: `apertura < cierre`, `cierre <= límiteSubsanación`, `inicioEvaluación < cierreEvaluación`, `cierreEvaluación > límiteSubsanación` y la regla de revisión previa; verificar con pruebas de cada combinación inválida.
- [x] 2.6 Crear `IConvocatoriaRepository` con operaciones de obtención, existencia de código, alta, actualización y consulta paginada; verificar que el dominio compila sin referencias a EF Core, ASP.NET, MassTransit, Npgsql ni Keycloak.
- [x] 2.7 Ejecutar las pruebas de dominio y arquitectura; verificar con `dotnet test tests/Ascenso.Programas.Domain.Tests` y `dotnet test tests/Ascenso.Programas.ArchitectureTests`.

## 3. Aplicación

- [x] 3.1 Crear los puertos `ICatalogoConvocatoriaReader`, `IPeriodoAcademicoClient` y el componente `CalendarioConvocatoria` para normalizar fechas de `America/Caracas`; verificar con pruebas unitarias de conversión UTC-4 y de cada fecha límite.
- [x] 3.2 Implementar `CrearConvocatoriaBorradorCommand`, su validador y manejador; verificar con pruebas de creación exitosa, programa no habilitado, periodo no vigente, cupos insuficientes, convenio inválido y criterios fuera de rango.
- [x] 3.3 Implementar `EditarConvocatoriaBorradorCommand`, su validador y manejador; verificar con pruebas de edición exitosa y `409 CONVOCATORIA_NO_EDITABLE` sobre estados no borrador.
- [x] 3.4 Implementar `PublicarConvocatoriaCommand` con revalidación de cupos, programa, periodo y documentos, y fecha de cierre futura; verificar con pruebas de publicación exitosa y `422` por cada precondición.
- [x] 3.5 Implementar `AgregarDocumentoRequeridoCommand` y `EliminarDocumentoRequeridoCommand` restringidos a borrador; verificar con pruebas de alta, baja y rechazo sobre convocatoria publicada.
- [x] 3.6 Implementar `ObtenerConvocatoriaPorIdQuery`, `ConsultarConvocatoriasQuery` y `ConsultarConvocatoriasAbiertasQuery` con DTOs paginados; verificar con pruebas de filtros, paginación y detalle.
- [x] 3.7 Registrar manejadores, validadores y puertos en `Ascenso.Programas.Application/DependencyInjection.cs`; verificar con `dotnet test tests/Ascenso.Programas.Application.Tests`.

## 4. Persistencia

- [x] 4.1 Crear `ConvocatoriaConfiguracion` y `DocumentoRequeridoConfiguracion` con tablas, columnas, `jsonb` para listas, auditoría y `CHECK` de cupos, edades, promedio, fechas y estado; verificar el modelo con una prueba de modelo de persistencia.
- [x] 4.2 Agregar `DbSet<Convocatoria>` y `DbSet<DocumentoRequerido>` a `ProgramasDbContext`, y traducir la violación de `ux_convocatoria_codigo` a `409 CODIGO_CONVOCATORIA_DUPLICADO`; verificar con una prueba de traducción de excepción.
- [x] 4.3 Implementar `ConvocatoriaRepository` y el adaptador `CatalogoConvocatoriaReader` con bloqueo `FOR UPDATE` del programa; verificar con pruebas de integración de persistencia, concurrencia y lectura de convenios.
- [x] 4.4 Implementar `PeriodoAcademicoClient` contra `MS-PLT` con timeout y *fail-closed*; verificar con WireMock un periodo vigente, uno inactivo y una respuesta no disponible (`503`).
- [x] 4.5 Generar y aplicar la migración `Convocatorias` en PostgreSQL; verificar con `dotnet ef database update` y una prueba que confirme índice único y restricciones `CHECK`.
- [x] 4.6 Ejecutar las pruebas de persistencia e integración de infraestructura; verificar con `dotnet test tests/Ascenso.Programas.IntegrationTests`.

## 5. API y seguridad

- [x] 5.1 Configurar JWT Bearer de Keycloak en `Program.cs` y registrar `PermissionPolicyProvider` antes de `AddAuthorization`; verificar que las políticas existentes siguen resolviéndose y que una política nueva exige el claim `permissions`.
- [x] 5.2 Implementar `ConvocatoriasController` público con los endpoints de creación, edición, consulta, publicación y documentos, aplicando las políticas `convocatorias:*`; verificar con pruebas de integración de cada código HTTP.
- [x] 5.3 Implementar `ConvocatoriasController` interno con `GET /internal/v1/convocatorias/{id}` y `/abiertas` protegido por `convocatorias:leer`; verificar con pruebas de contrato, `401` sin token y `403` sin permiso.
- [x] 5.4 Implementar `ManejadorResultadoAutorizacionConvocatorias` para responder `403` con `codigo = PERMISO_DENEGADO`; verificar con pruebas de integración de permisos permitidos, ausentes y token inválido.
- [x] 5.5 Agregar documentación OpenAPI/Swagger de los endpoints y sus códigos de error; verificar que `dotnet build` no reporta advertencias de contrato y que el JSON de OpenAPI incluye las rutas.

## 6. Cierre automático

- [x] 6.1 Configurar Hangfire con almacenamiento PostgreSQL y servidor en `MS-PRG`; verificar con una prueba de arranque y con la creación de las tablas de Hangfire.
- [x] 6.2 Implementar `CerrarConvocatoriasVencidasJob` recurrente cada minuto, idempotente y con emisión de `ConvocatoriaCerrada`; verificar con una prueba que cierra una convocatoria vencida y no modifica una futura.
- [x] 6.3 Ejecutar las pruebas del job y de integración con el programador; verificar que dos ejecuciones consecutivas no duplican cierres ni eventos.

## 7. Contrato interno, documentación y verificación integral

- [x] 7.1 Documentar el contrato de lectura para `MS-POS` en `docs/contratos/ms-prg/convocatorias.md` con rutas, DTOs, errores y dependencia de periodo; verificar que el documento coincide con los endpoints implementados.
- [x] 7.2 Actualizar `openspec/roadmaps/por_microservicio.md` y `openspec/roadmaps/por_integrante.md` para reflejar la implementación de `RF-02` en `MS-PRG`, dejando explícito que `MS-POS` solo consume el contrato; verificar que el roadmap muestra la HU 2.1 en el microservicio correcto.
- [x] 7.3 Ejecutar `dotnet build` y `dotnet test` de la solución `Ascenso.Programas.sln`; verificar que todas las pruebas pasan.
- [x] 7.4 Ejecutar `openspec validate rf2-1-int1-msprg --strict`; verificar que no hay errores ni advertencias bloqueantes.

## Workflow follow-up

- Obtener la aprobación explícita del responsable de `MS-PRG` antes de aplicar el cambio, por tratarse de un microservicio de otro integrante.
- Coordinar con el equipo de identidad el mapper del claim `permissions` y con `MS-PLT` el endpoint interno de periodo por identificador.
- Archivar el cambio después de la revisión y sincronizar la especificación `openspec/specs/ms-prg/convocatorias/spec.md`.
