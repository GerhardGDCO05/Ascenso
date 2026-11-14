using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Ascenso.Becas.Api.Seguridad;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Ascenso.Becas.IntegrationTests.Seguridad;

public sealed class AutenticacionJwtTests
{
    private const string Emisor = "https://keycloak.ascenso.test/realms/ascenso";
    private const string Audiencia = "ms-bec";
    private const string Clave =
        "clave-de-pruebas-con-mas-de-32-bytes-para-hs256-0123456789";

    [Fact]
    public void Debe_AceptarUnTokenValido_Cuando_EmisorAudienciaFirmaYVigenciaCoinciden()
    {
        var opciones = CrearOpciones();
        var token = CrearToken(opciones);

        var principal = CrearValidador().ValidateToken(
            token,
            AutenticacionJwt.CrearParametrosValidacion(opciones),
            out var tokenValidado);

        tokenValidado.Should().NotBeNull();
        principal.FindFirst("sub").Should().NotBeNull();
        principal.IsInRole("POSTULANTE").Should().BeTrue();
        principal.FindFirst("roles")!.Value.Should().Be("POSTULANTE");
    }

    [Fact]
    public void Debe_RechazarElToken_Cuando_LaFirmaNoCoincide()
    {
        var opciones = CrearOpciones();
        var token = CrearToken(opciones, clave: new string('x', 64));

        var accion = () => CrearValidador().ValidateToken(
            token,
            AutenticacionJwt.CrearParametrosValidacion(opciones),
            out _);

        accion.Should().Throw<SecurityTokenInvalidSignatureException>();
    }

    [Fact]
    public void Debe_RechazarElToken_Cuando_LaAudienciaNoCoincide()
    {
        var opciones = CrearOpciones();
        var token = CrearToken(opciones, audiencia: "otro-servicio");

        var accion = () => CrearValidador().ValidateToken(
            token,
            AutenticacionJwt.CrearParametrosValidacion(opciones),
            out _);

        accion.Should().Throw<SecurityTokenInvalidAudienceException>();
    }

    [Fact]
    public void Debe_RechazarElToken_Cuando_ElEmisorNoCoincide()
    {
        var opciones = CrearOpciones();
        var token = CrearToken(opciones, emisor: "https://otro-keycloak.test/realms/ascenso");

        var accion = () => CrearValidador().ValidateToken(
            token,
            AutenticacionJwt.CrearParametrosValidacion(opciones),
            out _);

        accion.Should().Throw<SecurityTokenInvalidIssuerException>();
    }

    [Fact]
    public void Debe_RechazarElToken_Cuando_EstaVencido()
    {
        var opciones = CrearOpciones();
        var token = CrearToken(opciones, expira: DateTime.UtcNow.AddMinutes(-5));

        var accion = () => CrearValidador().ValidateToken(
            token,
            AutenticacionJwt.CrearParametrosValidacion(opciones),
            out _);

        accion.Should().Throw<SecurityTokenExpiredException>();
    }

    [Fact]
    public void Debe_RechazarElToken_Cuando_NoTieneFechaDeExpiracion()
    {
        var opciones = CrearOpciones();
        var credenciales = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Clave)),
            SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: Emisor,
            audience: Audiencia,
            claims: [new Claim("sub", Guid.NewGuid().ToString())],
            notBefore: DateTime.UtcNow.AddMinutes(-1),
            expires: null,
            signingCredentials: credenciales);
        var tokenCodificado = new JwtSecurityTokenHandler().WriteToken(token);

        var accion = () => CrearValidador().ValidateToken(
            tokenCodificado,
            AutenticacionJwt.CrearParametrosValidacion(opciones),
            out _);

        accion.Should().Throw<SecurityTokenNoExpirationException>();
    }

    [Fact]
    public void Debe_ConfigurarMapInboundClaimsFalsoYRoleClaimTypeRoles()
    {
        var opciones = CrearOpciones();
        var bearer = new JwtBearerOptions();

        AutenticacionJwt.ConfigurarJwtBearer(bearer, opciones);

        bearer.MapInboundClaims.Should().BeFalse();
        bearer.SaveToken.Should().BeFalse();
        bearer.TokenValidationParameters.RoleClaimType.Should().Be("roles");
        bearer.TokenValidationParameters.NameClaimType.Should().Be("preferred_username");
        bearer.TokenValidationParameters.ValidateIssuer.Should().BeTrue();
        bearer.TokenValidationParameters.ValidateAudience.Should().BeTrue();
        bearer.TokenValidationParameters.ValidateIssuerSigningKey.Should().BeTrue();
        bearer.TokenValidationParameters.ValidateLifetime.Should().BeTrue();
        bearer.TokenValidationParameters.ValidIssuer.Should().Be(Emisor);
        bearer.TokenValidationParameters.ValidAudience.Should().Be(Audiencia);
    }

    [Fact]
    public void Debe_UsarElMetadataOidc_Cuando_NoHayClaveSimetrica()
    {
        var opciones = CrearOpciones();
        opciones.SigningKey = null;
        var bearer = new JwtBearerOptions();

        AutenticacionJwt.ConfigurarJwtBearer(bearer, opciones);

        bearer.Authority.Should().Be(Emisor);
        bearer.Audience.Should().Be(Audiencia);
        bearer.RequireHttpsMetadata.Should().BeFalse();
        bearer.TokenValidationParameters.IssuerSigningKey.Should().BeNull();
        bearer.TokenValidationParameters.ValidIssuer.Should().Be(Emisor);
    }

    private static JwtOptions CrearOpciones()
        => new()
        {
            Authority = Emisor,
            Issuer = Emisor,
            Audience = Audiencia,
            RequireHttpsMetadata = false,
            SigningKey = Clave,
            RoleClaimType = "roles",
            NameClaimType = "preferred_username"
        };

    private static JwtSecurityTokenHandler CrearValidador()
        => new()
        {
            MapInboundClaims = false
        };

    private static string CrearToken(
        JwtOptions opciones,
        string emisor = Emisor,
        string audiencia = Audiencia,
        DateTime? expira = null,
        string? clave = null)
    {
        var credenciales = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(clave ?? opciones.SigningKey!)),
            SecurityAlgorithms.HmacSha256);

        var expiracion = expira ?? DateTime.UtcNow.AddMinutes(5);
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = emisor,
            Audience = audiencia,
            Subject = new ClaimsIdentity(
            [
                new Claim("sub", Guid.NewGuid().ToString()),
                new Claim("roles", "POSTULANTE")
            ]),
            Expires = expiracion,
            NotBefore = expiracion.AddMinutes(-10),
            IssuedAt = expiracion.AddMinutes(-10),
            SigningCredentials = credenciales
        };

        return new JwtSecurityTokenHandler().CreateEncodedJwt(descriptor);
    }
}
