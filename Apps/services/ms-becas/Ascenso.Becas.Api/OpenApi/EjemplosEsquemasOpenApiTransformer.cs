using System.Text.Json.Nodes;
using Ascenso.Becas.Application.Otorgamiento.Dtos;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Ascenso.Becas.Api.OpenApi;

/// <summary>
/// Adjunta ejemplos representativos a los esquemas de respuesta y error más
/// relevantes del contrato.
/// </summary>
public sealed class EjemplosEsquemasOpenApiTransformer
    : IOpenApiSchemaTransformer
{
    private static readonly Dictionary<Type, string> _ejemplos =
        new()
        {
            [typeof(OfertaEmitidaDto)] = """
                {
                  "ofertaId": "0192f0c4-7a3e-7c1a-9b2d-4f5e6a7b8c9d",
                  "becaId": "0192f0c4-8b4f-7d2b-ac3e-5f6a7b8c9d0e",
                  "estado": "PENDIENTE_RESPUESTA",
                  "fechaVencimientoAceptacion": "2026-12-01T15:30:00+00:00"
                }
                """,
            [typeof(OfertaRespondidaDto)] = """
                {
                  "ofertaId": "0192f0c4-7a3e-7c1a-9b2d-4f5e6a7b8c9d",
                  "becaId": "0192f0c4-8b4f-7d2b-ac3e-5f6a7b8c9d0e",
                  "estadoOferta": "ACEPTADA",
                  "estadoBeca": "ACTIVA"
                }
                """,
            [typeof(CompromisoCargadoDto)] = """
                {
                  "compromisoId": "0192f0c4-9c5a-7e3c-bd4f-6a7b8c9d0e1f",
                  "numeroIntentoCarga": 1,
                  "estado": "EN_REVISION"
                }
                """,
            [typeof(CompromisoRevisadoDto)] = """
                {
                  "compromisoId": "0192f0c4-9c5a-7e3c-bd4f-6a7b8c9d0e1f",
                  "estado": "APROBADO",
                  "habilitadaParaDesembolso": true
                }
                """,
            [typeof(BecaActivaDto)] = """
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
                """,
            [typeof(Microsoft.AspNetCore.Mvc.ProblemDetails)] = """
                {
                  "type": null,
                  "title": "Solicitud inválida",
                  "status": 400,
                  "detail": "La solicitud contiene errores de validación.",
                  "instance": "/api/v1/becas/ofertas",
                  "code": "SOLICITUD_INVALIDA",
                  "correlationId": "4f2a9c1e8b7d4e3f9a6b5c8d1e2f3a4b",
                  "errors": {
                    "Justificacion": [
                      "La justificación es obligatoria."
                    ]
                  }
                }
                """
        };

    public Task TransformAsync(
        OpenApiSchema schema,
        OpenApiSchemaTransformerContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(context);

        if (_ejemplos.TryGetValue(context.JsonTypeInfo.Type, out var ejemplo))
        {
            schema.Examples = [JsonNode.Parse(ejemplo)!];
        }

        return Task.CompletedTask;
    }
}
