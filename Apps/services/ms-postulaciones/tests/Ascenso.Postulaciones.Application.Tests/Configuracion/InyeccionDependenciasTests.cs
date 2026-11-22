using Ascenso.Postulaciones.Application.Interfaces;
using Ascenso.Postulaciones.Application.Postulantes.Comandos.ActualizarPerfilSocioeconomico;
using Ascenso.Postulaciones.Application.Postulantes.Comandos.RegistrarPostulante;
using Ascenso.Postulaciones.Application.Postulantes.Consultas.ObtenerMiPerfil;
using Ascenso.Postulaciones.Application.Rubricas.Comandos.AgregarCriterio;
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
        servicios.AddSingleton(Substitute.For<IPeriodoAcademicoClient>());
        servicios.AddSingleton(Substitute.For<ICatalogoAcademicoClient>());
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
        alcance.ServiceProvider.GetRequiredService<ObtenerRubricaQueryHandler>()
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
    }
}
