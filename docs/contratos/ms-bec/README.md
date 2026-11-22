# Contratos de MS-BEC

- [OpenAPI](./openapi.json): endpoints públicos e internos publicados por la API.
- [Eventos](./eventos.md): sobres, exchanges y payloads de integración.
- [Identidad e integraciones HTTP](./identidad-integraciones.md): claims de Keycloak y contratos consumidos de MS-POS/MS-PRG.

El documento OpenAPI se genera desde el endpoint `/swagger/v1/swagger.json`
usando el host de pruebas de la API. Para actualizarlo:

```bash
dotnet run --project Apps/services/ms-becas/tools/ExportOpenApi/ExportOpenApi.csproj -- docs/contratos/ms-bec/openapi.json
```

`OpenApiDocumentoTests` verifica las rutas, el esquema de seguridad, los
encabezados de idempotencia y los ejemplos publicados. Los contratos de
identidad e integraciones quedan sujetos a la validación de sus equipos
propietarios antes de considerarse congelados.
