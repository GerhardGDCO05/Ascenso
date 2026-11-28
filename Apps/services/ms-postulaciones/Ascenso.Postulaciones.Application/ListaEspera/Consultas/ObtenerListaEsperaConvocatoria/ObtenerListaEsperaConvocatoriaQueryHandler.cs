using Ascenso.Postulaciones.Application.ListaEspera.DTOs;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Interfaces;
using FluentValidation;

namespace Ascenso.Postulaciones.Application.ListaEspera.Consultas.ObtenerListaEsperaConvocatoria;

public sealed class ObtenerListaEsperaConvocatoriaQueryHandler
{
    private readonly IEntradaListaEsperaRepository _entradaListaEsperaRepository;
    private readonly IValidator<ObtenerListaEsperaConvocatoriaQuery> _validator;

    public ObtenerListaEsperaConvocatoriaQueryHandler(
        IEntradaListaEsperaRepository entradaListaEsperaRepository,
        IValidator<ObtenerListaEsperaConvocatoriaQuery> validator)
    {
        _entradaListaEsperaRepository = entradaListaEsperaRepository
            ?? throw new ArgumentNullException(nameof(entradaListaEsperaRepository));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    }

    public async Task<ListaEsperaConvocatoriaDto> HandleAsync(
        ObtenerListaEsperaConvocatoriaQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        await _validator.ValidateAndThrowAsync(query, cancellationToken);

        EstadoListaEspera? filtroEstado = string.IsNullOrWhiteSpace(query.Estado)
            ? null
            : CodigoEnum.DesdeCodigo<EstadoListaEspera>(query.Estado);

        var entradas = await _entradaListaEsperaRepository.ListarPorConvocatoriaAsync(
            query.ConvocatoriaId,
            estado: null,
            cancellationToken);

        return ListaEsperaMapeador.AListaEsperaConvocatoriaDto(
            query.ConvocatoriaId,
            entradas,
            filtroEstado,
            query.Pagina,
            query.Tamano);
    }
}
