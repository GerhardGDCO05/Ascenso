using System.Security.Cryptography;
using Ascenso.Becas.Domain.Desembolsos.Agregados;
using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.Otorgamiento.ValueObjects;
using Ascenso.Becas.Infrastructure.Seguridad;
using Microsoft.EntityFrameworkCore;

namespace Ascenso.Becas.IntegrationTests.Persistencia;

[Collection(ColeccionPostgreSql.Nombre)]
public sealed class DatosPagoBecarioPersistenciaTests
{
    private readonly PostgreSqlFixture _fixture;

    public DatosPagoBecarioPersistenciaTests(PostgreSqlFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task ConservaHistorialYNuncaGuardaElNumeroDeCuentaEnClaro()
    {
        const string cuentaInicial = "01020000000000000001";
        const string cuentaNueva = "01020000000000000002";
        var cifrador = new CifradorCuentaBancariaAesGcm(
            Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));
        var becaId = Guid.NewGuid();
        var estudianteId = Guid.NewGuid();
        var otpInicial = Guid.NewGuid();
        var otpCambio = Guid.NewGuid();
        var contextoInicial = new ContextoOperacion(Guid.NewGuid(), Guid.NewGuid(),
            DateTimeOffset.UtcNow, autorizacionOtpId: otpInicial);
        var contextoCambio = new ContextoOperacion(Guid.NewGuid(), Guid.NewGuid(),
            DateTimeOffset.UtcNow, autorizacionOtpId: otpCambio);

        await using var contexto = _fixture.CrearContexto();
        var anterior = DatosPagoBecario.Crear(estudianteId, becaId, "Banco A", "0102",
            TipoCuentaBancaria.Ahorro, cuentaInicial, "V1234567", otpInicial,
            cifrador, contextoInicial);
        contexto.DatosPagoBecarios.Add(anterior);
        await contexto.SaveChangesAsync();

        await using (var transaccion = await contexto.Database.BeginTransactionAsync())
        {
            anterior.Reemplazar(contextoCambio);
            await contexto.SaveChangesAsync();
            var nuevo = DatosPagoBecario.Crear(estudianteId, becaId, "Banco B", "0103",
                TipoCuentaBancaria.Corriente, cuentaNueva, "V1234567", otpCambio,
                cifrador, contextoCambio);
            contexto.DatosPagoBecarios.Add(nuevo);
            await contexto.SaveChangesAsync();
            await transaccion.CommitAsync();
        }

        contexto.ChangeTracker.Clear();
        var cuentas = await contexto.DatosPagoBecarios
            .Where(item => item.BecaId == becaId)
            .OrderBy(item => item.FechaCreacion)
            .ToArrayAsync();
        Assert.Equal(2, cuentas.Length);
        Assert.Single(cuentas, item => item.Estado == EstadoDatosPago.Activo);
        Assert.Single(cuentas, item => item.Estado == EstadoDatosPago.Reemplazado);
        Assert.All(cuentas, item =>
        {
            Assert.StartsWith("v1:", item.NumeroCuentaCifradoAesGcm, StringComparison.Ordinal);
            Assert.DoesNotContain(cuentaInicial, item.NumeroCuentaCifradoAesGcm, StringComparison.Ordinal);
            Assert.DoesNotContain(cuentaNueva, item.NumeroCuentaCifradoAesGcm, StringComparison.Ordinal);
        });
        Assert.Equal(otpCambio,
            cuentas.Single(item => item.Estado == EstadoDatosPago.Reemplazado)
                .AutorizacionOtpModificacionId);

        var persistido = await contexto.Database.SqlQueryRaw<string>(
            "SELECT numero_cuenta_cifrado_aes_gcm AS \"Value\" FROM becas.datos_pago_becario WHERE beca_id = {0}",
            becaId).ToArrayAsync();
        Assert.Equal(2, persistido.Length);
        Assert.All(persistido, valor =>
        {
            Assert.DoesNotContain(cuentaInicial, valor, StringComparison.Ordinal);
            Assert.DoesNotContain(cuentaNueva, valor, StringComparison.Ordinal);
        });
    }
}
