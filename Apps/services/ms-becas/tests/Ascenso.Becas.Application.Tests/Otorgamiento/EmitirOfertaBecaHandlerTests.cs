using System.Security.Cryptography;
using System.Text;
using Ascenso.Becas.Application.Excepciones;
using Ascenso.Becas.Application.Otorgamiento.Comandos.EmitirOfertaBeca;
using Ascenso.Becas.Application.Otorgamiento.Servicios;
using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.Excepciones;
using Ascenso.Becas.Domain.Otorgamiento.Eventos;
using Ascenso.Becas.Domain.Ports;
using FluentAssertions;

namespace Ascenso.Becas.Application.Tests.Otorgamiento;

public sealed class EmitirOfertaBecaHandlerTests
{
    [Fact]
    public async Task Debe_EmitirLaOferta_Cuando_LaPostulacionEsAdjudicable()
    {
        var escenario = new EscenarioOtorgamiento();

        var resultado = await escenario.CrearEmitir().HandleAsync(
            new EmitirOfertaBecaCommand(escenario.PostulacionId));

        resultado.OfertaId.Should().NotBeEmpty();
        resultado.BecaId.Should().NotBeEmpty();
        resultado.Estado.Should().Be("PENDIENTE_RESPUESTA");
        resultado.FechaVencimientoAceptacion.Should().Be(
            DatosAplicacion.FechaBase.AddDays(15));

        var beca = escenario.Repositorio.Becas.Should().ContainSingle().Subject;
        beca.Estado.Should().Be(EstadoBeca.Otorgada);
        beca.CadenaBecaId.Should().Be(beca.Id);
        beca.BecaAnteriorId.Should().BeNull();
        beca.PeriodosCursados.Should().Be(0);
        beca.PeriodoVigenteActualId.Should().Be(escenario.PeriodoAcademicoId);
        beca.Oferta.Should().NotBeNull();
        beca.Oferta!.Estado.Should().Be(EstadoOferta.PendienteRespuesta);
        beca.Oferta.Id.Should().Be(resultado.OfertaId);
        beca.Oferta.UrlCartaPdfFirebase.Should().Be(escenario.Almacenamiento.Resultado.Url);
        var contenidoCarta = Encoding.UTF8.GetBytes(escenario.Generador.Contenido);
        beca.Oferta.HashCartaSha256.Should().Be(
            Convert.ToHexString(SHA256.HashData(contenidoCarta)).ToLowerInvariant());
        beca.Oferta.TipoMimeCartaPdf.Should().Be("application/pdf");
        beca.Oferta.TamanioCartaPdfBytes.Should().Be(contenidoCarta.Length);

        escenario.Generador.Datos.Should().NotBeNull();
        escenario.Generador.Datos!.NombreEstudiante.Should().Be("Ana Pérez");
        escenario.Generador.Datos.NombrePrograma.Should().Be("Programa de becas de prueba");
        escenario.Generador.Datos.MontoOfertaPorPeriodo.Should().Be(500m);

        escenario.Almacenamiento.RutasSubidas.Should().ContainSingle()
            .Which.Should().Be(
                $"becas/{beca.Id}/ofertas/{resultado.OfertaId}/carta-otorgamiento.pdf");
        escenario.Programador.Programadas.Should().ContainSingle()
            .Which.OfertaId.Should().Be(resultado.OfertaId);
        escenario.Programador.Programadas[0].FechaVencimiento.Should().Be(
            resultado.FechaVencimientoAceptacion);

        escenario.Repositorio.Becas.Should().ContainSingle()
            .Which.Oferta!.ReferenciaJobExpiracionHangfireId.Should()
            .Be(escenario.Programador.Referencia);

        escenario.Unidad.NivelesSolicitados.Should().ContainSingle()
            .Which.Should().Be(NivelAislamientoTransaccion.ReadCommitted);
        escenario.Unidad.Confirmaciones.Should().Be(1);
        escenario.Unidad.Guardados.Should().Be(1);
        escenario.Outbox.Descartado.Should().BeFalse();
        escenario.Outbox.Confirmados.Should()
            .ContainSingle(evento => evento is OfertaBecaEmitidaEvento);

        escenario.Programas.UltimaSolicitud.Should().NotBeNull();
        escenario.Programas.UltimaSolicitud!.ReferenciaTipo.Should().Be("OFERTA_BECA");
        escenario.Programas.UltimaSolicitud.ReferenciaId.Should().Be(resultado.OfertaId);
        escenario.Programas.UltimaSolicitud.Monto.Should().Be(2000m);
        escenario.Programas.UltimaSolicitud.Cupos.Should().Be(1);
        escenario.Postulaciones.PostulacionSolicitada.Should().Be(escenario.PostulacionId);
        escenario.Periodos.Llamadas.Should().Be(1);
        escenario.Postulaciones.Otorgamientos.Should().Be(1);
        escenario.Programas.Confirmaciones.Should().ContainSingle()
            .Which.Should().Be((escenario.ProgramaBecaId,
                beca.Oferta.ReservaPresupuestoId, resultado.OfertaId));
        escenario.Postulaciones.OtorgamientoSolicitado.Should()
            .Be((escenario.PostulacionId, resultado.OfertaId));
        escenario.Programas.ConsultasCondiciones.Should().Be(1);
        escenario.Intenciones.Pendientes.Should().BeEmpty();
    }

