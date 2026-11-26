using Ascenso.Becas.Application.Comun;
using Ascenso.Becas.Application.Despacho;
using Ascenso.Becas.Application.Excepciones;
using Ascenso.Becas.Application.Otorgamiento.Dtos;
using Ascenso.Becas.Application.Otorgamiento.Puertos;
using Ascenso.Becas.Domain.Comun;
using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.Excepciones;
using Ascenso.Becas.Domain.Otorgamiento.Agregados;
using Ascenso.Becas.Domain.Otorgamiento.Datos;
using Ascenso.Becas.Domain.Otorgamiento.Servicios;
using Ascenso.Becas.Domain.Otorgamiento.ValueObjects;
using Ascenso.Becas.Domain.Ports;

namespace Ascenso.Becas.Application.Otorgamiento.Comandos.EmitirOfertaBeca;

/// <summary>
/// Orquesta la emisión: configuración local, adjudicabilidad, reserva,
/// generación y subida de la carta, creación del agregado, persistencia y
/// Outbox. Si falla la persistencia se compensan el archivo y el job.
/// </summary>
public sealed class EmitirOfertaBecaHandler
    : ICommandHandler<EmitirOfertaBecaCommand, OfertaEmitidaDto>
{
    private const int MaximoLongitudCodigoBeca = 20;
    private const string TipoMimePdf = "application/pdf";

    private readonly IContextoOperacionActual _contextoActual;
    private readonly IClock _reloj;
    private readonly IPostulacionesClient _postulaciones;
    private readonly IPeriodosAcademicosClient _periodos;
    private readonly IProyeccionesOtorgamiento _proyecciones;
    private readonly IMetricaConfiguracionFaltante _metricaConfiguracion;
    private readonly IMetricasOtorgamiento _metricas;
    private readonly IProgramasClient _programas;
    private readonly ICartaOfertaPdfGenerator _generadorCarta;
    private readonly IPlantillaCompromisoPdfGenerator _generadorPlantilla;
    private readonly IAlmacenamientoArchivos _almacenamiento;
    private readonly IProgramadorExpiracionOfertas _programador;
    private readonly IBecaRepository _repositorio;
    private readonly IUnitOfWork _unidadDeTrabajo;
    private readonly IOutboxBeca _outbox;

    public EmitirOfertaBecaHandler(
        IContextoOperacionActual contextoActual,
        IClock reloj,
        IPostulacionesClient postulaciones,
        IPeriodosAcademicosClient periodos,
        IProyeccionesOtorgamiento proyecciones,
        IMetricaConfiguracionFaltante metricaConfiguracion,
        IMetricasOtorgamiento metricas,
        IProgramasClient programas,
        ICartaOfertaPdfGenerator generadorCarta,
        IPlantillaCompromisoPdfGenerator generadorPlantilla,
        IAlmacenamientoArchivos almacenamiento,
        IProgramadorExpiracionOfertas programador,
        IBecaRepository repositorio,
        IUnitOfWork unidadDeTrabajo,
        IOutboxBeca outbox)
    {
        _contextoActual = contextoActual ?? throw new ArgumentNullException(nameof(contextoActual));
        _reloj = reloj ?? throw new ArgumentNullException(nameof(reloj));
        _postulaciones = postulaciones ?? throw new ArgumentNullException(nameof(postulaciones));
        _periodos = periodos ?? throw new ArgumentNullException(nameof(periodos));
        _proyecciones = proyecciones ?? throw new ArgumentNullException(nameof(proyecciones));
        _metricaConfiguracion = metricaConfiguracion
            ?? throw new ArgumentNullException(nameof(metricaConfiguracion));
        _metricas = metricas ?? throw new ArgumentNullException(nameof(metricas));
        _programas = programas ?? throw new ArgumentNullException(nameof(programas));
        _generadorCarta = generadorCarta ?? throw new ArgumentNullException(nameof(generadorCarta));
        _generadorPlantilla = generadorPlantilla
            ?? throw new ArgumentNullException(nameof(generadorPlantilla));
        _almacenamiento = almacenamiento ?? throw new ArgumentNullException(nameof(almacenamiento));
        _programador = programador ?? throw new ArgumentNullException(nameof(programador));
        _repositorio = repositorio ?? throw new ArgumentNullException(nameof(repositorio));
        _unidadDeTrabajo = unidadDeTrabajo ?? throw new ArgumentNullException(nameof(unidadDeTrabajo));
        _outbox = outbox ?? throw new ArgumentNullException(nameof(outbox));
    }

    public async Task<OfertaEmitidaDto> HandleAsync(
        EmitirOfertaBecaCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var ahora = _reloj.UtcNow;
        var fechaOperacion = DateOnly.FromDateTime(ahora.UtcDateTime);
        var adjudicacion = await ObtenerAdjudicabilidadAsync(command.PostulacionId, cancellationToken);

        if (!adjudicacion.Adjudicable)
        {
            throw new PostulacionNoAdjudicableException(
                adjudicacion.PostulacionId == Guid.Empty
                    ? command.PostulacionId
                    : adjudicacion.PostulacionId,
                adjudicacion.MotivoNoAdjudicable);
        }

        ValidarRespuestaAdjudicabilidad(adjudicacion, command.PostulacionId);
        await ValidarPeriodoActualAsync(adjudicacion, cancellationToken);

        var proyeccionLocal = await _proyecciones.ObtenerAsync(
            adjudicacion.ProgramaBecaId,
            adjudicacion.CarreraId,
            adjudicacion.PeriodoAcademicoId,
            adjudicacion.UniversidadAliadaId,
            fechaOperacion,
            cancellationToken);

        if (proyeccionLocal is null)
        {
            _metricaConfiguracion.Registrar(IMetricaConfiguracionFaltante.RecursoPrograma);
            throw new ConfiguracionProgramaNoDisponibleException(
                adjudicacion.ProgramaBecaId,
                "No existe configuración local de programa, costo de matrícula y convenio para la postulación.");
        }

        var condiciones = await ObtenerCondicionesAsync(adjudicacion, cancellationToken);

        if (condiciones is null)
        {
            _metricaConfiguracion.Registrar(IMetricaConfiguracionFaltante.RecursoPrograma);
            throw new ConfiguracionProgramaNoDisponibleException(
                adjudicacion.ProgramaBecaId,
                "MS-PRG no devolvió condiciones vigentes para el programa, la carrera y el periodo.");
        }

        var proyeccion = AplicarCondicionesActuales(
            proyeccionLocal,
            condiciones,
            adjudicacion);
        ValidarProyeccion(proyeccion, fechaOperacion);

        var montoPorPeriodo = CalculadoraMontoOferta.CalcularMontoPorPeriodo(
            proyeccion.Programa.PoliticaCobertura,
            proyeccion.CostoMatricula.MontoReferenciaMatricula);

        if (montoPorPeriodo <= 0m)
        {
            _metricaConfiguracion.Registrar(IMetricaConfiguracionFaltante.RecursoPrograma);
            throw new ConfiguracionProgramaNoDisponibleException(
                adjudicacion.ProgramaBecaId,
                "El monto calculado de la oferta debe ser mayor que cero.");
        }

        var montoTotal = CalculadoraMontoOferta.CalcularMontoTotalEstimado(
            montoPorPeriodo,
            proyeccion.Programa.DuracionPeriodos);

        var becaId = UuidV7.Nuevo(ahora);
        var ofertaId = UuidV7.Nuevo(ahora);
        var codigoBeca = CrearCodigoBeca(becaId);
        var reserva = await ReservarAsync(
            adjudicacion,
            ofertaId,
            montoTotal,
            cancellationToken);

        var fechaVencimiento = ahora.AddDays(Beca.DiasVigenciaOferta);
        Stream? carta = null;
        ArchivoAlmacenado? archivo = null;
        Stream? plantilla = null;
        ArchivoAlmacenado? archivoPlantilla = null;
        string? referenciaJob = null;

        try
        {
            carta = await GenerarCartaAsync(
                becaId,
                ofertaId,
                adjudicacion,
                proyeccion,
                montoPorPeriodo,
                fechaVencimiento,
                cancellationToken);

            archivo = await SubirPdfAsync(
                carta,
                $"becas/{becaId}/ofertas/{ofertaId}/carta-otorgamiento.pdf",
                cancellationToken);

            if (proyeccion.Programa.RequiereCompromisoFirmado)
            {
                plantilla = await GenerarPlantillaAsync(
                    becaId,
                    codigoBeca,
                    adjudicacion,
                    proyeccion,
                    montoPorPeriodo,
                    fechaOperacion,
                    cancellationToken);

                archivoPlantilla = await SubirPdfAsync(
                    plantilla,
                    $"becas/{becaId}/compromisos/plantilla-compromiso.pdf",
                    cancellationToken);
            }

            referenciaJob = await ProgramarExpiracionAsync(
                ofertaId,
                fechaVencimiento,
                cancellationToken);

            var contexto = ContextoAplicacion.Crear(_contextoActual, _reloj);
            var beca = CrearAgregado(
                becaId,
                ofertaId,
                codigoBeca,
                adjudicacion,
                proyeccion,
                reserva,
                montoPorPeriodo,
                montoTotal,
                archivo,
                archivoPlantilla?.Url,
                referenciaJob,
                contexto);

            await OtorgarPostulacionAsync(
                command.PostulacionId,
                ofertaId,
                cancellationToken);

            await _unidadDeTrabajo.EjecutarEnTransaccionAsync(
                async token =>
                {
                    await _repositorio.AgregarAsync(beca, token);
                    await EncolarEventosAsync(beca, token);
                    await _unidadDeTrabajo.GuardarCambiosAsync(token);
                },
                cancellationToken);

            _metricas.OfertaEmitida();

            return new OfertaEmitidaDto(
                ofertaId,
                becaId,
                EstadoOferta.PendienteRespuesta.ACodigo(),
                fechaVencimiento);
        }
        catch
        {
            await CompensarAsync(referenciaJob, archivo?.Url, archivoPlantilla?.Url);
            throw;
        }
        finally
        {
            if (carta is not null)
            {
                await carta.DisposeAsync();
            }

            if (plantilla is not null)
            {
                await plantilla.DisposeAsync();
            }
        }
    }

    private async Task OtorgarPostulacionAsync(
        Guid postulacionId,
        Guid ofertaId,
        CancellationToken cancellationToken)
    {
        try
        {
            await _postulaciones.OtorgarAsync(
                postulacionId,
                ofertaId,
                cancellationToken);
        }
        catch (Exception excepcion) when (
            !cancellationToken.IsCancellationRequested
            && excepcion is HttpRequestException or TimeoutException or TaskCanceledException)
        {
            throw new DependenciaNoDisponibleException("MS-POS", excepcion);
        }
    }

    private async Task<ResultadoAdjudicabilidad> ObtenerAdjudicabilidadAsync(
        Guid postulacionId,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _postulaciones.ObtenerAdjudicabilidadAsync(postulacionId, cancellationToken);
        }
        catch (Exception excepcion) when (
            !cancellationToken.IsCancellationRequested
            && excepcion is HttpRequestException or TimeoutException or TaskCanceledException)
        {
            throw new DependenciaNoDisponibleException("MS-POS", excepcion);
        }
    }

    private async Task ValidarPeriodoActualAsync(
        ResultadoAdjudicabilidad adjudicacion,
        CancellationToken cancellationToken)
    {
        ResultadoPeriodoAcademico periodoActual;

        try
        {
            periodoActual = await _periodos.ObtenerActualAsync(cancellationToken);
        }
        catch (Exception excepcion) when (
            !cancellationToken.IsCancellationRequested
            && excepcion is HttpRequestException or TimeoutException or TaskCanceledException)
        {
            throw new DependenciaNoDisponibleException("MS-PLT", excepcion);
        }

        if (periodoActual.PeriodoAcademicoId != adjudicacion.PeriodoAcademicoId
            || !string.Equals(
                periodoActual.Codigo,
                adjudicacion.PeriodoCodigo,
                StringComparison.Ordinal))
        {
            throw new DependenciaNoDisponibleException(
                "MS-PLT",
                new InvalidOperationException(
                    "El periodo actual no coincide con el periodo de la postulación adjudicable."));
        }
    }

    private async Task<ResultadoReservaPresupuesto> ReservarAsync(
        ResultadoAdjudicabilidad adjudicacion,
        Guid ofertaId,
        decimal montoTotal,
        CancellationToken cancellationToken)
    {
        var solicitud = new SolicitudReservaPresupuesto(
            "OFERTA_BECA",
            ofertaId,
            adjudicacion.PeriodoAcademicoId,
            montoTotal,
            1);

        ResultadoReservaPresupuesto resultado;

        try
        {
            resultado = await _programas.ReservarAsync(
                adjudicacion.ProgramaBecaId,
                solicitud,
                cancellationToken);
        }
        catch (Exception excepcion) when (
            !cancellationToken.IsCancellationRequested
            && excepcion is HttpRequestException or TimeoutException or TaskCanceledException)
        {
            throw new DependenciaNoDisponibleException("MS-PRG", excepcion);
        }

        if (resultado.ReservaId == Guid.Empty
            || string.IsNullOrWhiteSpace(resultado.Estado)
            || resultado.ExpiraEn == default)
        {
            throw new DependenciaNoDisponibleException(
                "MS-PRG",
                new InvalidOperationException("La reserva devuelta no contiene los datos mínimos."));
        }

        var estado = resultado.Estado.Trim().ToUpperInvariant();

        if (estado is not ("PENDIENTE" or "CONFIRMADA"))
        {
            throw ReservaPresupuestoRechazadaException.PorFaltaDeCupoOPresupuesto();
        }

        if (resultado.Cupos < 1 || resultado.Monto < montoTotal)
        {
            throw ReservaPresupuestoRechazadaException.PorFaltaDeCupoOPresupuesto();
        }

        return resultado;
    }

    private async Task<ResultadoCondicionesPrograma?> ObtenerCondicionesAsync(
        ResultadoAdjudicabilidad adjudicacion,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _programas.ObtenerCondicionesAsync(
                adjudicacion.ProgramaBecaId,
                adjudicacion.CarreraId,
                adjudicacion.PeriodoAcademicoId,
                adjudicacion.UniversidadAliadaId,
                cancellationToken);
        }
        catch (Exception excepcion) when (
            !cancellationToken.IsCancellationRequested
            && excepcion is HttpRequestException or TimeoutException or TaskCanceledException)
        {
            throw new DependenciaNoDisponibleException("MS-PRG", excepcion);
        }
    }

    private static ProyeccionOtorgamiento AplicarCondicionesActuales(
        ProyeccionOtorgamiento proyeccionLocal,
        ResultadoCondicionesPrograma condiciones,
        ResultadoAdjudicabilidad adjudicacion)
    {
        if (condiciones.ProgramaBecaId != proyeccionLocal.Programa.ProgramaBecaId
            || condiciones.CarreraId != adjudicacion.CarreraId
            || condiciones.PeriodoAcademicoId != adjudicacion.PeriodoAcademicoId
            || condiciones.UniversidadAliadaId != adjudicacion.UniversidadAliadaId
            || condiciones.CostoMatricula.CarreraId != adjudicacion.CarreraId
            || condiciones.CostoMatricula.PeriodoAcademicoId != adjudicacion.PeriodoAcademicoId
            || condiciones.Convenio.UniversidadAliadaId != adjudicacion.UniversidadAliadaId)
        {
            throw new DependenciaNoDisponibleException(
                "MS-PRG",
                new InvalidOperationException(
                    "Las condiciones devueltas no coinciden con la postulación adjudicable."));
        }

        return proyeccionLocal with
        {
            Programa = proyeccionLocal.Programa with
            {
                PoliticaCobertura = condiciones.PoliticaCobertura,
                CondicionesPermanencia = condiciones.CondicionesPermanencia
            },
            CostoMatricula = new CostoMatriculaProyeccion(
                condiciones.CostoMatricula.CostoMatriculaReferenciaId,
                condiciones.CostoMatricula.MontoReferenciaMatricula,
                condiciones.CostoMatricula.Moneda,
                condiciones.CostoMatricula.Estado,
                condiciones.CostoMatricula.FechaVigenciaDesde,
                condiciones.CostoMatricula.FechaVigenciaHasta,
                condiciones.CostoMatricula.NumeroVersion),
            Convenio = new ConvenioProyeccion(
                condiciones.Convenio.ConvenioId,
                condiciones.Convenio.Estado,
                condiciones.Convenio.FechaFin)
        };
    }

    private async Task<Stream> GenerarCartaAsync(
        Guid becaId,
        Guid ofertaId,
        ResultadoAdjudicabilidad adjudicacion,
        ProyeccionOtorgamiento proyeccion,
        decimal montoPorPeriodo,
        DateTimeOffset fechaVencimiento,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _generadorCarta.GenerarAsync(
                new DatosCartaOferta(
                    becaId,
                    ofertaId,
                    adjudicacion.NombreEstudiante.Trim(),
                    proyeccion.Programa.Nombre,
                    adjudicacion.PeriodoCodigo,
                    proyeccion.Programa.PoliticaCobertura,
                    montoPorPeriodo,
                    DateOnly.FromDateTime(fechaVencimiento.UtcDateTime)),
                cancellationToken);
        }
        catch (Exception excepcion) when (!cancellationToken.IsCancellationRequested)
        {
            throw new DependenciaNoDisponibleException("generador-carta", excepcion);
        }
    }

    private async Task<Stream> GenerarPlantillaAsync(
        Guid becaId,
        string codigoBeca,
        ResultadoAdjudicabilidad adjudicacion,
        ProyeccionOtorgamiento proyeccion,
        decimal montoPorPeriodo,
        DateOnly fechaEmision,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _generadorPlantilla.GenerarAsync(
                new DatosPlantillaCompromiso(
                    becaId,
                    codigoBeca,
                    adjudicacion.NombreEstudiante.Trim(),
                    adjudicacion.NumeroDocumentoEstudiante.Trim(),
                    proyeccion.Programa.Nombre,
                    adjudicacion.PeriodoCodigo,
                    proyeccion.Programa.PoliticaCobertura,
                    montoPorPeriodo,
                    proyeccion.Programa.DuracionPeriodos,
                    proyeccion.Programa.CondicionesPermanencia,
                    fechaEmision),
                cancellationToken);
        }
        catch (Exception excepcion) when (!cancellationToken.IsCancellationRequested)
        {
            throw new DependenciaNoDisponibleException(
                "generador-plantilla-compromiso",
                excepcion);
        }
    }

    private async Task<ArchivoAlmacenado> SubirPdfAsync(
        Stream contenido,
        string rutaLogica,
        CancellationToken cancellationToken)
    {
        ArchivoAlmacenado archivo;

        try
        {
            archivo = await _almacenamiento.SubirAsync(
                contenido,
                TipoMimePdf,
                rutaLogica,
                cancellationToken);
        }
        catch (Exception excepcion) when (!cancellationToken.IsCancellationRequested)
        {
            throw new DependenciaNoDisponibleException("almacenamiento", excepcion);
        }

        if (string.IsNullOrWhiteSpace(archivo.Url) || !EsHashSha256Valido(archivo.HashSha256))
        {
            if (!string.IsNullOrWhiteSpace(archivo.Url))
            {
                await EliminarArchivoSilenciosoAsync(archivo.Url, cancellationToken);
            }

            throw new DependenciaNoDisponibleException(
                "almacenamiento",
                new InvalidOperationException("El almacenamiento devolvió una URL o un hash inválido."));
        }

        if (archivo.TamanioBytes <= 0)
        {
            await EliminarArchivoSilenciosoAsync(archivo.Url, cancellationToken);
            throw new DependenciaNoDisponibleException(
                "almacenamiento",
                new InvalidOperationException("El almacenamiento devolvió un tamaño inválido."));
        }

        return archivo;
    }

    private async Task<string> ProgramarExpiracionAsync(
        Guid ofertaId,
        DateTimeOffset fechaVencimiento,
        CancellationToken cancellationToken)
    {
        try
        {
            var referencia = await _programador.ProgramarAsync(
                ofertaId,
                fechaVencimiento,
                cancellationToken);

            if (string.IsNullOrWhiteSpace(referencia))
            {
                throw new InvalidOperationException("El planificador devolvió una referencia vacía.");
            }

            return referencia;
        }
        catch (Exception excepcion) when (!cancellationToken.IsCancellationRequested)
        {
            throw new DependenciaNoDisponibleException("planificador-expiracion", excepcion);
        }
    }

    private static Beca CrearAgregado(
        Guid becaId,
        Guid ofertaId,
        string codigoBeca,
        ResultadoAdjudicabilidad adjudicacion,
        ProyeccionOtorgamiento proyeccion,
        ResultadoReservaPresupuesto reserva,
        decimal montoPorPeriodo,
        decimal montoTotal,
        ArchivoAlmacenado archivo,
        string? urlPlantillaCompromiso,
        string referenciaJob,
        ContextoOperacion contexto)
    {
        try
        {
            var beca = Beca.CrearOtorgada(
                new DatosCreacionBeca(
                    adjudicacion.PostulanteEstudianteId,
                    adjudicacion.NumeroDocumentoEstudiante,
                    adjudicacion.PostulacionId,
                    proyeccion.Programa.ProgramaBecaId,
                    adjudicacion.ConvocatoriaOrigenId,
                    adjudicacion.PeriodoAcademicoId,
                    proyeccion.Programa.DuracionPeriodos,
                    proyeccion.Programa.PoliticaCobertura,
                    proyeccion.CostoMatricula.CostoMatriculaReferenciaId,
                    montoPorPeriodo,
                    proyeccion.Programa.PermiteAcumulacion,
                    proyeccion.Programa.RequiereCompromisoFirmado,
                    codigoBeca,
                    urlPlantillaCompromiso),
                contexto,
                becaId);

            beca.EmitirOferta(
                new DatosOfertaBeca(
                    adjudicacion.PostulacionId,
                    reserva.ReservaId,
                    montoPorPeriodo,
                    montoTotal,
                    proyeccion.Programa.DuracionPeriodos,
                    proyeccion.Programa.CondicionesPermanencia,
                    archivo.Url,
                    archivo.HashSha256,
                    TipoMimePdf,
                    archivo.TamanioBytes,
                    referenciaJob,
                    contexto.AutorizacionOtpId),
                contexto,
                ofertaId);

            return beca;
        }
        catch (InvalidOperationException excepcion)
        {
            throw new ConflictoOperacionException(excepcion.Message);
        }
        catch (ArgumentException excepcion)
        {
            throw new SolicitudInvalidaException(excepcion.Message);
        }
    }

    private async Task EncolarEventosAsync(Beca beca, CancellationToken cancellationToken)
    {
        foreach (var evento in beca.EventosDominio)
        {
            await _outbox.EncolarAsync(evento, beca.Version, cancellationToken);
        }
    }

    private async Task CompensarAsync(
        string? referenciaJob,
        string? urlCarta,
        string? urlPlantilla)
    {
        if (!string.IsNullOrWhiteSpace(referenciaJob))
        {
            try
            {
                await _programador.CancelarAsync(referenciaJob, CancellationToken.None);
            }
            catch
            {
                // Compensación best-effort: el job es idempotente y la oferta no existe.
            }
        }

        if (!string.IsNullOrWhiteSpace(urlCarta))
        {
            await EliminarArchivoSilenciosoAsync(urlCarta, CancellationToken.None);
        }

        if (!string.IsNullOrWhiteSpace(urlPlantilla))
        {
            await EliminarArchivoSilenciosoAsync(urlPlantilla, CancellationToken.None);
        }
    }

    private async Task EliminarArchivoSilenciosoAsync(string url, CancellationToken cancellationToken)
    {
        try
        {
            await _almacenamiento.EliminarAsync(url, cancellationToken);
        }
        catch
        {
            // Compensación best-effort: se prioriza el error original.
        }
    }

    private static void ValidarRespuestaAdjudicabilidad(
        ResultadoAdjudicabilidad adjudicacion,
        Guid postulacionSolicitada)
    {
        if (adjudicacion.PostulacionId != Guid.Empty
            && adjudicacion.PostulacionId != postulacionSolicitada)
        {
            throw new DependenciaNoDisponibleException(
                "MS-POS",
                new InvalidOperationException("La postulación devuelta no coincide con la solicitada."));
        }

        if (adjudicacion.ProgramaBecaId == Guid.Empty
            || adjudicacion.PeriodoAcademicoId == Guid.Empty
            || string.IsNullOrWhiteSpace(adjudicacion.PeriodoCodigo)
            || adjudicacion.UniversidadAliadaId == Guid.Empty
            || adjudicacion.CarreraId == Guid.Empty
            || adjudicacion.PostulanteEstudianteId == Guid.Empty
            || string.IsNullOrWhiteSpace(adjudicacion.NombreEstudiante)
            || string.IsNullOrWhiteSpace(adjudicacion.NumeroDocumentoEstudiante)
            || adjudicacion.ConvocatoriaOrigenId == Guid.Empty)
        {
            throw new DependenciaNoDisponibleException(
                "MS-POS",
                new InvalidOperationException("La respuesta de adjudicabilidad está incompleta."));
        }
    }

    private void ValidarProyeccion(
        ProyeccionOtorgamiento proyeccion,
        DateOnly fechaOperacion)
    {
        var programa = proyeccion.Programa;

        if (!string.Equals(programa.Estado, "ACTIVO", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(programa.Estado, "CONFIGURADO", StringComparison.OrdinalIgnoreCase))
        {
            _metricaConfiguracion.Registrar(IMetricaConfiguracionFaltante.RecursoPrograma);
            throw new ConfiguracionProgramaNoDisponibleException(
                programa.ProgramaBecaId,
                "El programa de becas no está activo para emitir ofertas.");
        }

        if (programa.DuracionPeriodos <= 0)
        {
            _metricaConfiguracion.Registrar(IMetricaConfiguracionFaltante.RecursoPrograma);
            throw new ConfiguracionProgramaNoDisponibleException(
                programa.ProgramaBecaId,
                "La duración del programa debe ser mayor que cero.");
        }

        var costo = proyeccion.CostoMatricula;

        if (!string.Equals(costo.Estado, "VIGENTE", StringComparison.OrdinalIgnoreCase)
            || costo.FechaVigenciaDesde > fechaOperacion
            || (costo.FechaVigenciaHasta is { } hasta && hasta < fechaOperacion))
        {
            _metricaConfiguracion.Registrar(
                IMetricaConfiguracionFaltante.RecursoCostoMatricula);
            throw new ConfiguracionProgramaNoDisponibleException(
                programa.ProgramaBecaId,
                "No existe un costo de matrícula vigente para la carrera y el periodo.");
        }

        if (!string.Equals(proyeccion.Convenio.Estado, "VIGENTE", StringComparison.OrdinalIgnoreCase)
            || proyeccion.Convenio.FechaFin < fechaOperacion)
        {
            _metricaConfiguracion.Registrar(IMetricaConfiguracionFaltante.RecursoConvenio);
            throw new ConfiguracionProgramaNoDisponibleException(
                programa.ProgramaBecaId,
                "El convenio con la universidad no está vigente.");
        }
    }

    private static string CrearCodigoBeca(Guid becaId)
        => $"BECA-{becaId:N}"[..MaximoLongitudCodigoBeca];

    private static bool EsHashSha256Valido(string? hash)
    {
        if (hash is null || hash.Length != 64)
        {
            return false;
        }

        foreach (var caracter in hash)
        {
            if (!Uri.IsHexDigit(caracter))
            {
                return false;
            }
        }

        return true;
    }
}
