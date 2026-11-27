using Ascenso.Becas.Domain.Enums;
using FluentValidation;

namespace Ascenso.Becas.Application.Desembolsos;

public sealed class ProgramarCalendarioDesembolsoValidator : AbstractValidator<ProgramarCalendarioDesembolsoCommand>
{
    public ProgramarCalendarioDesembolsoValidator()
    {
        RuleFor(item => item.BecaId).NotEmpty();
        RuleFor(item => item.PeriodoAcademicoId).NotEmpty();
        RuleFor(item => item.Frecuencia).IsInEnum();
        RuleFor(item => item.Destinatario).IsInEnum();
        RuleFor(item => item.MontoMaximoAprobadoPeriodo).GreaterThanOrEqualTo(0);
        RuleFor(item => item.Cuotas).NotEmpty();
        RuleFor(item => item.FechaFin).GreaterThanOrEqualTo(item => item.FechaInicio);
    }
}

public sealed class ActualizarDatosPagoBecarioValidator : AbstractValidator<ActualizarDatosPagoBecarioCommand>
{
    public ActualizarDatosPagoBecarioValidator()
    {
        RuleFor(item => item.BecaId).NotEmpty();
        RuleFor(item => item.NombreBanco).NotEmpty();
        RuleFor(item => item.CodigoBanco).NotEmpty();
        RuleFor(item => item.TipoCuenta).IsInEnum();
        RuleFor(item => item.NumeroCuenta).NotEmpty();
        RuleFor(item => item.DocumentoTitular).NotEmpty();
        RuleFor(item => item.CodigoOtp).NotEmpty();
    }
}

public sealed class RegistrarEjecucionDesembolsoValidator : AbstractValidator<RegistrarEjecucionDesembolsoCommand>
{
    public RegistrarEjecucionDesembolsoValidator()
    {
        RuleFor(item => item.CalendarioId).NotEmpty();
        RuleFor(item => item.NumeroCuota).GreaterThan(0);
        RuleFor(item => item.Monto).GreaterThan(0);
        RuleFor(item => item.FechaEjecucion).NotEmpty();
        RuleFor(item => item.Destinatario).IsInEnum();
        RuleFor(item => item.MetodoPago).IsInEnum();
        RuleFor(item => item.CuentaDestinoCifrada).NotEmpty()
            .When(item => item.Destinatario == DestinatarioPago.Institucion);
        RuleFor(item => item.ReferenciaBancaria).NotEmpty();
    }
}

public sealed class ConfirmarDesembolsoValidator : AbstractValidator<ConfirmarDesembolsoCommand>
{
    public ConfirmarDesembolsoValidator()
    {
        RuleFor(item => item.CalendarioId).NotEmpty();
        RuleFor(item => item.NumeroCuota).GreaterThan(0);
        RuleFor(item => item.CodigoOtp).NotEmpty();
    }
}

public sealed class RegistrarIncidenciaDesembolsoValidator : AbstractValidator<RegistrarIncidenciaDesembolsoCommand>
{
    public RegistrarIncidenciaDesembolsoValidator()
    {
        RuleFor(item => item.CalendarioId).NotEmpty();
        RuleFor(item => item.NumeroCuota).GreaterThan(0);
        RuleFor(item => item.Estado).Must(estado => estado is EstadoDesembolso.Fallido or EstadoDesembolso.Devuelto);
        RuleFor(item => item.Motivo).NotEmpty();
        RuleFor(item => item.ObservacionBanco).NotEmpty();
    }
}

public sealed class ResolverIncidenciaDesembolsoValidator : AbstractValidator<ResolverIncidenciaDesembolsoCommand>
{
    public ResolverIncidenciaDesembolsoValidator()
    {
        RuleFor(item => item.CalendarioId).NotEmpty();
        RuleFor(item => item.NumeroCuota).GreaterThan(0);
        RuleFor(item => item.IncidenciaId).NotEmpty();
    }
}

public sealed class ReintentarDesembolsoValidator : AbstractValidator<ReintentarDesembolsoCommand>
{
    public ReintentarDesembolsoValidator()
    {
        RuleFor(item => item.CalendarioId).NotEmpty();
        RuleFor(item => item.NumeroCuota).GreaterThan(0);
    }
}

public sealed class ObtenerCalendarioDesembolsosValidator : AbstractValidator<ObtenerCalendarioDesembolsosQuery>
{
    public ObtenerCalendarioDesembolsosValidator()
    {
        RuleFor(item => item.BecaId).NotEmpty();
        RuleFor(item => item.Pagina).GreaterThan(0);
        RuleFor(item => item.TamanoPagina).InclusiveBetween(1, 100);
    }
}

public sealed class ObtenerDesembolsosValidator : AbstractValidator<ObtenerDesembolsosQuery>
{
    public ObtenerDesembolsosValidator()
    {
        RuleFor(item => item.CalendarioId).NotEmpty();
        RuleFor(item => item.Pagina).GreaterThan(0);
        RuleFor(item => item.TamanoPagina).InclusiveBetween(1, 100);
    }
}
