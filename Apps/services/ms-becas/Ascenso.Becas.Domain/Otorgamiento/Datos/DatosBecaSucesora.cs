using Ascenso.Becas.Domain.ValueObjects;

namespace Ascenso.Becas.Domain.Otorgamiento.Datos;

public sealed record class DatosBecaSucesora(
    Guid PeriodoSolicitadoId,
    Guid CostoMatriculaReferenciaSnapshotId,
    decimal MontoComprometidoPeriodo,
    Guid ReservaPresupuestoConfirmadaId,
    PoliticaCobertura CoberturaAplicada,
    CondicionesPermanencia CondicionesSnapshot,
    bool RequiereCompromisoFirmado,
    bool RequiereNuevaFirma,
    string CodigoBeca,
    string? UrlPlantillaCompromiso);
