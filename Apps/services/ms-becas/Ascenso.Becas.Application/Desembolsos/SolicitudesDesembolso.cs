using Ascenso.Becas.Application.Despacho;
using Ascenso.Becas.Domain.Desembolsos.Datos;
using Ascenso.Becas.Domain.Enums;

namespace Ascenso.Becas.Application.Desembolsos;

public sealed record ProgramarCalendarioDesembolsoCommand(
    Guid BecaId, Guid PeriodoAcademicoId, FrecuenciaDesembolso Frecuencia,
    DestinatarioPago Destinatario, decimal MontoMaximoAprobadoPeriodo,
    DateOnly FechaInicio, DateOnly FechaFin, IReadOnlyList<CuotaProgramada> Cuotas)
    : ICommand<Guid>;

public sealed record ActualizarDatosPagoBecarioCommand(
    Guid BecaId, string NombreBanco, string CodigoBanco, TipoCuentaBancaria TipoCuenta,
    string NumeroCuenta, string DocumentoTitular, string CodigoOtp)
    : ICommand<Guid>;

public sealed record RegistrarEjecucionDesembolsoCommand(
    Guid CalendarioId, int NumeroCuota, decimal Monto, DateOnly FechaEjecucion,
    DestinatarioPago Destinatario, Guid? DatosPagoBecarioId,
    string CuentaDestinoCifrada, MetodoPago MetodoPago, string ReferenciaBancaria)
    : ICommand<DesembolsoDto>;

public sealed record ConfirmarDesembolsoCommand(
    Guid CalendarioId, int NumeroCuota, string CodigoOtp)
    : ICommand<DesembolsoDto>;

public sealed record RegistrarIncidenciaDesembolsoCommand(
    Guid CalendarioId, int NumeroCuota, EstadoDesembolso Estado,
    string Motivo, string ObservacionBanco)
    : ICommand<Guid>;

public sealed record ResolverIncidenciaDesembolsoCommand(
    Guid CalendarioId, int NumeroCuota, Guid IncidenciaId)
    : ICommand<DesembolsoDto>;

public sealed record ReintentarDesembolsoCommand(Guid CalendarioId, int NumeroCuota)
    : ICommand<DesembolsoDto>;

public sealed record ObtenerCalendarioDesembolsosQuery(Guid BecaId, int Pagina = 1, int TamanoPagina = 50)
    : IQuery<IReadOnlyList<CalendarioDesembolsoDto>>;

public sealed record ObtenerDesembolsosQuery(Guid CalendarioId, int Pagina = 1, int TamanoPagina = 50)
    : IQuery<IReadOnlyList<DesembolsoDto>>;

public sealed record DesembolsoDto(
    Guid Id, Guid CalendarioId, int NumeroCuota, decimal MontoProgramado,
    decimal? MontoEjecutado, DateOnly FechaProgramada, string Estado,
    bool EsVigente, Guid? DesembolsoOrigenReintentoId);

public sealed record CalendarioDesembolsoDto(
    Guid Id, Guid BecaId, Guid PeriodoAcademicoId, string Frecuencia,
    string Destinatario, decimal MontoMaximoAprobadoPeriodo,
    decimal SumaCuotasProgramadas, int NumeroCuotas, string Estado);
