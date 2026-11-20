using System.Security.Cryptography;
using System.Text;
using Ascenso.Becas.Application.Excepciones;
using Ascenso.Becas.Application.Otorgamiento.Comandos.EmitirOfertaBeca;
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
    public async Task Debe_CompensarArchivoYJob_Cuando_FallaLaPersistencia()
    {
        var escenario = new EscenarioOtorgamiento();
        escenario.Unidad.ErrorGuardado = new InvalidOperationException("fallo de persistencia");

        var accion = () => escenario.CrearEmitir().HandleAsync(
            new EmitirOfertaBecaCommand(escenario.PostulacionId));

        await accion.Should().ThrowAsync<InvalidOperationException>();

        escenario.Programador.Canceladas.Should().ContainSingle()
            .Which.Should().Be(escenario.Programador.Referencia);
        escenario.Almacenamiento.UrlsEliminadas.Should().ContainSingle()
            .Which.Should().Be(escenario.Almacenamiento.Resultado.Url);
        escenario.Unidad.Confirmaciones.Should().Be(0);
        escenario.Unidad.Reversiones.Should().Be(1);
        escenario.Outbox.Descartado.Should().BeTrue();
        escenario.Outbox.Confirmados.Should().BeEmpty();
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
}
