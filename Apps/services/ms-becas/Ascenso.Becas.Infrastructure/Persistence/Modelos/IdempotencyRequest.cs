namespace Ascenso.Becas.Infrastructure.Persistence.Modelos;

/// <summary>
/// Registro de una solicitud idempotente, con su respuesta y vigencia.
/// </summary>
public sealed class IdempotencyRequest
{
    public Guid Id { get; set; }

    public string Endpoint { get; set; } = string.Empty;

    public string Clave { get; set; } = string.Empty;

    public string HashPayload { get; set; } = string.Empty;

    public string Estado { get; set; } = string.Empty;

    public int? StatusCode { get; set; }

    public string? ResponseBody { get; set; }

    public Guid? RecursoId { get; set; }

    public DateTimeOffset CreadoEn { get; set; }

    public DateTimeOffset ExpiraEn { get; set; }
}
