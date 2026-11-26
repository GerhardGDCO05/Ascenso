using Ascenso.Postulaciones.Application.Asignaciones.Comandos.AsignarEvaluadoresPostulacion;
using Ascenso.Postulaciones.Application.Asignaciones.Comandos.AutorizarExcepcionEvaluadorUnico;
using Ascenso.Postulaciones.Application.Asignaciones.Consultas.ObtenerAsignacionesPostulacion;
using Ascenso.Postulaciones.Application.Asignaciones.Consultas.ObtenerCargaEvaluadoresComite;
using Ascenso.Postulaciones.Application.Asignaciones.Consultas.ObtenerExcepcionEvaluadorUnico;
using Ascenso.Postulaciones.Application.Asignaciones.Consultas.ObtenerMisAsignaciones;
using Ascenso.Postulaciones.Application.Comites.Comandos.AgregarMiembroComite;
using Ascenso.Postulaciones.Application.Comites.Comandos.CrearComiteEvaluador;
using Ascenso.Postulaciones.Application.Comites.Comandos.DesvincularMiembroComite;
using Ascenso.Postulaciones.Application.Comites.Consultas.ObtenerComiteEvaluador;
using Ascenso.Postulaciones.Application.Comites.Consultas.ObtenerComitePorPrograma;
using Ascenso.Postulaciones.Application.Consolidacion.Comandos.ConfirmarRankingConvocatoria;
using Ascenso.Postulaciones.Application.Consolidacion.Comandos.ConsolidarPostulacion;
using Ascenso.Postulaciones.Application.Consolidacion.Comandos.RevisarDiscrepanciaConsolidacion;
using Ascenso.Postulaciones.Application.Consolidacion.Consultas.ConsultarAdjudicabilidadPostulacion;
using Ascenso.Postulaciones.Application.Consolidacion.Consultas.ListarResultadosConsolidadosConvocatoria;
using Ascenso.Postulaciones.Application.Consolidacion.Consultas.ObtenerRankingConvocatoria;
using Ascenso.Postulaciones.Application.Consolidacion.Consultas.ObtenerResultadoConsolidadoPostulacion;
using Ascenso.Postulaciones.Application.Consolidacion.Interfaces;
using Ascenso.Postulaciones.Application.Consolidacion.Servicios;
using Ascenso.Postulaciones.Application.Evaluaciones.Comandos.EnviarEvaluacionDefinitiva;
using Ascenso.Postulaciones.Application.Evaluaciones.Comandos.GuardarBorradorEvaluacion;
using Ascenso.Postulaciones.Application.Evaluaciones.Consultas.ObtenerEvaluacionesCoevaluadoresPostulacion;
using Ascenso.Postulaciones.Application.Evaluaciones.Consultas.ObtenerEvaluacionesPostulacion;
using Ascenso.Postulaciones.Application.Evaluaciones.Consultas.ObtenerMiEvaluacionPorAsignacion;
using Ascenso.Postulaciones.Application.Evaluaciones.Consultas.ObtenerMisEvaluaciones;
using Ascenso.Postulaciones.Application.Evaluaciones.Interfaces;
using Ascenso.Postulaciones.Application.Evaluaciones.Servicios;
using Ascenso.Postulaciones.Application.Interfaces;
using Ascenso.Postulaciones.Application.Postulantes.Comandos.ActualizarPerfilSocioeconomico;
using Ascenso.Postulaciones.Application.Postulantes.Comandos.RegistrarPostulante;
using Ascenso.Postulaciones.Application.Postulantes.Consultas.ObtenerMiPerfil;
using Ascenso.Postulaciones.Application.Rubricas.Comandos.AgregarCriterio;
using Ascenso.Postulaciones.Application.Rubricas.Comandos.BloquearRubrica;
using Ascenso.Postulaciones.Application.Rubricas.Comandos.CrearRubrica;
using Ascenso.Postulaciones.Application.Rubricas.Comandos.EditarCriterio;
using Ascenso.Postulaciones.Application.Rubricas.Comandos.EliminarCriterio;
using Ascenso.Postulaciones.Application.Rubricas.Comandos.PublicarRubrica;
using Ascenso.Postulaciones.Application.Rubricas.Consultas.ObtenerRubrica;
using Ascenso.Postulaciones.Domain.Interfaces;
using FluentAssertions;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Ascenso.Postulaciones.Application.Tests.Configuracion;

