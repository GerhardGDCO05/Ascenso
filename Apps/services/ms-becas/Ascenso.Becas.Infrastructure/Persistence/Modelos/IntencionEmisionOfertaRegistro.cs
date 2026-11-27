namespace Ascenso.Becas.Infrastructure.Persistence.Modelos;

/// <summary>Payload preparado que permite repetir una emisión tras una caída.</summary>
public sealed class IntencionEmisionOfertaRegistro
{
    public Guid OfertaId { get; set; }

    public Guid PostulacionId { get; set; }

    public string Payload { get; set; } = string.Empty;

    public DateTimeOffset CreadoEn { get; set; }
}
