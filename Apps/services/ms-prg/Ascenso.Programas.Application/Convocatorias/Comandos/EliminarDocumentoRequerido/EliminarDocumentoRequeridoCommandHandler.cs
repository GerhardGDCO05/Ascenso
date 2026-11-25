using Ascenso.Programas.Application.Convocatorias.DTOs;
using Ascenso.Programas.Domain.Enums;
using Ascenso.Programas.Domain.Excepciones;
using Ascenso.Programas.Domain.Interfaces;

namespace Ascenso.Programas.Application.Convocatorias.Comandos.EliminarDocumentoRequerido;

public sealed class EliminarDocumentoRequeridoCommandHandler
{
    private readonly IConvocatoriaRepository _convocatoriaRepository;
    private readonly IUnitOfWork _unitOfWork;

    public EliminarDocumentoRequeridoCommandHandler(
        IConvocatoriaRepository convocatoriaRepository,
        IUnitOfWork unitOfWork)
    {
        _convocatoriaRepository = convocatoriaRepository ?? throw new ArgumentNullException(nameof(convocatoriaRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<ConvocatoriaDetalleDto> HandleAsync(
        EliminarDocumentoRequeridoCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.ConvocatoriaId == Guid.Empty)
        {
            throw new ArgumentException("El identificador de la convocatoria es obligatorio.", nameof(command));
        }

        if (command.DocumentoId == Guid.Empty)
        {
            throw new ArgumentException("El identificador del documento requerido es obligatorio.", nameof(command));
        }

        var convocatoria = await _convocatoriaRepository.ObtenerPorIdAsync(command.ConvocatoriaId, cancellationToken)
            ?? throw new ConvocatoriaNoEncontradaException(command.ConvocatoriaId);

        if (convocatoria.Estado != EstadoConvocatoria.Borrador)
        {
            throw new ConvocatoriaNoEditableException(convocatoria.Id, convocatoria.Estado);
        }

        convocatoria.QuitarDocumento(command.DocumentoId, command.UsuarioModificacionId);

        _convocatoriaRepository.Actualizar(convocatoria);
        await _unitOfWork.GuardarCambiosAsync(cancellationToken);

        return ConvocatoriaMapeador.ADetalleDto(convocatoria);
    }
}
