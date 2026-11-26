using FluentValidation;

namespace Ascenso.Becas.Application.SeguimientoAcademico.Comandos.RegistrarReporteAcademico;

public sealed class RegistrarReporteAcademicoValidator
    : AbstractValidator<RegistrarReporteAcademicoCommand>
{
    public RegistrarReporteAcademicoValidator()
    {
        RuleFor(comando => comando.BecaId).NotEmpty();
        RuleFor(comando => comando.PeriodoAcademicoId).NotEmpty();
        RuleFor(comando => comando.UniversidadAliadaId).NotEmpty();
        RuleFor(comando => comando.PromedioPeriodo)
            .InclusiveBetween(0m, 20m)
            .Must(valor => decimal.Round(valor, 2) == valor);
        RuleFor(comando => comando.MateriasAprobadas).GreaterThanOrEqualTo(0);
        RuleFor(comando => comando.MateriasReprobadas).GreaterThanOrEqualTo(0);
        RuleFor(comando => comando.MateriasCursando).GreaterThanOrEqualTo(0);
        RuleFor(comando => comando.CreditosAprobados).GreaterThanOrEqualTo(0);
        RuleFor(comando => comando.HorasServicioCumplidas)
            .GreaterThanOrEqualTo(0).When(comando => comando.HorasServicioCumplidas.HasValue);
        RuleFor(comando => comando.EstadoMatricula).IsInEnum();
        RuleFor(comando => comando.FuenteEntrega).IsInEnum();
        RuleFor(comando => comando.UrlComprobanteNotasFirebase).NotEmpty();
        RuleFor(comando => comando.HashComprobanteSha256).NotEmpty().Length(64);
        RuleFor(comando => comando.TamanioComprobanteBytes)
            .InclusiveBetween(1, 5 * 1024 * 1024);
        RuleFor(comando => comando.TipoMimeComprobante).Equal("application/pdf");
    }
}
