using System.Security.Cryptography;
using Ascenso.Becas.Application.Comun;
using Ascenso.Becas.Application.Despacho;
using Ascenso.Becas.Application.Excepciones;
using Ascenso.Becas.Application.Otorgamiento.Dtos;
using Ascenso.Becas.Application.Otorgamiento.Puertos;
using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.Otorgamiento.Agregados;
using Ascenso.Becas.Domain.Ports;

namespace Ascenso.Becas.Application.Otorgamiento.Comandos.CargarCompromisoBecario;

/// <summary>
/// Valida el PDF, calcula su hash, lo sube antes de confirmar y registra el
/// intento de carga. Si la persistencia falla se elimina el archivo.
/// </summary>
public sealed class CargarCompromisoBecarioHandler
    : ICommandHandler<CargarCompromisoBecarioCommand, CompromisoCargadoDto>
{
    private const string TipoMimePdf = "application/pdf";

    private readonly IContextoOperacionActual _contextoActual;
    private readonly IClock _reloj;
    private readonly IBecaRepository _repositorio;
    private readonly IAlmacenamientoArchivos _almacenamiento;
    private readonly IUnitOfWork _unidadDeTrabajo;
    private readonly IOutboxBeca _outbox;

    public CargarCompromisoBecarioHandler(
        IContextoOperacionActual contextoActual,
        IClock reloj,
        IBecaRepository repositorio,
        IAlmacenamientoArchivos almacenamiento,
        IUnitOfWork unidadDeTrabajo,
        IOutboxBeca outbox)
    {
        _contextoActual = contextoActual ?? throw new ArgumentNullException(nameof(contextoActual));
        _reloj = reloj ?? throw new ArgumentNullException(nameof(reloj));
        _repositorio = repositorio ?? throw new ArgumentNullException(nameof(repositorio));
        _almacenamiento = almacenamiento ?? throw new ArgumentNullException(nameof(almacenamiento));
        _unidadDeTrabajo = unidadDeTrabajo ?? throw new ArgumentNullException(nameof(unidadDeTrabajo));
        _outbox = outbox ?? throw new ArgumentNullException(nameof(outbox));
    }

    public async Task<CompromisoCargadoDto> HandleAsync(
        CargarCompromisoBecarioCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        ValidarArchivoPdf(command);
        var ip = ObtenerDireccionIp();
        var hash = Convert.ToHexString(SHA256.HashData(command.Contenido))
            .ToLowerInvariant();

        var becaInicial = await _repositorio.ObtenerPorIdAsync(command.BecaId, cancellationToken)
            ?? throw new RecursoNoEncontradoException("beca", command.BecaId);

        var compromiso = becaInicial.Compromiso;
        if (compromiso is null)
        {
            throw new ConflictoOperacionException(
                "La beca no exige compromiso firmado o no tiene un compromiso inicializado.");
        }

        var siguienteIntento = compromiso.NumeroIntentoCarga + 1;
        var ruta = $"becas/{command.BecaId}/compromisos/{compromiso.Id}/intento-{siguienteIntento}.pdf";

        ArchivoAlmacenado archivo;

        try
        {
            await using var contenido = new MemoryStream(command.Contenido, writable: false);
            archivo = await _almacenamiento.SubirAsync(
                contenido,
                TipoMimePdf,
                ruta,
                cancellationToken);
        }
        catch (Exception excepcion) when (!cancellationToken.IsCancellationRequested)
        {
            throw new DependenciaNoDisponibleException("almacenamiento", excepcion);
        }

        if (string.IsNullOrWhiteSpace(archivo.Url))
        {
            throw new DependenciaNoDisponibleException(
                "almacenamiento",
                new InvalidOperationException("El almacenamiento devolvió una URL vacía."));
        }

        if (!string.IsNullOrWhiteSpace(archivo.HashSha256)
            && !string.Equals(archivo.HashSha256, hash, StringComparison.OrdinalIgnoreCase))
        {
            await EliminarArchivoSilenciosoAsync(archivo.Url);
            throw new DependenciaNoDisponibleException(
                "almacenamiento",
                new InvalidOperationException("El hash devuelto no coincide con el archivo cargado."));
        }

        try
        {
            CompromisoCargadoDto? resultado = null;

            await _unidadDeTrabajo.EjecutarEnTransaccionAsync(
                async token =>
                {
                    var beca = await _repositorio.ObtenerBecaParaActualizarAsync(command.BecaId, token)
                        ?? throw new RecursoNoEncontradoException("beca", command.BecaId);

                    var contexto = ContextoAplicacion.Crear(_contextoActual, _reloj);

                    try
                    {
                        beca.CargarCompromiso(archivo.Url, hash, ip, contexto);
                    }
                    catch (ArgumentException excepcion)
                    {
                        throw new ArchivoCompromisoInvalidoException(excepcion.Message);
                    }
                    catch (InvalidOperationException excepcion)
                    {
                        throw new ConflictoOperacionException(excepcion.Message);
                    }

                    await EncolarEventosAsync(beca, token);
                    await _unidadDeTrabajo.GuardarCambiosAsync(token);

                    resultado = new CompromisoCargadoDto(
                        beca.Compromiso!.Id,
                        beca.Compromiso.NumeroIntentoCarga,
                        beca.Compromiso.Estado.ACodigo());
                },
                cancellationToken);

            return resultado
                ?? throw new InvalidOperationException("La carga del compromiso no produjo un resultado.");
        }
        catch
        {
            await EliminarArchivoSilenciosoAsync(archivo.Url);
            throw;
        }
    }

    private static void ValidarArchivoPdf(CargarCompromisoBecarioCommand command)
    {
        if (command.Contenido is null || command.Contenido.Length == 0)
        {
            throw new ArchivoCompromisoInvalidoException("El archivo del compromiso no puede estar vacío.");
        }

        if (command.Contenido.Length > CargarCompromisoBecarioValidator.TamanioMaximoBytes)
        {
            throw new ArchivoCompromisoInvalidoException("El archivo del compromiso no puede superar 5 MB.");
        }

        if (!string.Equals(command.TipoMime, TipoMimePdf, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(
                Path.GetExtension(command.NombreArchivo),
                ".pdf",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new ArchivoCompromisoInvalidoException("El archivo del compromiso debe ser un PDF.");
        }

        ReadOnlySpan<byte> firma = "%PDF-"u8;
        if (!command.Contenido.AsSpan().StartsWith(firma))
        {
            throw new ArchivoCompromisoInvalidoException(
                "El contenido del archivo no corresponde a un PDF válido.");
        }
    }

    private string ObtenerDireccionIp()
    {
        var direccion = _contextoActual.DireccionIp;

        if (string.IsNullOrWhiteSpace(direccion)
            || !System.Net.IPAddress.TryParse(direccion, out _))
        {
            throw new SolicitudInvalidaException(
                "No fue posible determinar una dirección IP válida para la carga del compromiso.");
        }

        return direccion.Trim();
    }

    private async Task EncolarEventosAsync(Beca beca, CancellationToken cancellationToken)
    {
        foreach (var evento in beca.EventosDominio)
        {
            await _outbox.EncolarAsync(evento, cancellationToken);
        }
    }

    private async Task EliminarArchivoSilenciosoAsync(string url)
    {
        try
        {
            await _almacenamiento.EliminarAsync(url, CancellationToken.None);
        }
        catch
        {
            // Compensación best-effort: se prioriza el error original.
        }
    }
}