    [Fact]
    public async Task Debe_MantenerIntencion_Cuando_MS_PRGNoConfirmaLaReserva()
    {
        var escenario = new EscenarioOtorgamiento();
        escenario.Programas.ErrorConfirmar = new HttpRequestException(
            "MS-PRG no confirmó la reserva.");

        var accion = () => escenario.CrearEmitir().HandleAsync(
            new EmitirOfertaBecaCommand(escenario.PostulacionId));

        var excepcion = await accion.Should()
            .ThrowAsync<DependenciaNoDisponibleException>();
        excepcion.Which.Dependencia.Should().Be("MS-PRG");
        var intencion = escenario.Intenciones.Pendientes.Should().ContainSingle().Subject;
        escenario.Postulaciones.Otorgamientos.Should().Be(0);
        escenario.Repositorio.Becas.Should().BeEmpty();
        escenario.Almacenamiento.UrlsEliminadas.Should().BeEmpty();

        escenario.Programas.ErrorConfirmar = null;
        var resultado = await new FinalizadorEmisionOferta(
            escenario.Postulaciones, escenario.Programas,
            escenario.Intenciones, escenario.Repositorio, escenario.Unidad,
            escenario.Outbox).ConfirmarAsync(intencion);

        resultado.OfertaId.Should().Be(intencion.OfertaId);
        escenario.Programas.Confirmaciones.Should().ContainSingle();
        escenario.Postulaciones.Otorgamientos.Should().Be(1);
        escenario.Intenciones.Pendientes.Should().BeEmpty();
    }

    [Fact]
    public async Task Debe_UsarElArancelActualDeMS_PRG_AlCalcularLaOferta()
    {
        var escenario = new EscenarioOtorgamiento();
        var proyeccion = escenario.Proyecciones.Proyeccion!;
        escenario.Programas.CrearCondiciones = (_, carreraId, periodoId, universidadId) =>
            new ResultadoCondicionesPrograma(
                escenario.ProgramaBecaId,
                carreraId,
                periodoId,
                universidadId,
                proyeccion.Programa.PoliticaCobertura,
                proyeccion.Programa.CondicionesPermanencia,
                new CostoMatriculaVigente(
                    escenario.CostoMatriculaId,
                    carreraId,
                    periodoId,
                    1200m,
                    "USD",
                    2,
                    DateOnly.FromDateTime(DatosAplicacion.FechaBase.UtcDateTime).AddDays(-1),
                    null,
                    "VIGENTE"),
                new ConvenioVigente(
                    universidadId,
                    escenario.ConvenioId,
                    "VIGENTE",
                    DateOnly.FromDateTime(DatosAplicacion.FechaBase.UtcDateTime).AddYears(1)));
        escenario.Programas.Resultado = DatosAplicacion.Reserva(monto: 2400m);

        await escenario.CrearEmitir().HandleAsync(
            new EmitirOfertaBecaCommand(escenario.PostulacionId));

        escenario.Generador.Datos!.MontoOfertaPorPeriodo.Should().Be(600m);
        escenario.Programas.UltimaSolicitud!.Monto.Should().Be(2400m);
    }

