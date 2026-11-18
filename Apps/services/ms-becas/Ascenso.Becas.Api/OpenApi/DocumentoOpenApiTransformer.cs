using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Ascenso.Becas.Api.OpenApi;

/// <summary>
/// Publica el esquema de seguridad Bearer de Keycloak y lo aplica globalmente
/// al documento OpenAPI.
/// </summary>
public sealed class DocumentoOpenApiTransformer : IOpenApiDocumentTransformer
{
    public const string NombreEsquemaSeguridad = "Bearer";

    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(document);

        document.Info.Title = "MS-BEC — API de Becas";
        document.Info.Description =
            "Otorgamiento, compromisos firmados y consulta interna de becas.";

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??=
            new Dictionary<string, IOpenApiSecurityScheme>();

        document.Components.SecuritySchemes[NombreEsquemaSeguridad] =
            new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description =
                    "JWT emitido por Keycloak. Los clientes internos usan un "
                    + "Service Token con el alcance becas.internal.read."
            };

        document.Security ??= [];
        document.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference(NombreEsquemaSeguridad, document)] = []
        });

        return Task.CompletedTask;
    }
}
