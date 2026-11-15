using Ascenso.Postulaciones.Application.Common;
using Ascenso.Postulaciones.Application.Interfaces;
using Ascenso.Postulaciones.Application.Postulantes.DTOs;
using Ascenso.Postulaciones.Domain.Excepciones;
using Ascenso.Postulaciones.Domain.Interfaces;

namespace Ascenso.Postulaciones.Application.Postulantes.Consultas.ObtenerMiPerfil;

public sealed class ObtenerMiPerfilQueryHandler
{
    private readonly IUsuarioActual _usuarioActual;
    private readonly IPostulanteRepository _postulanteRepository;

    public ObtenerMiPerfilQueryHandler(
        IUsuarioActual usuarioActual,
        IPostulanteRepository postulanteRepository)
    {
        _usuarioActual = usuarioActual ?? throw new ArgumentNullException(nameof(usuarioActual));
        _postulanteRepository = postulanteRepository ?? throw new ArgumentNullException(nameof(postulanteRepository));
    }

    public async Task<PostulanteDto> HandleAsync(
        ObtenerMiPerfilQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var usuarioId = _usuarioActual.ObtenerUsuarioIdRequerido();

        var postulante = await _postulanteRepository
            .ObtenerPorUsuarioIdAsync(usuarioId, cancellationToken)
            ?? throw new PostulanteNoEncontradoException();

        return postulante.ADto();
    }
}
