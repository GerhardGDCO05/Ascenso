using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Excepciones;
using FluentValidation;

namespace Ascenso.Postulaciones.Application.Consolidacion.Consultas.ObtenerRankingConvocatoria;

public sealed class ObtenerRankingConvocatoriaQueryValidator
    : AbstractValidator<ObtenerRankingConvocatoriaQuery>
{
    public ObtenerRankingConvocatoriaQueryValidator()
    {
        RuleFor(x => x.ConvocatoriaId)
            .NotEmpty()
            .WithErrorCode(RankingInvalidoException.CodigoError)
            .WithMessage("El identificador de la convocatoria es obligatorio.");

        RuleFor(x => x.Pagina)
            .GreaterThanOrEqualTo(1)
            .WithErrorCode(RankingInvalidoException.CodigoError)
            .WithMessage("El número de página debe ser mayor o igual a 1.");

        RuleFor(x => x.Tamano)
            .InclusiveBetween(
                ObtenerRankingConvocatoriaQuery.TamanoMinimo,
                ObtenerRankingConvocatoriaQuery.TamanoMaximo)
            .WithErrorCode(RankingInvalidoException.CodigoError)
            .WithMessage(
                $"El tamaño de página debe estar entre {ObtenerRankingConvocatoriaQuery.TamanoMinimo} y {ObtenerRankingConvocatoriaQuery.TamanoMaximo}.");

        RuleFor(x => x.Clasificacion)
            .Must(EsClasificacionValida)
            .When(x => !string.IsNullOrWhiteSpace(x.Clasificacion))
            .WithErrorCode(RankingInvalidoException.CodigoError)
            .WithMessage("La clasificación debe ser DENTRO_DE_CUPO o LISTA_DE_ESPERA.");
    }

    private static bool EsClasificacionValida(string? clasificacion)
    {
        if (string.IsNullOrWhiteSpace(clasificacion))
        {
            return true;
        }

        try
        {
            _ = CodigoEnum.DesdeCodigo<ClasificacionRanking>(clasificacion);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