    [Fact]
    public async Task Debe_RechazarLaEmision_Cuando_LaPostulacionNoEsAdjudicable()
    {
        var escenario = new EscenarioOtorgamiento();
        escenario.Postulaciones.Resultado = escenario.Postulaciones.Resultado! with
        {
            Adjudicable = false,
            MotivoNoAdjudicable = "sin cupo disponible"
        };

        var accion = () => escenario.CrearEmitir().HandleAsync(
            new EmitirOfertaBecaCommand(escenario.PostulacionId));

        var excepcion = await accion.Should().ThrowAsync<PostulacionNoAdjudicableException>();

        excepcion.Which.Codigo.Should().Be("POSTULACION_NO_ADJUDICABLE");
        excepcion.Which.Motivo.Should().Be("sin cupo disponible");
        escenario.Proyecciones.Llamadas.Should().Be(0);
        escenario.Programas.Llamadas.Should().Be(0);
        escenario.Generador.Datos.Should().BeNull();
    }

    [Fact]
    public async Task Debe_RechazarLaEmision_Cuando_FaltaLaConfiguracionLocal()
    {
        var escenario = new EscenarioOtorgamiento();
        escenario.Proyecciones.Proyeccion = null;

        var accion = () => escenario.CrearEmitir().HandleAsync(
            new EmitirOfertaBecaCommand(escenario.PostulacionId));

        var excepcion = await accion.Should().ThrowAsync<ConfiguracionProgramaNoDisponibleException>();

        excepcion.Which.Codigo.Should().Be("CONFIGURACION_PROGRAMA_NO_DISPONIBLE");
        excepcion.Which.ProgramaBecaId.Should().Be(escenario.ProgramaBecaId);
        escenario.Programas.Llamadas.Should().Be(0);
        escenario.MetricaConfiguracion.Recursos.Should().ContainSingle()
            .Which.Should().Be("programa");
    }

    [Theory]
    [InlineData("INACTIVO", "VIGENTE", "VIGENTE", "programa")]
    [InlineData("ACTIVO", "VENCIDO", "VIGENTE", "costo_matricula")]
    [InlineData("ACTIVO", "VIGENTE", "VENCIDO", "convenio")]
    public async Task Debe_RechazarLaEmision_Cuando_UnaProyeccionNoEsVigente(
        string estadoPrograma,
        string estadoCosto,
        string estadoConvenio,
        string recursoMetrica)
    {
        var escenario = new EscenarioOtorgamiento();
        escenario.Proyecciones.Proyeccion = DatosAplicacion.Proyeccion(
            escenario.ProgramaBecaId,
            escenario.CostoMatriculaId,
            escenario.ConvenioId,
            DateOnly.FromDateTime(DatosAplicacion.FechaBase.UtcDateTime),
            estadoPrograma,
            estadoCosto,
            estadoConvenio);

        var accion = () => escenario.CrearEmitir().HandleAsync(
            new EmitirOfertaBecaCommand(escenario.PostulacionId));

        var excepcion = await accion.Should().ThrowAsync<ConfiguracionProgramaNoDisponibleException>();

        excepcion.Which.Codigo.Should().Be("CONFIGURACION_PROGRAMA_NO_DISPONIBLE");
        escenario.Programas.Llamadas.Should().Be(0);
        escenario.MetricaConfiguracion.Recursos.Should().ContainSingle()
            .Which.Should().Be(recursoMetrica);
    }

