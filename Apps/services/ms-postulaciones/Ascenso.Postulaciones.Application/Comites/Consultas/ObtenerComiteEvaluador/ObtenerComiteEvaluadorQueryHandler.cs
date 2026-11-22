using Ascenso.Postulaciones.Application.Comites.DTOs;
using Ascenso.Postulaciones.Application.Common;
using Ascenso.Postulaciones.Application.Interfaces;
using Ascenso.Postulaciones.Domain.Excepciones;
using Ascenso.Postulaciones.Domain.Interfaces;

namespace Ascenso.Postulaciones.Application.Comites.Consultas.ObtenerComiteEvaluador;

public sealed class ObtenerComiteEvaluadorQueryHandler
{
    private readonly IUsuarioActual _usuarioActual;
    private readonly IComiteEvaluadorRepository _comiteRepository;

    public ObtenerComiteEvaluadorQueryHandler(
        IUsuarioActual usuarioActual,
        IComiteEvaluadorRepository comiteRepository)
    {
        _usuarioActual = usuarioActual ?? throw new ArgumentNullException(nameof(usuarioActual));
        _comiteRepository = comiteRepository ?? throw new ArgumentNullException(nameof(comiteRepository));
    }

    public async Task<ComiteEvaluadorDto> HandleAsync(
        ObtenerComiteEvaluadorQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        _ = _usuarioActual.ObtenerUsuarioIdRequerido();

        var comite = await _comiteRepository
            .ObtenerPorIdAsync(query.ComiteId, cancellationToken)
            ?? throw new ComiteNoEncontradoException();

        return comite.ADto();
    }
}
