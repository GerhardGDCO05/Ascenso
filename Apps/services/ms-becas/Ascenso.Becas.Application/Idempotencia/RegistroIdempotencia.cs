namespace Ascenso.Becas.Application.Idempotencia;

/// <summary>
/// Registro de una solicitud idempotente con la respuesta que debe replicarse.
/// </summary>
public sealed record RegistroIdempotencia(
    Guid Id,
    string Endpoint,
    string Clave,
    string HashPayload,
    string Estado,
    int? StatusCode,
    string? ResponseBody,
    Guid? RecursoId,
    DateTimeOffset CreadoEn,
    DateTimeOffset ExpiraEn);