    [Theory]
    [InlineData("RECHAZADA", 2000, 1)]
    [InlineData("PENDIENTE", 1999, 1)]
    [InlineData("PENDIENTE", 2000, 0)]
    public async Task Debe_RechazarLaEmision_Cuando_LaReservaNoEsAceptable(
        string estado,
        decimal monto,
        int cupos)
    {
        var escenario = new EscenarioOtorgamiento();
        escenario.Programas.Resultado = DatosAplicacion.Reserva(
            monto: monto,
            cupos: cupos,
            estado: estado);

        var accion = () => escenario.CrearEmitir().HandleAsync(
            new EmitirOfertaBecaCommand(escenario.PostulacionId));

        var excepcion = await accion.Should().ThrowAsync<ReservaPresupuestoRechazadaException>();

        excepcion.Which.Codigo.Should().Be("SIN_CUPO_O_PRESUPUESTO");
        escenario.Generador.Datos.Should().BeNull();
        escenario.Almacenamiento.RutasSubidas.Should().BeEmpty();
    }

    [Fact]
    public async Task Debe_MapearLaCaidaDeMS_POS_Cuando_ElClienteFalla()
    {
        var escenario = new EscenarioOtorgamiento();
        escenario.Postulaciones.Error = new HttpRequestException("sin conexión");

        var accion = () => escenario.CrearEmitir().HandleAsync(
            new EmitirOfertaBecaCommand(escenario.PostulacionId));

        var excepcion = await accion.Should().ThrowAsync<DependenciaNoDisponibleException>();

        excepcion.Which.Dependencia.Should().Be("MS-POS");
        excepcion.Which.Codigo.Should().Be("DEPENDENCIA_NO_DISPONIBLE");
    }

    [Fact]
    public async Task Debe_FallarCerrado_CuandoElPeriodoActualNoCoincideConLaPostulacion()
    {
        var escenario = new EscenarioOtorgamiento();
        escenario.Periodos.Resultado = escenario.Periodos.Resultado with
        {
            PeriodoAcademicoId = Guid.CreateVersion7()
        };

        var accion = () => escenario.CrearEmitir().HandleAsync(
            new EmitirOfertaBecaCommand(escenario.PostulacionId));

        var excepcion = await accion.Should().ThrowAsync<DependenciaNoDisponibleException>();

        excepcion.Which.Dependencia.Should().Be("MS-PLT");
        escenario.Proyecciones.Llamadas.Should().Be(0);
        escenario.Programas.ConsultasCondiciones.Should().Be(0);
        escenario.Programas.Llamadas.Should().Be(0);
        escenario.Repositorio.Becas.Should().BeEmpty();
    }

    [Fact]
    public async Task Debe_FallarCerrado_Cuando_MS_PRGNoDevuelveLasCondicionesDelPrograma()
    {
        var escenario = new EscenarioOtorgamiento();
        escenario.Programas.ErrorCondiciones = new HttpRequestException("sin conexión");

        var accion = () => escenario.CrearEmitir().HandleAsync(
            new EmitirOfertaBecaCommand(escenario.PostulacionId));

        var excepcion = await accion.Should().ThrowAsync<DependenciaNoDisponibleException>();

        excepcion.Which.Dependencia.Should().Be("MS-PRG");
        escenario.Programas.Llamadas.Should().Be(0);
        escenario.Generador.Datos.Should().BeNull();
        escenario.Repositorio.Becas.Should().BeEmpty();
    }

    [Fact]
    public async Task Debe_ConservarIntencionYRecursos_Cuando_FallaElOtorgamientoEnMS_POS()
    {
        var escenario = new EscenarioOtorgamiento();
        escenario.Postulaciones.ErrorOtorgar = new HttpRequestException("sin conexión");

        var accion = () => escenario.CrearEmitir().HandleAsync(
            new EmitirOfertaBecaCommand(escenario.PostulacionId));

        var excepcion = await accion.Should().ThrowAsync<DependenciaNoDisponibleException>();

        excepcion.Which.Dependencia.Should().Be("MS-POS");
        escenario.Programas.Llamadas.Should().Be(1);
        escenario.Programador.Canceladas.Should().BeEmpty();
        escenario.Almacenamiento.UrlsEliminadas.Should().BeEmpty();
        escenario.Intenciones.Pendientes.Should().ContainSingle()
            .Which.PostulacionId.Should().Be(escenario.PostulacionId);
        escenario.Repositorio.Becas.Should().BeEmpty();
        escenario.Unidad.Confirmaciones.Should().Be(0);
        escenario.Outbox.Confirmados.Should().BeEmpty();

        var intencion = escenario.Intenciones.Pendientes.Single();
        escenario.Postulaciones.ErrorOtorgar = null;
        var resultado = await new FinalizadorEmisionOferta(
            escenario.Postulaciones, escenario.Programas,
            escenario.Intenciones, escenario.Repositorio,
            escenario.Unidad, escenario.Outbox).ConfirmarAsync(intencion);
        resultado.OfertaId.Should().Be(intencion.OfertaId);
        escenario.Postulaciones.Otorgamientos.Should().Be(2);
        escenario.Intenciones.Pendientes.Should().BeEmpty();
        escenario.Repositorio.Becas.Should().ContainSingle();
    }

