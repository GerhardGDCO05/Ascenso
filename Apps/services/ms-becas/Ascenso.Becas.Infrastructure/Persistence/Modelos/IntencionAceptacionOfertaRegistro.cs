namespace Ascenso.Becas.Infrastructure.Persistence.Modelos;

/// <summary>Snapshot durable de una aceptación pendiente.</summary>
public sealed class IntencionAceptacionOfertaRegistro
{
    public Guid OfertaId { get; set; }

    public Guid BecaId { get; set; }

    public string Payload { get; set; } = string.Empty;

    public DateTimeOffset CreadoEn { get; set; }
}
