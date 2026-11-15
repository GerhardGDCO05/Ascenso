using System.Security.Claims;
using System.Text.Json;
using Ascenso.Becas.Api.Seguridad;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Ascenso.Becas.IntegrationTests.Seguridad;

public sealed class AutorizacionRecursosTests
{
    [Fact]
    public async Task Debe_Autorizar_AlCoordinadorConAlcanceSobreElPrograma()
    {
        var programa = Guid.NewGuid();
        using var proveedor = CrearProveedor();

        var resultado = await AutorizarAsync(
            proveedor,
            CrearUsuario(programas: [programa]),
            new RecursoAutorizable(ProgramaId: programa),
            PoliticasSeguridad.AlcancePrograma);

        resultado.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Debe_Rechazar_AlCoordinadorSinAlcanceSobreElPrograma()
    {
        var programaDelToken = Guid.NewGuid();
        var programaDelRecurso = Guid.NewGuid();
        using var proveedor = CrearProveedor();

        var resultado = await AutorizarAsync(
            proveedor,
            CrearUsuario(programas: [programaDelToken]),
            new RecursoAutorizable(ProgramaId: programaDelRecurso),
            PoliticasSeguridad.AlcancePrograma);

        resultado.Succeeded.Should().BeFalse();
        resultado.Failure!.FailedRequirements.Should()
            .ContainSingle(requisito => requisito is AlcanceProgramaRequirement);
    }

    [Fact]
    public async Task Debe_Rechazar_AlCoordinadorSinClaimProgramaIds()
    {
        using var proveedor = CrearProveedor();

        var resultado = await AutorizarAsync(
            proveedor,
            CrearUsuario(),
            new RecursoAutorizable(ProgramaId: Guid.NewGuid()),
            PoliticasSeguridad.AlcancePrograma);

        resultado.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task Debe_Autorizar_ALaDireccionSinAlcanceExplicito()
    {
        using var proveedor = CrearProveedor();

        var resultado = await AutorizarAsync(
            proveedor,
            CrearUsuario(roles: ["DIRECCION"]),
            new RecursoAutorizable(ProgramaId: Guid.NewGuid()),
            PoliticasSeguridad.AlcancePrograma);

        resultado.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Debe_Rechazar_Cuando_ElRecursoNoIndicaPrograma()
    {
        using var proveedor = CrearProveedor();

        var resultado = await AutorizarAsync(
            proveedor,
            CrearUsuario(programas: [Guid.NewGuid()]),
            new RecursoAutorizable(),
            PoliticasSeguridad.AlcancePrograma);

        resultado.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task Debe_Responder403_Cuando_ElCoordinadorNoTieneAlcance()
    {
        using var proveedor = CrearProveedor();
        var autorizacion = await AutorizarAsync(
            proveedor,
            CrearUsuario(programas: [Guid.NewGuid()]),
            new RecursoAutorizable(ProgramaId: Guid.NewGuid()),
            PoliticasSeguridad.AlcancePrograma);

        var respuesta = await EjecutarManejadorAsync(autorizacion);

        respuesta.Estado.Should().Be(StatusCodes.Status403Forbidden);
        respuesta.Codigo.Should().Be("PERMISO_DENEGADO");
    }

    [Fact]
    public async Task Debe_Autorizar_AlPostulanteDuenoDelRecurso()
    {
        var postulante = Guid.NewGuid();
        using var proveedor = CrearProveedor();

        var resultado = await AutorizarAsync(
            proveedor,
            CrearUsuario(roles: ["POSTULANTE"], postulanteId: postulante),
            new RecursoAutorizable(PostulanteId: postulante),
            PoliticasSeguridad.PropiedadPostulante);

        resultado.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Debe_Rechazar_AlPostulanteQueNoEsDuenoDelRecurso()
    {
        using var proveedor = CrearProveedor();

        var resultado = await AutorizarAsync(
            proveedor,
            CrearUsuario(
                roles: ["POSTULANTE"],
                postulanteId: Guid.NewGuid()),
            new RecursoAutorizable(PostulanteId: Guid.NewGuid()),
            PoliticasSeguridad.PropiedadPostulante);

        resultado.Succeeded.Should().BeFalse();
        resultado.Failure!.FailedRequirements.Should()
            .ContainSingle(requisito => requisito is PropiedadPostulanteRequirement);
    }

    [Fact]
    public async Task Debe_Rechazar_Cuando_FaltaElClaimPostulanteId()
    {
        using var proveedor = CrearProveedor();

        var resultado = await AutorizarAsync(
            proveedor,
            CrearUsuario(roles: ["POSTULANTE"]),
            new RecursoAutorizable(PostulanteId: Guid.NewGuid()),
            PoliticasSeguridad.PropiedadPostulante);

        resultado.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task Debe_Rechazar_Cuando_ElRecursoNoIndicaPostulante()
    {
        using var proveedor = CrearProveedor();

        var resultado = await AutorizarAsync(
            proveedor,
            CrearUsuario(
                roles: ["POSTULANTE"],
                postulanteId: Guid.NewGuid()),
            new RecursoAutorizable(),
            PoliticasSeguridad.PropiedadPostulante);

        resultado.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task Debe_Responder403_Cuando_ElPostulanteOperaSobreUnRecursoAjeno()
    {
        using var proveedor = CrearProveedor();
        var autorizacion = await AutorizarAsync(
            proveedor,
            CrearUsuario(
                roles: ["POSTULANTE"],
                postulanteId: Guid.NewGuid()),
            new RecursoAutorizable(PostulanteId: Guid.NewGuid()),
            PoliticasSeguridad.PropiedadPostulante);

        var respuesta = await EjecutarManejadorAsync(autorizacion);

        respuesta.Estado.Should().Be(StatusCodes.Status403Forbidden);
        respuesta.Codigo.Should().Be("PERMISO_DENEGADO");
    }

    private static ServiceProvider CrearProveedor()
    {
        var servicios = new ServiceCollection();
        servicios.AddLogging();
        servicios.AgregarAutorizacion(new ConfigurationBuilder().Build());

        return servicios.BuildServiceProvider();
    }

    private static Task<AuthorizationResult> AutorizarAsync(
        ServiceProvider proveedor,
        ClaimsPrincipal usuario,
        object recurso,
        string politica)
        => proveedor
            .GetRequiredService<IAuthorizationService>()
            .AuthorizeAsync(usuario, recurso, politica);

    private static ClaimsPrincipal CrearUsuario(
        string[]? roles = null,
        Guid? postulanteId = null,
        Guid[]? programas = null)
    {
        var claims = new List<Claim>
        {
            new("sub", Guid.NewGuid().ToString())
        };

        foreach (var rol in roles ?? ["COORDINADOR_PROGRAMA"])
        {
            claims.Add(new Claim("roles", rol));
        }

        if (postulanteId is { } idPostulante)
        {
            claims.Add(new Claim("postulante_id", idPostulante.ToString()));
        }

        foreach (var programa in programas ?? [])
        {
            claims.Add(new Claim("programa_ids", programa.ToString()));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer"));
    }

    private static async Task<(int Estado, string Codigo)> EjecutarManejadorAsync(
        AuthorizationResult autorizacion)
    {
        autorizacion.Succeeded.Should().BeFalse();

        var contexto = new DefaultHttpContext();
        contexto.Request.Path = "/api/v1/becas/ofertas";
        contexto.Response.Body = new MemoryStream();

        await new AutorizacionResultadoHandler().HandleAsync(
            _ => Task.CompletedTask,
            contexto,
            new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build(),
            PolicyAuthorizationResult.Forbid(autorizacion.Failure));

        contexto.Response.Body.Position = 0;
        using var documento = await JsonDocument.ParseAsync(contexto.Response.Body);

        return (
            contexto.Response.StatusCode,
            documento.RootElement.GetProperty("code").GetString()!);
    }
}