    [Fact]
    public async Task Debe_MapearLaCaidaDeMS_PRG_Cuando_ElClienteFalla()
    {
        var escenario = new EscenarioOtorgamiento();
        escenario.Programas.Error = new TimeoutException("timeout");

        var accion = () => escenario.CrearEmitir().HandleAsync(
            new EmitirOfertaBecaCommand(escenario.PostulacionId));

        var excepcion = await accion.Should().ThrowAsync<DependenciaNoDisponibleException>();

        excepcion.Which.Dependencia.Should().Be("MS-PRG");
    }

    [Fact]
    public async Task Debe_RechazarLaEmision_Cuando_LaAdjudicabilidadEstaIncompleta()
    {
        var escenario = new EscenarioOtorgamiento();
        escenario.Postulaciones.Resultado = escenario.Postulaciones.Resultado! with
        {
            PostulanteEstudianteId = Guid.Empty
        };

        var accion = () => escenario.CrearEmitir().HandleAsync(
            new EmitirOfertaBecaCommand(escenario.PostulacionId));

        var excepcion = await accion.Should().ThrowAsync<DependenciaNoDisponibleException>();

        excepcion.Which.Dependencia.Should().Be("MS-POS");
    }

    [Fact]
    public async Task Debe_ReintentarLaMismaOferta_Cuando_FallaLaPersistenciaLocal()
    {
        var escenario = new EscenarioOtorgamiento();
        escenario.Unidad.ErrorGuardado = new InvalidOperationException("fallo de persistencia");

        var accion = () => escenario.CrearEmitir().HandleAsync(
            new EmitirOfertaBecaCommand(escenario.PostulacionId));

        await accion.Should().ThrowAsync<InvalidOperationException>();

        escenario.Programador.Canceladas.Should().BeEmpty();
        escenario.Almacenamiento.UrlsEliminadas.Should().BeEmpty();
        var intencion = escenario.Intenciones.Pendientes.Should().ContainSingle().Subject;
        escenario.Postulaciones.OtorgamientoSolicitado.Should()
            .Be((escenario.PostulacionId, intencion.OfertaId));
        escenario.Unidad.Confirmaciones.Should().Be(0);
        escenario.Unidad.Reversiones.Should().Be(1);
        escenario.Outbox.Descartado.Should().BeTrue();
        escenario.Outbox.Confirmados.Should().BeEmpty();

        escenario.Unidad.ErrorGuardado = null;
        var resultado = await new FinalizadorEmisionOferta(
            escenario.Postulaciones, escenario.Programas,
            escenario.Intenciones, escenario.Repositorio,
            escenario.Unidad, escenario.Outbox).ConfirmarAsync(intencion);

        resultado.OfertaId.Should().Be(intencion.OfertaId);
        escenario.Postulaciones.Otorgamientos.Should().Be(2);
        escenario.Intenciones.Pendientes.Should().BeEmpty();
        escenario.Outbox.Confirmados.Should()
            .ContainSingle(evento => evento is OfertaBecaEmitidaEvento);
    }

