using Ascenso.Postulaciones.Application.Interfaces;
using Ascenso.Postulaciones.Application.Postulantes.Comandos.RegistrarPostulante;
using Ascenso.Postulaciones.Application.Postulantes.Consultas.ObtenerMiPerfil;
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
        servicios.AddSingleton(Substitute.For<IPeriodoAcademicoClient>());
        servicios.AddSingleton(Substitute.For<ICatalogoAcademicoClient>());
        servicios.AddSingleton(Substitute.For<IUnitOfWork>());
        servicios.AddSingleton(TimeProvider.System);

        servicios.AgregarAplicacionPostulaciones();

        using var proveedor = servicios.BuildServiceProvider();
        using var alcance = proveedor.CreateScope();

        alcance.ServiceProvider.GetRequiredService<RegistrarPostulanteCommandHandler>()
            .Should().NotBeNull();
        alcance.ServiceProvider.GetRequiredService<ObtenerMiPerfilQueryHandler>()
            .Should().NotBeNull();

        var validadores = alcance.ServiceProvider
            .GetServices<IValidator<RegistrarPostulanteCommand>>();

        validadores.Should().ContainSingle(
            validador => validador is RegistrarPostulanteCommandValidator);
    }
}
