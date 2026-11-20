using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Interfaces;
using Ascenso.Postulaciones.Domain.Postulantes.Agregados;
using Ascenso.Postulaciones.Domain.Postulantes.ObjetosValor;
using Ascenso.Postulaciones.Infrastructure;
using Ascenso.Postulaciones.Infrastructure.Persistence;
using Ascenso.Postulaciones.Infrastructure.Persistence.Repositorios;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Ascenso.Postulaciones.IntegrationTests.Persistencia;

public sealed class PostulanteRepositoryTests
{
    private static PostulacionesDbContext CrearContexto()
        => new PostulacionesDbContextFactory().CreateDbContext([]);

    [Fact]
    public async Task Debe_RastrearLaEntidad_Cuando_SeAgregaUnPostulante()
    {
        await using var contexto = CrearContexto();
        var repositorio = new PostulanteRepository(contexto);
        var postulante = Postulante.Crear(
            personaId: null,
            usuarioId: Guid.NewGuid(),
            numeroDocumentoIdentidad: "V-12345678",
            nombres: "Ana",
            apellidos: "Pérez",
            datosContacto: new DatosContacto("ana@correo.com", "+584141234567"),
            universidadAliadaId: Guid.NewGuid(),
            carreraId: Guid.NewGuid(),
            periodoActualId: Guid.NewGuid(),
            condicionAcademica: CondicionAcademicaIngreso.Admitido,
            aceptaTratamientoDatos: true,
            fechaRegistro: DateTimeOffset.UtcNow);

        await repositorio.AgregarAsync(postulante);

        contexto.Entry(postulante).State.Should().Be(EntityState.Added);
    }

    [Fact]
    public async Task Debe_LanzarArgumentException_Cuando_LosParametrosDeConsultaSonInvalidos()
    {
        await using var contexto = CrearContexto();
        var repositorio = new PostulanteRepository(contexto);

        var porId = () => repositorio.ObtenerPorIdAsync(Guid.Empty);
        var porUsuario = () => repositorio.ObtenerPorUsuarioIdAsync(Guid.Empty);
        var paraActualizar = () => repositorio.ObtenerParaActualizarPorUsuarioIdAsync(Guid.Empty);
        var porDocumento = () => repositorio.ExistePorNumeroDocumentoAsync("   ");

        await porId.Should().ThrowAsync<ArgumentException>();
        await porUsuario.Should().ThrowAsync<ArgumentException>();
        await paraActualizar.Should().ThrowAsync<ArgumentException>();
        await porDocumento.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public void Debe_GenerarConsultasConFiltroPorIdUsuarioYDocumento_Cuando_SeInspeccionaElModelo()
    {
        using var contexto = CrearContexto();
        var id = Guid.NewGuid();
        var usuarioId = Guid.NewGuid();

        var sqlPorId = contexto.Postulantes
            .AsNoTracking()
            .Where(p => p.Id == id)
            .ToQueryString();

        var sqlPorUsuario = contexto.Postulantes
            .AsNoTracking()
            .Where(p => p.UsuarioId == usuarioId)
            .ToQueryString();

        var sqlPorDocumento = contexto.Postulantes
            .AsNoTracking()
            .Where(p => p.NumeroDocumentoIdentidad == "V-12345678")
            .ToQueryString();

        sqlPorId.Should().Contain("postulaciones.postulantes");
        sqlPorId.Should().Contain("WHERE");
        sqlPorUsuario.Should().Contain("usuario_id");
        sqlPorDocumento.Should().Contain("numero_documento_identidad");
    }

    [Fact]
    public void Debe_IncluirFamiliaresEnLasConsultasDeLecturaYActualizacion_Cuando_SeInspeccionanLasConsultas()
    {
        using var contexto = CrearContexto();
        var repositorio = new PostulanteRepository(contexto);
        var id = Guid.NewGuid();
        var usuarioId = Guid.NewGuid();

        var porId = repositorio.ConsultarPorId(id);
        var porUsuario = repositorio.ConsultarPorUsuarioId(usuarioId);
        var paraActualizar = repositorio.ConsultarParaActualizarPorUsuarioId(usuarioId);

        porId.ToQueryString().Should().Contain("miembros_familiares");
        porId.ToQueryString().Should().Contain("LEFT JOIN");
        porUsuario.ToQueryString().Should().Contain("miembros_familiares");
        porUsuario.ToQueryString().Should().Contain("LEFT JOIN");
        paraActualizar.ToQueryString().Should().Contain("miembros_familiares");
        paraActualizar.ToQueryString().Should().Contain("LEFT JOIN");

        porId.Expression.ToString().Should().Contain("AsNoTracking");
        porUsuario.Expression.ToString().Should().Contain("AsNoTracking");
        paraActualizar.Expression.ToString().Should().NotContain("AsNoTracking");
    }

    [Fact]
    public void Debe_RegistrarRepositorioYClientesHttp_Cuando_SeConfiguraInfraestructura()
    {
        var servicios = new ServiceCollection();
        var configuracion = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ServiciosExternos:Plataforma:UrlBase"] = "http://localhost:5001",
                ["ServiciosExternos:Programas:UrlBase"] = "http://localhost:5002",
                ["ServiciosExternos:TimeoutSegundos"] = "3"
            })
            .Build();

        servicios.AgregarInfraestructuraPostulaciones(
            PostulacionesDbContextFactory.CadenaConexionPorDefecto,
            configuracion);

        servicios.Should().Contain(d => d.ServiceType == typeof(IPostulanteRepository));
        servicios.Should().Contain(d => d.ServiceType == typeof(IUnitOfWork));
        servicios.Should().Contain(d => d.ServiceType == typeof(IPeriodoAcademicoClient));
        servicios.Should().Contain(d => d.ServiceType == typeof(ICatalogoAcademicoClient));
    }
}