    [Fact]
    public async Task Debe_ConservarRecursos_Cuando_PersisteIntencionPeroSePierdeRespuesta()
    {
        var escenario = new EscenarioOtorgamiento();
        escenario.Intenciones.ErrorGuardarDespues = new IOException(
            "Se perdió la respuesta tras el commit de la intención.");

        var accion = () => escenario.CrearEmitir().HandleAsync(
            new EmitirOfertaBecaCommand(escenario.PostulacionId));

        await accion.Should().ThrowAsync<IOException>();
        escenario.Intenciones.Pendientes.Should().ContainSingle();
        escenario.Almacenamiento.UrlsEliminadas.Should().BeEmpty();
        escenario.Programador.Canceladas.Should().BeEmpty();
        escenario.Postulaciones.Otorgamientos.Should().Be(0);
    }

    [Fact]
    public async Task Debe_CompensarRecursos_Cuando_IntencionNoSeGuardo()
    {
        var escenario = new EscenarioOtorgamiento();
        escenario.Intenciones.ErrorGuardarAntes = new IOException(
            "No se pudo guardar la intención.");

        var accion = () => escenario.CrearEmitir().HandleAsync(
            new EmitirOfertaBecaCommand(escenario.PostulacionId));

        await accion.Should().ThrowAsync<IOException>();
        escenario.Intenciones.Pendientes.Should().BeEmpty();
        escenario.Almacenamiento.UrlsEliminadas.Should().ContainSingle();
        escenario.Programador.Canceladas.Should().ContainSingle();
        escenario.Postulaciones.Otorgamientos.Should().Be(0);
    }

    [Fact]
    public async Task Debe_CompensarLaCarta_Cuando_FallaLaProgramacionDelJob()
    {
        var escenario = new EscenarioOtorgamiento();
        escenario.Programador.ErrorProgramar = new InvalidOperationException("fallo del planificador");

        var accion = () => escenario.CrearEmitir().HandleAsync(
            new EmitirOfertaBecaCommand(escenario.PostulacionId));

        var excepcion = await accion.Should().ThrowAsync<DependenciaNoDisponibleException>();

        excepcion.Which.Dependencia.Should().Be("planificador-expiracion");
        escenario.Almacenamiento.UrlsEliminadas.Should().ContainSingle()
            .Which.Should().Be(escenario.Almacenamiento.Resultado.Url);
        escenario.Repositorio.Becas.Should().BeEmpty();
    }

    [Fact]
    public async Task Debe_FallarSinPersistir_Cuando_LaSubidaDeLaCartaFalla()
    {
        var escenario = new EscenarioOtorgamiento();
        escenario.Almacenamiento.ErrorSubida = new IOException("fallo de almacenamiento");

        var accion = () => escenario.CrearEmitir().HandleAsync(
            new EmitirOfertaBecaCommand(escenario.PostulacionId));

        var excepcion = await accion.Should().ThrowAsync<DependenciaNoDisponibleException>();

        excepcion.Which.Dependencia.Should().Be("almacenamiento");
        escenario.Programador.Programadas.Should().BeEmpty();
        escenario.Repositorio.Becas.Should().BeEmpty();
        escenario.Unidad.Confirmaciones.Should().Be(0);
    }

    [Fact]
    public async Task Debe_EliminarLaCarta_Cuando_ElAlmacenamientoDevuelveUnHashInvalido()
    {
        var escenario = new EscenarioOtorgamiento();
        escenario.Almacenamiento.CalcularHash = false;
        escenario.Almacenamiento.Resultado = new ArchivoAlmacenado(
            "https://storage.example/becas/carta.pdf",
            "hash-invalido",
            10);

        var accion = () => escenario.CrearEmitir().HandleAsync(
            new EmitirOfertaBecaCommand(escenario.PostulacionId));

        var excepcion = await accion.Should().ThrowAsync<DependenciaNoDisponibleException>();

        excepcion.Which.Dependencia.Should().Be("almacenamiento");
        escenario.Almacenamiento.UrlsEliminadas.Should().ContainSingle()
            .Which.Should().Be("https://storage.example/becas/carta.pdf");
        escenario.Repositorio.Becas.Should().BeEmpty();
    }

