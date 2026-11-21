using Ascenso.Postulaciones.Application.Common;
using Ascenso.Postulaciones.Application.Interfaces;
using Ascenso.Postulaciones.Application.Rubricas.DTOs;
using Ascenso.Postulaciones.Domain.Excepciones;
using Ascenso.Postulaciones.Domain.Interfaces;

namespace Ascenso.Postulaciones.Application.Rubricas.Consultas.ObtenerRubrica;

public sealed class ObtenerRubricaQueryHandler
{
    private readonly IUsuarioActual _usuarioActual;
    private readonly IRubricaRepository _rubricaRepository;

    public ObtenerRubricaQueryHandler(
        IUsuarioActual usuarioActual,
        IRubricaRepository rubricaRepository)
    {
        _usuarioActual = usuarioActual ?? throw new ArgumentNullException(nameof(usuarioActual));
        _rubricaRepository = rubricaRepository ?? throw new ArgumentNullException(nameof(rubricaRepository));
    }

    public async Task<RubricaDto> HandleAsync(
        ObtenerRubricaQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        _ = _usuarioActual.ObtenerUsuarioIdRequerido();

        var rubrica = await _rubricaRepository
            .ObtenerPorIdAsync(query.RubricaId, cancellationToken)
            ?? throw new RubricaNoEncontradaException();

        return rubrica.ADto();
    }
}
