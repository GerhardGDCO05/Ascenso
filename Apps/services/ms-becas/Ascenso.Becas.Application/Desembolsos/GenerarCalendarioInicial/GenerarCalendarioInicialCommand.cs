using Ascenso.Becas.Application.Despacho;

namespace Ascenso.Becas.Application.Desembolsos.GenerarCalendarioInicial;

public sealed record GenerarCalendarioInicialCommand(Guid BecaId) : ICommand<bool>;
