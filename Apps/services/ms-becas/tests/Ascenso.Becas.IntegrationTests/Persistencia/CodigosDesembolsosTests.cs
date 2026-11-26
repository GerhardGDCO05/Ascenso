using Ascenso.Becas.Domain.Desembolsos.Agregados;
using Ascenso.Becas.Domain.Desembolsos.Entidades;
using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ascenso.Becas.IntegrationTests.Persistencia;

public sealed class CodigosDesembolsosTests
{
    [Theory]
    [InlineData(typeof(CalendarioDesembolso), nameof(CalendarioDesembolso.Frecuencia), typeof(FrecuenciaDesembolso), "MENSUAL,SEMESTRAL")]
    [InlineData(typeof(CalendarioDesembolso), nameof(CalendarioDesembolso.DestinatarioPredeterminado), typeof(DestinatarioPago), "BECARIO,INSTITUCION")]
    [InlineData(typeof(CalendarioDesembolso), nameof(CalendarioDesembolso.EstadoPlan), typeof(EstadoPlanDesembolso), "PROGRAMADO,SUSPENDIDO,COMPLETADO,CANCELADO")]
    [InlineData(typeof(Desembolso), nameof(Desembolso.Destinatario), typeof(DestinatarioPago), "BECARIO,INSTITUCION")]
    [InlineData(typeof(Desembolso), nameof(Desembolso.Estado), typeof(EstadoDesembolso), "PROGRAMADO,EN_PROCESO,PAGADO_CONFIRMADO,FALLIDO,DEVUELTO,SUSPENDIDO,CANCELADO")]
    [InlineData(typeof(DatosPagoBecario), nameof(DatosPagoBecario.Estado), typeof(EstadoDatosPago), "ACTIVO,REEMPLAZADO")]
    [InlineData(typeof(IncidenciaDesembolso), nameof(IncidenciaDesembolso.Tipo), typeof(TipoIncidenciaDesembolso), "FALLO_BANCARIO,DEVOLUCION_BANCARIA")]
    [InlineData(typeof(IncidenciaDesembolso), nameof(IncidenciaDesembolso.Estado), typeof(EstadoIncidencia), "ABIERTA,RESUELTA")]
    public void PersistenciaConvierteEnumsALosCodigosDefinidos(Type entidad, string propiedad, Type tipoEnum, string codigos)
    {
        using var contexto = new BecasDbContextFactory().CreateDbContext([]);
        var columna = contexto.Model.FindEntityType(entidad)!.FindProperty(propiedad)!;
        Assert.Equal("varchar", columna.GetColumnType());
        var convertidor = columna.GetTypeMapping().Converter!;
        Assert.Equal(tipoEnum, convertidor.ModelClrType);
        var valores = Enum.GetValues(tipoEnum).Cast<object>()
            .Select(valor => convertidor.ConvertToProvider(valor))
            .Cast<string>();
        Assert.Equal(codigos.Split(','), valores);
    }

    [Fact]
    public void MetodoOpcionalConservaElCodigoLiteral()
    {
        using var contexto = new BecasDbContextFactory().CreateDbContext([]);
        var columna = contexto.Model.FindEntityType(typeof(Desembolso))!
            .FindProperty(nameof(Desembolso.MetodoPago))!;
        Assert.Equal("varchar", columna.GetColumnType());
        Assert.Equal("TRANSFERENCIA_BANCARIA", columna.GetTypeMapping().Converter!
            .ConvertToProvider(MetodoPago.TransferenciaBancaria));
    }
}