    [Fact]
    public async Task Debe_Fallar_Cuando_ElGeneradorDeCartaFalla()
    {
        var escenario = new EscenarioOtorgamiento();
        escenario.Generador.Error = new InvalidOperationException("fallo del generador");

        var accion = () => escenario.CrearEmitir().HandleAsync(
            new EmitirOfertaBecaCommand(escenario.PostulacionId));

        var excepcion = await accion.Should().ThrowAsync<DependenciaNoDisponibleException>();

        excepcion.Which.Dependencia.Should().Be("generador-carta");
        escenario.Almacenamiento.RutasSubidas.Should().BeEmpty();
    }

    [Fact]
    public async Task Debe_GenerarYAlmacenarLaPlantillaDelCompromiso_Cuando_ElProgramaLoExige()
    {
        var escenario = new EscenarioOtorgamiento();
        ExigirCompromiso(escenario);
        ConfigurarUrlsPorRuta(escenario);

        var resultado = await escenario.CrearEmitir().HandleAsync(
            new EmitirOfertaBecaCommand(escenario.PostulacionId));

        var beca = escenario.Repositorio.Becas.Should().ContainSingle().Subject;
        var rutaPlantilla =
            $"becas/{resultado.BecaId}/compromisos/plantilla-compromiso.pdf";

        beca.Compromiso.Should().NotBeNull();
        beca.Compromiso!.Estado.Should().Be(EstadoCompromiso.PendienteCarga);
        beca.Compromiso.UrlPlantillaGeneradaFirebase.Should().Be(
            $"https://storage.example/{rutaPlantilla}");
        beca.Compromiso.UrlPdfFirmadoFirebase.Should().BeNull();
        beca.Compromiso.NumeroIntentoCarga.Should().Be(0);

        escenario.GeneradorPlantilla.Datos.Should().NotBeNull();
        var datos = escenario.GeneradorPlantilla.Datos!;
        datos.BecaId.Should().Be(resultado.BecaId);
        datos.CodigoBeca.Should().Be(beca.CodigoBeca);
        datos.NombreEstudiante.Should().Be("Ana Pérez");
        datos.NumeroDocumentoEstudiante.Should().Be("12345678");
        datos.NombrePrograma.Should().Be("Programa de becas de prueba");
        datos.PeriodoCodigo.Should().Be("2026-2");
        datos.Cobertura.Should().Be(beca.CoberturaAplicada);
        datos.MontoPorPeriodo.Should().Be(500m);
        datos.DuracionPeriodos.Should().Be(4);
        datos.Condiciones.PromedioMinimoRenovacion.Should().Be(14m);
        datos.Condiciones.MateriasMinimasAprobadas.Should().Be(4);
        datos.Condiciones.MaximoMateriasReprobadas.Should().Be(1);
        datos.Condiciones.HorasServicioPorPeriodo.Should().Be(80);
        datos.Condiciones.ConsecuenciaIncumplimiento.Should()
            .Be(ConsecuenciaPermanencia.Suspension);
        datos.FechaEmision.Should().Be(
            DateOnly.FromDateTime(DatosAplicacion.FechaBase.UtcDateTime));

        escenario.Almacenamiento.RutasSubidas.Should().HaveCount(2);
        escenario.Almacenamiento.RutasSubidas.Should().Contain(
            $"becas/{resultado.BecaId}/ofertas/{resultado.OfertaId}/carta-otorgamiento.pdf");
        escenario.Almacenamiento.RutasSubidas.Should().Contain(rutaPlantilla);
        escenario.Almacenamiento.UrlsEliminadas.Should().BeEmpty();
    }

    [Fact]
    public async Task Debe_NoGenerarLaPlantilla_Cuando_ElProgramaNoExigeCompromiso()
    {
        var escenario = new EscenarioOtorgamiento();

        var resultado = await escenario.CrearEmitir().HandleAsync(
            new EmitirOfertaBecaCommand(escenario.PostulacionId));

        var beca = escenario.Repositorio.Becas.Should().ContainSingle().Subject;

        beca.Compromiso.Should().BeNull();
        escenario.GeneradorPlantilla.Datos.Should().BeNull();
        escenario.Almacenamiento.RutasSubidas.Should().ContainSingle()
            .Which.Should().Be(
                $"becas/{resultado.BecaId}/ofertas/{resultado.OfertaId}/carta-otorgamiento.pdf");
    }

