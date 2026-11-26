using FluentValidation;

namespace Ascenso.Postulaciones.Application.Consolidacion.Consultas.ListarResultadosConsolidadosConvocatoria;

public sealed class ListarResultadosConsolidadosConvocatoriaQueryValidator
    : AbstractValidator<ListarResultadosConsolidadosConvocatoriaQuery>
{
    public ListarResultadosConsolidadosConvocatoriaQueryValidator()
    {
        RuleFor(x => x.ConvocatoriaId)
            .NotEmpty()
            .WithMessage("El identificador de la convocatoria es obligatorio.");

        RuleFor(x => x.Pagina)
            .GreaterThanOrEqualTo(1)
            .WithMessage("El número de página debe ser mayor o igual a 1.");

        RuleFor(x => x.Tamano)
            .InclusiveBetween(
                ListarResultadosConsolidadosConvocatoriaQuery.TamanoMinimo,
                ListarResultadosConsolidadosConvocatoriaQuery.TamanoMaximo)
            .WithMessage(
                $"El tamaño de página debe estar entre {ListarResultadosConsolidadosConvocatoriaQuery.TamanoMinimo} y {ListarResultadosConsolidadosConvocatoriaQuery.TamanoMaximo}.");
    }
}
