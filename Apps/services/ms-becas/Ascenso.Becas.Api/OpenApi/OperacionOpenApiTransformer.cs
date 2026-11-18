using System.Text.Json.Nodes;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Ascenso.Becas.Api.OpenApi;

/// <summary>
/// Documenta el encabezado <c>Idempotency-Key</c> en todos los comandos
/// <c>POST</c> de la API y adjunta ejemplos de solicitud y respuesta en las
/// operaciones principales.
/// </summary>
public sealed class OperacionOpenApiTransformer : IOpenApiOperationTransformer
{
    public const string NombreEncabezadoIdempotencia = "Idempotency-Key";

    private const string EjemploEmision =
        """
        {
          "postulacionId": "0192f0c4-1234-7abc-8def-0123456789ab"
        }
        """;

    private const string EjemploAnulacion =
        """
        {
          "justificacion": "La postulación fue retirada por el estudiante."
        }
        """;

    private const string EjemploRevision =
        """
        {
          "aprobar": true,
          "observaciones": "Documento firmado y legible."
        }
        """;

    private const string EjemploRespuestaRevision =
        """
        {
          "compromisoId": "0192f0c4-9c5a-7e3c-bd4f-6a7b8c9d0e1f",
          "estado": "APROBADO",
          "habilitadaParaDesembolso": true
        }
        """;

    private const string EjemploRespuestaEmision =
        """
        {
          "ofertaId": "0192f0c4-7a3e-7c1a-9b2d-4f5e6a7b8c9d",
          "becaId": "0192f0c4-8b4f-7d2b-ac3e-5f6a7b8c9d0e",
          "estado": "PENDIENTE_RESPUESTA",
          "fechaVencimientoAceptacion": "2026-12-01T15:30:00+00:00"
        }
        """;

    private const string EjemploBecasActivas =
        """
        [
          {
            "becaId": "0192f0c4-8b4f-7d2b-ac3e-5f6a7b8c9d0e",
            "programaBecaId": "0192f0c4-a1b2-7c3d-8e4f-5a6b7c8d9e0f",
            "periodoAcademicoId": "0192f0c4-b2c3-7d4e-9f5a-6b7c8d9e0f10",
            "estado": "ACTIVA",
            "modalidadCobertura": "PARCIAL",
            "porcentajeCobertura": 50,
            "montoFijoPeriodo": 0,
            "moneda": "USD",
            "permiteAcumulacion": false
          }
        ]
        """;

    public Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(context);

        var esPost = string.Equals(
            context.Description.HttpMethod,
            HttpMethods.Post,
            StringComparison.OrdinalIgnoreCase);

        if (esPost)
        {
            AgregarIdempotencia(operation);
        }

        var ruta = context.Description.RelativePath?.TrimStart('/');

        switch (ruta)
        {
            case "api/v1/becas/ofertas" when esPost:
                EstablecerEjemploRequestBody(operation, EjemploEmision);
                EstablecerEjemploRespuesta(operation, "201", EjemploRespuestaEmision);
                break;
            case "api/v1/becas/ofertas/{ofertaId}/anular" when esPost:
                EstablecerEjemploRequestBody(operation, EjemploAnulacion);
                break;
            case "api/v1/becas/compromisos/{compromisoId}/revision" when esPost:
                EstablecerEjemploRequestBody(operation, EjemploRevision);
                EstablecerEjemploRespuesta(
                    operation,
                    "200",
                    EjemploRespuestaRevision);
                break;
            case "internal/v1/becas/activas" when !esPost:
                EstablecerEjemploRespuesta(operation, "200", EjemploBecasActivas);
                break;
        }

        return Task.CompletedTask;
    }

    private static void AgregarIdempotencia(OpenApiOperation operation)
    {
        operation.Parameters ??= [];

        if (operation.Parameters.Any(parametro =>
                string.Equals(
                    parametro.Name,
                    NombreEncabezadoIdempotencia,
                    StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        operation.Parameters.Add(new OpenApiParameter
        {
            Name = NombreEncabezadoIdempotencia,
            In = ParameterLocation.Header,
            Required = true,
            Description =
                "Clave única de idempotencia de 8 a 128 caracteres. "
                + "Reintentar con la misma clave y el mismo payload devuelve "
                + "la respuesta original.",
            Schema = new OpenApiSchema
            {
                Type = JsonSchemaType.String,
                MinLength = 8,
                MaxLength = 128
            }
        });
    }

    private static void EstablecerEjemploRequestBody(
        OpenApiOperation operation,
        string ejemplo)
    {
        if (operation.RequestBody?.Content is { } contenidos
            && contenidos.TryGetValue("application/json", out var contenido))
        {
            contenido.Example = JsonNode.Parse(ejemplo);
        }
    }

    private static void EstablecerEjemploRespuesta(
        OpenApiOperation operation,
        string codigo,
        string ejemplo)
    {
        if (operation.Responses is not null
            && operation.Responses.TryGetValue(codigo, out var respuesta)
            && respuesta.Content is not null
            && respuesta.Content.TryGetValue("application/json", out var contenido))
        {
            contenido.Example = JsonNode.Parse(ejemplo);
        }
    }
}

