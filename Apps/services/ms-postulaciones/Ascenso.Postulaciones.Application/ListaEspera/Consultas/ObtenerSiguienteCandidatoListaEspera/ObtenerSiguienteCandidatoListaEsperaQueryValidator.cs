using Ascenso.Postulaciones.Domain.Excepciones;
using FluentValidation;

namespace Ascenso.Postulaciones.Application.ListaEspera.Consultas.ObtenerSiguienteCandidatoListaEspera;

public sealed class ObtenerSiguienteCandidatoListaEsperaQueryValidator
    : AbstractValidator<ObtenerSiguienteCandidatoListaEsperaQuery>
{
    public ObtenerSiguienteCandidatoListaEsperaQueryValidator()
    {
        RuleFor(x => x.ConvocatoriaId)
            .NotEmpty()
            .WithErrorCode(ListaEsperaInvalidaException.CodigoError)
            .WithMessage("El identificador de la convocatoria es obligatorio.");
    }
}