    [Fact]
    public async Task Debe_ConservarCartaYPlantilla_Cuando_FallaLaPersistencia()
    {
        var escenario = new EscenarioOtorgamiento();
        ExigirCompromiso(escenario);
        ConfigurarUrlsPorRuta(escenario);
        escenario.Unidad.ErrorGuardado = new InvalidOperationException("fallo de persistencia");

        var accion = () => escenario.CrearEmitir().HandleAsync(
            new EmitirOfertaBecaCommand(escenario.PostulacionId));

        await accion.Should().ThrowAsync<InvalidOperationException>();

        escenario.Almacenamiento.UrlsEliminadas.Should().BeEmpty();
        escenario.Intenciones.Pendientes.Should().ContainSingle();
        escenario.Unidad.Confirmaciones.Should().Be(0);
        escenario.Unidad.Reversiones.Should().Be(1);
    }

    [Fact]
    public async Task Debe_FallarYCompensarLaCarta_Cuando_ElGeneradorDePlantillaFalla()
    {
        var escenario = new EscenarioOtorgamiento();
        ExigirCompromiso(escenario);
        ConfigurarUrlsPorRuta(escenario);
        escenario.GeneradorPlantilla.Error =
            new InvalidOperationException("fallo del generador de plantilla");

        var accion = () => escenario.CrearEmitir().HandleAsync(
            new EmitirOfertaBecaCommand(escenario.PostulacionId));

        var excepcion = await accion.Should().ThrowAsync<DependenciaNoDisponibleException>();

        excepcion.Which.Dependencia.Should().Be("generador-plantilla-compromiso");
        escenario.Almacenamiento.RutasSubidas.Should().ContainSingle()
            .Which.Should().Contain("carta-otorgamiento.pdf");
        escenario.Almacenamiento.UrlsEliminadas.Should().ContainSingle()
            .Which.Should().Contain("carta-otorgamiento.pdf");
        escenario.Repositorio.Becas.Should().BeEmpty();
    }

    [Fact]
    public async Task Debe_FallarYCompensarLaCarta_Cuando_LaSubidaDeLaPlantillaFalla()
    {
        var escenario = new EscenarioOtorgamiento();
        ExigirCompromiso(escenario);
        ConfigurarUrlsPorRuta(escenario);
        escenario.Almacenamiento.ErrorSubidaSegunRuta = ruta =>
            ruta.EndsWith("/plantilla-compromiso.pdf", StringComparison.Ordinal)
                ? new IOException("fallo de almacenamiento de la plantilla")
                : null;

        var accion = () => escenario.CrearEmitir().HandleAsync(
            new EmitirOfertaBecaCommand(escenario.PostulacionId));

        var excepcion = await accion.Should().ThrowAsync<DependenciaNoDisponibleException>();

        excepcion.Which.Dependencia.Should().Be("almacenamiento");
        escenario.Almacenamiento.UrlsEliminadas.Should().ContainSingle()
            .Which.Should().Contain("carta-otorgamiento.pdf");
        escenario.Repositorio.Becas.Should().BeEmpty();
        escenario.Unidad.Confirmaciones.Should().Be(0);
    }

    private static void ExigirCompromiso(EscenarioOtorgamiento escenario)
    {
        var proyeccion = escenario.Proyecciones.Proyeccion
            ?? throw new InvalidOperationException("La proyección de prueba no está configurada.");

        escenario.Proyecciones.Proyeccion = proyeccion with
        {
            Programa = proyeccion.Programa with
            {
                RequiereCompromisoFirmado = true
            }
        };
    }

    private static void ConfigurarUrlsPorRuta(EscenarioOtorgamiento escenario)
        => escenario.Almacenamiento.CrearResultado = ruta => new ArchivoAlmacenado(
            $"https://storage.example/{ruta}",
            new string('a', 64),
            1);
}