public sealed class InyeccionDependenciasTests
{
    [Fact]
    public void Debe_ResolverLosHandlersYValidadores_Cuando_SeRegistraLaAplicacion()
    {
        var servicios = new ServiceCollection();

        servicios.AddSingleton(Substitute.For<IUsuarioActual>());
        servicios.AddSingleton(Substitute.For<IPostulanteRepository>());
        servicios.AddSingleton(Substitute.For<IRubricaRepository>());
        servicios.AddSingleton(Substitute.For<IComiteEvaluadorRepository>());
        servicios.AddSingleton(Substitute.For<IAsignacionEvaluacionRepository>());
        servicios.AddSingleton(Substitute.For<IExcepcionEvaluadorUnicoRepository>());
        servicios.AddSingleton(Substitute.For<IEvaluacionRepository>());
        servicios.AddSingleton(Substitute.For<IConsultaAsignacionParaEvaluacionRepository>());
        servicios.AddSingleton(Substitute.For<IConsultaRubricaParaEvaluacionRepository>());
        servicios.AddSingleton(Substitute.For<IConsultaEvaluacionesCoevaluadoresRepository>());
        servicios.AddSingleton(Substitute.For<IResultadoConsolidadoRepository>());
        servicios.AddSingleton(Substitute.For<IConsultaDatosConsolidacionRepository>());
        servicios.AddSingleton(Substitute.For<IProveedorParametrosConsolidacion>());
        servicios.AddSingleton(Substitute.For<IRankingConvocatoriaRepository>());
        servicios.AddSingleton(Substitute.For<IConsultaEstadoEvaluacionesConvocatoriaRepository>());
        servicios.AddSingleton(Substitute.For<IDisponibilidadRankingClient>());
        servicios.AddSingleton(Substitute.For<IProveedorMontoEstimadoBeca>());
        servicios.AddSingleton(Substitute.For<IPeriodoAcademicoClient>());
        servicios.AddSingleton(Substitute.For<ICatalogoAcademicoClient>());
        servicios.AddSingleton(Substitute.For<IConvocatoriaEvaluacionClient>());
        servicios.AddSingleton(Substitute.For<IUnitOfWork>());
        servicios.AddSingleton(TimeProvider.System);

        servicios.AgregarAplicacionPostulaciones();

        using var proveedor = servicios.BuildServiceProvider();
        using var alcance = proveedor.CreateScope();

        alcance.ServiceProvider.GetRequiredService<RegistrarPostulanteCommandHandler>()
            .Should().NotBeNull();
        alcance.ServiceProvider.GetRequiredService<ActualizarPerfilSocioeconomicoCommandHandler>()
            .Should().NotBeNull();
        alcance.ServiceProvider.GetRequiredService<ObtenerMiPerfilQueryHandler>()
            .Should().NotBeNull();
        alcance.ServiceProvider.GetRequiredService<CrearRubricaCommandHandler>()
            .Should().NotBeNull();
        alcance.ServiceProvider.GetRequiredService<AgregarCriterioCommandHandler>()
            .Should().NotBeNull();
        alcance.ServiceProvider.GetRequiredService<EditarCriterioCommandHandler>()
            .Should().NotBeNull();
        alcance.ServiceProvider.GetRequiredService<EliminarCriterioCommandHandler>()
            .Should().NotBeNull();
        alcance.ServiceProvider.GetRequiredService<PublicarRubricaCommandHandler>()
            .Should().NotBeNull();
        alcance.ServiceProvider.GetRequiredService<BloquearRubricaCommandHandler>()
            .Should().NotBeNull();
        alcance.ServiceProvider.GetRequiredService<ObtenerRubricaQueryHandler>()
            .Should().NotBeNull();
        alcance.ServiceProvider.GetRequiredService<CrearComiteEvaluadorCommandHandler>()
            .Should().NotBeNull();
        alcance.ServiceProvider.GetRequiredService<AgregarMiembroComiteCommandHandler>()
            .Should().NotBeNull();
        alcance.ServiceProvider.GetRequiredService<DesvincularMiembroComiteCommandHandler>()
            .Should().NotBeNull();
        alcance.ServiceProvider.GetRequiredService<ObtenerComiteEvaluadorQueryHandler>()
            .Should().NotBeNull();
        alcance.ServiceProvider.GetRequiredService<ObtenerComitePorProgramaQueryHandler>()
            .Should().NotBeNull();
        alcance.ServiceProvider.GetRequiredService<AsignarEvaluadoresPostulacionCommandHandler>()
            .Should().NotBeNull();
        alcance.ServiceProvider.GetRequiredService<ObtenerAsignacionesPostulacionQueryHandler>()
            .Should().NotBeNull();
        alcance.ServiceProvider.GetRequiredService<ObtenerCargaEvaluadoresComiteQueryHandler>()
            .Should().NotBeNull();
        alcance.ServiceProvider.GetRequiredService<ObtenerMisAsignacionesQueryHandler>()
            .Should().NotBeNull();
        alcance.ServiceProvider.GetRequiredService<AutorizarExcepcionEvaluadorUnicoCommandHandler>()
            .Should().NotBeNull();
        alcance.ServiceProvider.GetRequiredService<ObtenerExcepcionEvaluadorUnicoQueryHandler>()
            .Should().NotBeNull();
        alcance.ServiceProvider.GetRequiredService<IBloqueadorRubricaEvaluacion>()
            .Should().BeOfType<BloqueadorRubricaEvaluacionAdapter>();
        alcance.ServiceProvider.GetRequiredService<GuardarBorradorEvaluacionCommandHandler>()
            .Should().NotBeNull();
        alcance.ServiceProvider.GetRequiredService<EnviarEvaluacionDefinitivaCommandHandler>()
            .Should().NotBeNull();
        alcance.ServiceProvider.GetRequiredService<ObtenerMiEvaluacionPorAsignacionQueryHandler>()
            .Should().NotBeNull();
        alcance.ServiceProvider.GetRequiredService<ObtenerMisEvaluacionesQueryHandler>()
            .Should().NotBeNull();
        alcance.ServiceProvider.GetRequiredService<ObtenerEvaluacionesPostulacionQueryHandler>()
            .Should().NotBeNull();
        alcance.ServiceProvider.GetRequiredService<ObtenerEvaluacionesCoevaluadoresPostulacionQueryHandler>()
            .Should().NotBeNull();
        alcance.ServiceProvider.GetRequiredService<IConsolidadorPostulacionEvaluacion>()
            .Should().BeOfType<ConsolidadorPostulacionEvaluacionAdapter>();
        alcance.ServiceProvider.GetRequiredService<ConsolidarPostulacionCommandHandler>()
            .Should().NotBeNull();
        alcance.ServiceProvider.GetRequiredService<RevisarDiscrepanciaConsolidacionCommandHandler>()
            .Should().NotBeNull();
        alcance.ServiceProvider.GetRequiredService<ObtenerResultadoConsolidadoPostulacionQueryHandler>()
            .Should().NotBeNull();
        alcance.ServiceProvider.GetRequiredService<ListarResultadosConsolidadosConvocatoriaQueryHandler>()
            .Should().NotBeNull();
        alcance.ServiceProvider.GetRequiredService<ObtenerRankingConvocatoriaQueryHandler>()
            .Should().NotBeNull();
        alcance.ServiceProvider.GetRequiredService<ConfirmarRankingConvocatoriaCommandHandler>()
            .Should().NotBeNull();
        alcance.ServiceProvider.GetRequiredService<ConsultarAdjudicabilidadPostulacionQueryHandler>()
            .Should().NotBeNull();

        var validadoresRegistro = alcance.ServiceProvider
            .GetServices<IValidator<RegistrarPostulanteCommand>>();

        validadoresRegistro.Should().ContainSingle(
            validador => validador is RegistrarPostulanteCommandValidator);

        var validadoresSocioeconomicos = alcance.ServiceProvider
            .GetServices<IValidator<ActualizarPerfilSocioeconomicoCommand>>();

        validadoresSocioeconomicos.Should().ContainSingle(
            validador => validador is ActualizarPerfilSocioeconomicoCommandValidator);

        var validadoresFamiliares = alcance.ServiceProvider
            .GetServices<IValidator<DatosMiembroFamiliarCommand>>();

        validadoresFamiliares.Should().ContainSingle(
            validador => validador is DatosMiembroFamiliarCommandValidator);

        var validadoresCrearRubrica = alcance.ServiceProvider
            .GetServices<IValidator<CrearRubricaCommand>>();

        validadoresCrearRubrica.Should().ContainSingle(
            validador => validador is CrearRubricaCommandValidator);

        var validadoresAgregarCriterio = alcance.ServiceProvider
            .GetServices<IValidator<AgregarCriterioCommand>>();

        validadoresAgregarCriterio.Should().ContainSingle(
            validador => validador is AgregarCriterioCommandValidator);

        var validadoresEditarCriterio = alcance.ServiceProvider
            .GetServices<IValidator<EditarCriterioCommand>>();

        validadoresEditarCriterio.Should().ContainSingle(
            validador => validador is EditarCriterioCommandValidator);

        var validadoresCrearComite = alcance.ServiceProvider
            .GetServices<IValidator<CrearComiteEvaluadorCommand>>();

        validadoresCrearComite.Should().ContainSingle(
            validador => validador is CrearComiteEvaluadorCommandValidator);

        var validadoresAgregarMiembro = alcance.ServiceProvider
            .GetServices<IValidator<AgregarMiembroComiteCommand>>();

        validadoresAgregarMiembro.Should().ContainSingle(
            validador => validador is AgregarMiembroComiteCommandValidator);

        var validadoresDesvincularMiembro = alcance.ServiceProvider
            .GetServices<IValidator<DesvincularMiembroComiteCommand>>();

        validadoresDesvincularMiembro.Should().ContainSingle(
            validador => validador is DesvincularMiembroComiteCommandValidator);

        var validadoresAsignarEvaluadores = alcance.ServiceProvider
            .GetServices<IValidator<AsignarEvaluadoresPostulacionCommand>>();

        validadoresAsignarEvaluadores.Should().ContainSingle(
            validador => validador is AsignarEvaluadoresPostulacionCommandValidator);

        var validadoresObtenerAsignaciones = alcance.ServiceProvider
            .GetServices<IValidator<ObtenerAsignacionesPostulacionQuery>>();

        validadoresObtenerAsignaciones.Should().ContainSingle(
            validador => validador is ObtenerAsignacionesPostulacionQueryValidator);

        var validadoresObtenerCarga = alcance.ServiceProvider
            .GetServices<IValidator<ObtenerCargaEvaluadoresComiteQuery>>();

        validadoresObtenerCarga.Should().ContainSingle(
            validador => validador is ObtenerCargaEvaluadoresComiteQueryValidator);

        var validadoresMisAsignaciones = alcance.ServiceProvider
            .GetServices<IValidator<ObtenerMisAsignacionesQuery>>();

        validadoresMisAsignaciones.Should().ContainSingle(
            validador => validador is ObtenerMisAsignacionesQueryValidator);

        var validadoresAutorizarExcepcion = alcance.ServiceProvider
            .GetServices<IValidator<AutorizarExcepcionEvaluadorUnicoCommand>>();

        validadoresAutorizarExcepcion.Should().ContainSingle(
            validador => validador is AutorizarExcepcionEvaluadorUnicoCommandValidator);

        var validadoresObtenerExcepcion = alcance.ServiceProvider
            .GetServices<IValidator<ObtenerExcepcionEvaluadorUnicoQuery>>();

        validadoresObtenerExcepcion.Should().ContainSingle(
            validador => validador is ObtenerExcepcionEvaluadorUnicoQueryValidator);

        var validadoresGuardarBorrador = alcance.ServiceProvider
            .GetServices<IValidator<GuardarBorradorEvaluacionCommand>>();

        validadoresGuardarBorrador.Should().ContainSingle(
            validador => validador is GuardarBorradorEvaluacionCommandValidator);

        var validadoresEnviarDefinitiva = alcance.ServiceProvider
            .GetServices<IValidator<EnviarEvaluacionDefinitivaCommand>>();

        validadoresEnviarDefinitiva.Should().ContainSingle(
            validador => validador is EnviarEvaluacionDefinitivaCommandValidator);

        var validadoresMiEvaluacion = alcance.ServiceProvider
            .GetServices<IValidator<ObtenerMiEvaluacionPorAsignacionQuery>>();

        validadoresMiEvaluacion.Should().ContainSingle(
            validador => validador is ObtenerMiEvaluacionPorAsignacionQueryValidator);

        var validadoresMisEvaluaciones = alcance.ServiceProvider
            .GetServices<IValidator<ObtenerMisEvaluacionesQuery>>();

        validadoresMisEvaluaciones.Should().ContainSingle(
            validador => validador is ObtenerMisEvaluacionesQueryValidator);

        var validadoresEvaluacionesPostulacion = alcance.ServiceProvider
            .GetServices<IValidator<ObtenerEvaluacionesPostulacionQuery>>();

        validadoresEvaluacionesPostulacion.Should().ContainSingle(
            validador => validador is ObtenerEvaluacionesPostulacionQueryValidator);

        var validadoresEvaluacionesCoevaluadores = alcance.ServiceProvider
            .GetServices<IValidator<ObtenerEvaluacionesCoevaluadoresPostulacionQuery>>();

        validadoresEvaluacionesCoevaluadores.Should().ContainSingle(
            validador => validador is ObtenerEvaluacionesCoevaluadoresPostulacionQueryValidator);

        var validadoresConsolidarPostulacion = alcance.ServiceProvider
            .GetServices<IValidator<ConsolidarPostulacionCommand>>();

        validadoresConsolidarPostulacion.Should().ContainSingle(
            validador => validador is ConsolidarPostulacionCommandValidator);

        var validadoresRevisarDiscrepancia = alcance.ServiceProvider
            .GetServices<IValidator<RevisarDiscrepanciaConsolidacionCommand>>();

        validadoresRevisarDiscrepancia.Should().ContainSingle(
            validador => validador is RevisarDiscrepanciaConsolidacionCommandValidator);

        var validadoresObtenerConsolidado = alcance.ServiceProvider
            .GetServices<IValidator<ObtenerResultadoConsolidadoPostulacionQuery>>();

        validadoresObtenerConsolidado.Should().ContainSingle(
            validador => validador is ObtenerResultadoConsolidadoPostulacionQueryValidator);

        var validadoresListarConsolidados = alcance.ServiceProvider
            .GetServices<IValidator<ListarResultadosConsolidadosConvocatoriaQuery>>();

        validadoresListarConsolidados.Should().ContainSingle(
            validador => validador is ListarResultadosConsolidadosConvocatoriaQueryValidator);

        var validadoresObtenerRanking = alcance.ServiceProvider
            .GetServices<IValidator<ObtenerRankingConvocatoriaQuery>>();

        validadoresObtenerRanking.Should().ContainSingle(
            validador => validador is ObtenerRankingConvocatoriaQueryValidator);

        var validadoresConfirmarRanking = alcance.ServiceProvider
            .GetServices<IValidator<ConfirmarRankingConvocatoriaCommand>>();

        validadoresConfirmarRanking.Should().ContainSingle(
            validador => validador is ConfirmarRankingConvocatoriaCommandValidator);

        var validadoresConsultarAdjudicabilidad = alcance.ServiceProvider
            .GetServices<IValidator<ConsultarAdjudicabilidadPostulacionQuery>>();

        validadoresConsultarAdjudicabilidad.Should().ContainSingle(
            validador => validador is ConsultarAdjudicabilidadPostulacionQueryValidator);
    }
}
