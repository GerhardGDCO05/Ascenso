using System.Reflection;
using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.Otorgamiento.Entidades;

namespace Ascenso.Becas.Domain.Tests.Otorgamiento;

public sealed class HistorialEstadoBecaTests
{
    private const string Justificacion = "Justificación suficiente para la operación.";

    [Fact]
    public void Debe_NoExponerSettersNiOperacionesDeModificacion()
    {
        var propiedades = typeof(HistorialEstadoBeca)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance);

        Assert.All(
            propiedades,
            propiedad => Assert.False(
                propiedad.SetMethod is not null && propiedad.SetMethod.IsPublic,
                $"La propiedad '{propiedad.Name}' expone un setter público."));

        var metodosPublicos = typeof(HistorialEstadoBeca)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(metodo => !metodo.IsSpecialName)
            .ToArray();

        Assert.Empty(metodosPublicos);
    }

    [Fact]
    public void Debe_RegistrarUnaFilaPorTransicion_IncluyendoLaCreacionYLaEmision()
    {
        var beca = PruebasOtorgamiento.CrearBecaConOferta();
        var aceptacion = PruebasOtorgamiento.Contexto(
            PruebasOtorgamiento.FechaBase.AddDays(1),
            "Aceptación de la oferta.");

        beca.AceptarOferta(beca.Oferta!.Id, aceptacion);

        Assert.Equal(4, beca.Historial.Count);
        Assert.Single(beca.Historial, registro => registro.EstadoBecaNuevo == EstadoBeca.Otorgada);
        Assert.Single(beca.Historial, registro => registro.EstadoOfertaNuevo == EstadoOferta.PendienteRespuesta);
        Assert.Single(beca.Historial, registro => registro.EstadoOfertaNuevo == EstadoOferta.Aceptada);
        Assert.Single(beca.Historial, registro => registro.EstadoBecaNuevo == EstadoBeca.Activa);
    }

    [Fact]
    public void Debe_RegistrarUnaFilaPorCadaTransicionPosterior()
    {
        var beca = PruebasOtorgamiento.CrearBecaActiva();
        var contador = beca.Historial.Count;

        beca.Suspender(PruebasOtorgamiento.Contexto(
            PruebasOtorgamiento.FechaBase.AddDays(5),
            Justificacion));
        Assert.Equal(contador + 1, beca.Historial.Count);

        beca.Reactivar(PruebasOtorgamiento.Contexto(
            PruebasOtorgamiento.FechaBase.AddDays(6),
            Justificacion));
        Assert.Equal(contador + 2, beca.Historial.Count);

        beca.Renovar(PruebasOtorgamiento.Contexto(
            PruebasOtorgamiento.FechaBase.AddDays(30),
            Justificacion));
        Assert.Equal(contador + 3, beca.Historial.Count);

        var ultimo = beca.Historial.Last();
        Assert.Equal(EstadoBeca.Renovada, ultimo.EstadoBecaNuevo);
        Assert.True(ultimo.EstadoTerminalIrreversible);
    }

    [Fact]
    public void Debe_RegistrarElActorLaJustificacionYLaFecha_Cuando_OcurreUnaTransicion()
    {
        var beca = PruebasOtorgamiento.CrearBecaActiva();
        var contexto = PruebasOtorgamiento.Contexto(
            PruebasOtorgamiento.FechaBase.AddDays(7),
            Justificacion,
            PruebasOtorgamiento.NuevoId());

        beca.Revocar(contexto);

        var registro = beca.Historial.Last();
        Assert.Equal(contexto.UsuarioAutorId, registro.UsuarioAutorId);
        Assert.Equal(contexto.CorrelacionId, registro.CorrelacionId);
        Assert.Equal(contexto.Fecha, registro.FechaCambio);
        Assert.Equal(contexto.Fecha, registro.FechaCreacion);
        Assert.Equal(Justificacion, registro.JustificacionDetallada); ;
        Assert.Equal(contexto.AutorizacionOtpId, registro.AutorizacionOtpId);
        Assert.True(registro.EstadoTerminalIrreversible);
    }
}
