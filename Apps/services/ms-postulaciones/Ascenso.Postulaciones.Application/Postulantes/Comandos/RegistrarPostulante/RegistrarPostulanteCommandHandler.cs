using Ascenso.Postulaciones.Application.Common;
using Ascenso.Postulaciones.Application.Interfaces;
using Ascenso.Postulaciones.Application.Postulantes.DTOs;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Excepciones;
using Ascenso.Postulaciones.Domain.Interfaces;
using Ascenso.Postulaciones.Domain.Postulantes.Agregados;
using Ascenso.Postulaciones.Domain.Postulantes.ObjetosValor;
using FluentValidation;

namespace Ascenso.Postulaciones.Application.Postulantes.Comandos.RegistrarPostulante;

public sealed class RegistrarPostulanteCommandHandler
{
    private readonly IUsuarioActual _usuarioActual;
    private readonly IPostulanteRepository _postulanteRepository;
    private readonly IPeriodoAcademicoClient _periodoAcademicoClient;
    private readonly ICatalogoAcademicoClient _catalogoAcademicoClient;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<RegistrarPostulanteCommand> _validator;
    private readonly TimeProvider _timeProvider;

    public RegistrarPostulanteCommandHandler(
        IUsuarioActual usuarioActual,
        IPostulanteRepository postulanteRepository,
        IPeriodoAcademicoClient periodoAcademicoClient,
        ICatalogoAcademicoClient catalogoAcademicoClient,
        IUnitOfWork unitOfWork,
        IValidator<RegistrarPostulanteCommand> validator,
        TimeProvider timeProvider)
    {
        _usuarioActual = usuarioActual ?? throw new ArgumentNullException(nameof(usuarioActual));
        _postulanteRepository = postulanteRepository ?? throw new ArgumentNullException(nameof(postulanteRepository));
        _periodoAcademicoClient = periodoAcademicoClient ?? throw new ArgumentNullException(nameof(periodoAcademicoClient));
        _catalogoAcademicoClient = catalogoAcademicoClient ?? throw new ArgumentNullException(nameof(catalogoAcademicoClient));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<PostulanteDto> HandleAsync(
        RegistrarPostulanteCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        await _validator.ValidateAndThrowAsync(command, cancellationToken);

        var usuarioId = _usuarioActual.ObtenerUsuarioIdRequerido();

        var postulanteExistente = await _postulanteRepository
            .ObtenerPorUsuarioIdAsync(usuarioId, cancellationToken);

        if (postulanteExistente is not null)
        {
            throw new PerfilYaRegistradoException();
        }

        var numeroDocumentoIdentidad = command.NumeroDocumentoIdentidad.Trim();
        if (await _postulanteRepository.ExistePorNumeroDocumentoAsync(
                numeroDocumentoIdentidad,
                cancellationToken))
        {
            throw new IdentidadDuplicadaException();
        }

        var periodoActual = await _periodoAcademicoClient
            .ObtenerPeriodoActualAsync(cancellationToken);

        if (periodoActual.Id != command.PeriodoAcademicoId
            || periodoActual.Estado != EstadoPeriodoAcademico.Vigente)
        {
            throw new PeriodoAcademicoInvalidoException();
        }

        var universidad = await _catalogoAcademicoClient
            .ObtenerUniversidadAsync(command.UniversidadAliadaId, cancellationToken);

        if (universidad.Id != command.UniversidadAliadaId
            || universidad.Estado != EstadoUniversidadAliada.Activa
            || !universidad.TieneConvenioVigente)
        {
            throw new UniversidadNoElegibleException();
        }

        var carrera = universidad.Carreras
            .FirstOrDefault(carreraCatalogo => carreraCatalogo.Id == command.CarreraId);

        if (carrera is null || !carrera.Activa)
        {
            throw new CarreraNoElegibleException();
        }

        if (!PostulanteMapeador.IntentarConvertirCondicionAcademica(
                command.CondicionAcademica,
                out var condicionAcademica))
        {
            throw new CondicionAcademicaInvalidaException();
        }

        var datosContacto = new DatosContacto(
            command.DatosContacto.CorreoElectronico,
            command.DatosContacto.Telefono);

        var postulante = Postulante.Crear(
            personaId: null,
            usuarioId,
            numeroDocumentoIdentidad,
            command.Nombres,
            command.Apellidos,
            datosContacto,
            command.UniversidadAliadaId,
            command.CarreraId,
            command.PeriodoAcademicoId,
            condicionAcademica,
            command.AceptaTratamientoDatos,
            _timeProvider.GetUtcNow());

        await _postulanteRepository.AgregarAsync(postulante, cancellationToken);
        await _unitOfWork.GuardarCambiosAsync(cancellationToken);

        return postulante.ADto();
    }
}
