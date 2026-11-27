using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Excepciones;
using FluentValidation;

namespace Ascenso.Postulaciones.Application.ListaEspera.Consultas.ObtenerListaEsperaConvocatoria;

public sealed class ObtenerListaEsperaConvocatoriaQueryValidator
    : AbstractValidator<ObtenerListaEsperaConvocatoriaQuery>
{
    public ObtenerListaEsperaConvocatoriaQueryValidator()
    {
        RuleFor(x => x.ConvocatoriaId)
            .NotEmpty()
            .WithErrorCode(ListaEsperaInvalidaException.CodigoError)
            .WithMessage("El identificador de la convocatoria es obligatorio.");

        RuleFor(x => x.Pagina)
            .GreaterThanOrEqualTo(1)
            .WithErrorCode(ListaEsperaInvalidaException.CodigoError)
            .WithMessage("El número de página debe ser mayor o igual a 1.");

        RuleFor(x => x.Tamano)
            .InclusiveBetween(
                ObtenerListaEsperaConvocatoriaQuery.TamanoMinimo,
                ObtenerListaEsperaConvocatoriaQuery.TamanoMaximo)
            .WithErrorCode(ListaEsperaInvalidaException.CodigoError)
            .WithMessage(
                $"El tamaño de página debe estar entre {ObtenerListaEsperaConvocatoriaQuery.TamanoMinimo} y {ObtenerListaEsperaConvocatoriaQuery.TamanoMaximo}.");

        RuleFor(x => x.Estado)
            .Must(EsEstadoValido)
            .When(x => !string.IsNullOrWhiteSpace(x.Estado))
            .WithErrorCode(ListaEsperaInvalidaException.CodigoError)
            .WithMessage("El estado debe ser EN_ESPERA, ELEGIBLE_POR_VACANTE o PROMOVIDA.");
    }

    private static bool EsEstadoValido(string? estado)
    {
        if (string.IsNullOrWhiteSpace(estado))
        {
            return true;
        }

        try
        {
            _ = CodigoEnum.DesdeCodigo<EstadoListaEspera>(estado);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
