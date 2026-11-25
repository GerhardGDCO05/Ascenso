using Ascenso.Programas.Application.Convocatorias.DTOs;
using Ascenso.Programas.Domain.Enums;
using Ascenso.Programas.Domain.Excepciones;
using Ascenso.Programas.Domain.Interfaces;
using FluentValidation;

namespace Ascenso.Programas.Application.Convocatorias.Comandos.AgregarDocumentoRequerido;

public sealed class AgregarDocumentoRequeridoCommandHandler
{
    private readonly IConvocatoriaRepository _convocatoriaRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<AgregarDocumentoRequeridoCommand> _validator;

    public AgregarDocumentoRequeridoCommandHandler(
        IConvocatoriaRepository convocatoriaRepository,
        IUnitOfWork unitOfWork,
        IValidator<AgregarDocumentoRequeridoCommand> validator)
    {
        _convocatoriaRepository = convocatoriaRepository ?? throw new ArgumentNullException(nameof(convocatoriaRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    }

    public async Task<DocumentoRequeridoDto> HandleAsync(
        AgregarDocumentoRequeridoCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        await _validator.ValidateAndThrowAsync(command, cancellationToken);

        var convocatoria = await _convocatoriaRepository.ObtenerPorIdAsync(command.ConvocatoriaId, cancellationToken)
            ?? throw new ConvocatoriaNoEncontradaException(command.ConvocatoriaId);

        if (convocatoria.Estado != EstadoConvocatoria.Borrador)
        {
            throw new ConvocatoriaNoEditableException(convocatoria.Id, convocatoria.Estado);
        }

        var documento = convocatoria.AgregarDocumento(
            command.TipoDocumento,
            command.Nombre,
            command.Descripcion,
            command.Obligatorio,
            command.TiposMimePermitidos,
            command.TamanioMaximoMb,
            command.UsuarioModificacionId);

        _convocatoriaRepository.Actualizar(convocatoria);
        await _unitOfWork.GuardarCambiosAsync(cancellationToken);

        return ConvocatoriaMapeador.ADto(documento);
    }
}
