using Ascenso.Becas.Application.Despacho;
using Ascenso.Becas.Domain.Enums;

namespace Ascenso.Becas.Application.Otorgamiento.Comandos.CicloVidaBeca;

public sealed record SuspenderBecaCommand(
    Guid BecaId, TipoSuspensionBeca Tipo, string Motivo,
    string RequisitosParaReanudar, DateOnly FechaInicio,
    DateOnly? FechaRevision, bool EsIndefinida, string Justificacion)
    : ICommand<Guid>;

public sealed record ReanudarBecaCommand(Guid BecaId, string Justificacion)
    : ICommand<Guid>;

public sealed record RevocarBecaDefinitivamenteCommand(
    Guid BecaId, string Justificacion, string CodigoOtp)
    : ICommand;
