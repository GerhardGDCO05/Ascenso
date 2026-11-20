using Ascenso.Postulaciones.Application.Common;
using Ascenso.Postulaciones.Application.Interfaces;
using Ascenso.Postulaciones.Application.Postulantes.DTOs;
using Ascenso.Postulaciones.Domain.Excepciones;
using Ascenso.Postulaciones.Domain.Interfaces;
using Ascenso.Postulaciones.Domain.Postulantes.ObjetosValor;
using FluentValidation;

namespace Ascenso.Postulaciones.Application.Postulantes.Comandos.ActualizarPerfilSocioeconomico;

public sealed class ActualizarPerfilSocioeconomicoCommandHandler
{
    private readonly IUsuarioActual _usuarioActual;
    private readonly IPostulanteRepository _postulanteRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<ActualizarPerfilSocioeconomicoCommand> _validator;
    private readonly TimeProvider _timeProvider;

    public ActualizarPerfilSocioeconomicoCommandHandler(
        IUsuarioActual usuarioActual,
        IPostulanteRepository postulanteRepository,
        IUnitOfWork unitOfWork,
        IValidator<ActualizarPerfilSocioeconomicoCommand> validator,
        TimeProvider timeProvider)
    {
        _usuarioActual = usuarioActual ?? throw new ArgumentNullException(nameof(usuarioActual));
        _postulanteRepository = postulanteRepository ?? throw new ArgumentNullException(nameof(postulanteRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<PostulanteDto> HandleAsync(
        ActualizarPerfilSocioeconomicoCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        await _validator.ValidateAndThrowAsync(command, cancellationToken);

        var usuarioId = _usuarioActual.ObtenerUsuarioIdRequerido();

        var postulante = await _postulanteRepository
            .ObtenerParaActualizarPorUsuarioIdAsync(usuarioId, cancellationToken)
            ?? throw new PostulanteNoEncontradoException();

        if (!PostulanteMapeador.IntentarConvertirEstadoCivil(command.EstadoCivil, out var estadoCivil))
        {
            throw new CatalogoInvalidoException(nameof(command.EstadoCivil));
        }

        if (!PostulanteMapeador.IntentarConvertirOcupacion(command.Ocupacion, out var ocupacion))
        {
            throw new CatalogoInvalidoException(nameof(command.Ocupacion));
        }

        var miembrosFamiliares = ConstruirMiembrosFamiliares(command.MiembrosFamiliares);

        postulante.RegistrarPerfilSocioeconomico(
            estadoCivil,
            command.PromedioActual!.Value,
            command.Trabaja!.Value,
            ocupacion,
            command.IngresoMensualPostulante!.Value,
            miembrosFamiliares,
            usuarioId,
            _timeProvider.GetUtcNow());

        await _unitOfWork.GuardarCambiosAsync(cancellationToken);

        return postulante.ADto();
    }

    private static List<DatosMiembroFamiliar> ConstruirMiembrosFamiliares(
        IReadOnlyList<DatosMiembroFamiliarCommand> miembrosFamiliares)
    {
        var resultado = new List<DatosMiembroFamiliar>(miembrosFamiliares.Count);

        foreach (var miembro in miembrosFamiliares)
        {
            if (!PostulanteMapeador.IntentarConvertirParentesco(miembro.Parentesco, out var parentesco))
            {
                throw new CatalogoInvalidoException(nameof(miembro.Parentesco));
            }

            if (!PostulanteMapeador.IntentarConvertirOcupacion(miembro.Ocupacion, out var ocupacion))
            {
                throw new CatalogoInvalidoException(nameof(miembro.Ocupacion));
            }

            resultado.Add(
                new DatosMiembroFamiliar(
                    parentesco,
                    miembro.Edad!.Value,
                    ocupacion,
                    miembro.IngresoMensual!.Value));
        }

        return resultado;
    }
}
