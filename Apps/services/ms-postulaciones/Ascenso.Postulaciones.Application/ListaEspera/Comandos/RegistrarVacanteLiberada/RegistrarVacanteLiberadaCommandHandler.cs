using Ascenso.Postulaciones.Application.Interfaces;
using Ascenso.Postulaciones.Application.ListaEspera.DTOs;
using Ascenso.Postulaciones.Domain.Comun;
using Ascenso.Postulaciones.Domain.Interfaces;
using Ascenso.Postulaciones.Domain.ListaEspera.Servicios;
using FluentValidation;

namespace Ascenso.Postulaciones.Application.ListaEspera.Comandos.RegistrarVacanteLiberada;

public sealed class RegistrarVacanteLiberadaCommandHandler
{
    private readonly IUsuarioActual _usuarioActual;
    private readonly IGestorConcurrenciaListaEspera _gestorConcurrencia;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<RegistrarVacanteLiberadaCommand> _validator;
    private readonly TimeProvider _timeProvider;

    public RegistrarVacanteLiberadaCommandHandler(
        IUsuarioActual usuarioActual,
        IGestorConcurrenciaListaEspera gestorConcurrencia,
        IUnitOfWork unitOfWork,
        IValidator<RegistrarVacanteLiberadaCommand> validator,
        TimeProvider timeProvider)
    {
        _usuarioActual = usuarioActual ?? throw new ArgumentNullException(nameof(usuarioActual));
        _gestorConcurrencia = gestorConcurrencia ?? throw new ArgumentNullException(nameof(gestorConcurrencia));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<EntradaListaEsperaDto> HandleAsync(
        RegistrarVacanteLiberadaCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        await _validator.ValidateAndThrowAsync(command, cancellationToken);

        var usuarioActorId = _usuarioActual.EstaAutenticado && _usuarioActual.UsuarioId != Guid.Empty
            ? _usuarioActual.UsuarioId
            : UsuarioSistema.Id;

        return await _gestorConcurrencia.EjecutarConBloqueoConvocatoriaAsync(
            command.ConvocatoriaId,
            async (entradasBloqueadas, ct) =>
            {
                var siguiente = ServicioPromocionListaEspera.SeleccionarSiguienteParaVacante(entradasBloqueadas);
                siguiente.MarcarElegiblePorVacante(_timeProvider.GetUtcNow(), usuarioActorId);
                await _unitOfWork.GuardarCambiosAsync(ct);
                return siguiente.ADto();
            },
            cancellationToken);
    }
}
