using Ascenso.Postulaciones.Domain.Comun;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Excepciones;
using Ascenso.Postulaciones.Domain.Postulantes.Entidades;
using Ascenso.Postulaciones.Domain.Postulantes.ObjetosValor;

namespace Ascenso.Postulaciones.Domain.Tests.Postulantes.Entidades;

public sealed class MiembroFamiliarTests
{
    private static readonly Guid _postulanteId = Guid.NewGuid();
    private static readonly Guid _usuarioId = Guid.NewGuid();
    private static readonly DateTimeOffset _fechaCreacion =
        new(2026, 11, 15, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Debe_CrearElMiembroFamiliar_Cuando_LosDatosSonValidos()
    {
        var datos = new DatosMiembroFamiliar(Parentesco.Madre, 45, Ocupacion.LaboresHogar, 0m);

        var miembro = MiembroFamiliar.Crear(_postulanteId, datos, _fechaCreacion, _usuarioId);

        Assert.NotEqual(Guid.Empty, miembro.Id);
        Assert.True(UuidV7.EsVersion7(miembro.Id));
        Assert.Equal(_postulanteId, miembro.PostulanteId);
        Assert.Equal(Parentesco.Madre, miembro.Parentesco);
        Assert.Equal(45, miembro.Edad);
        Assert.Equal(Ocupacion.LaboresHogar, miembro.Ocupacion);
        Assert.Equal(0m, miembro.IngresoMensual);
        Assert.Equal(_fechaCreacion, miembro.FechaCreacion);
        Assert.Equal(_usuarioId, miembro.UsuarioCreacionId);
        Assert.Equal(1, miembro.Version);
    }

    [Fact]
    public void Debe_RechazarElMiembroFamiliar_Cuando_NoTienePostulante()
    {
        var datos = new DatosMiembroFamiliar(Parentesco.Padre, 50, Ocupacion.Empleado, 1000m);

        var excepcion = Assert.Throws<MiembroFamiliarInvalidoException>(
            () => MiembroFamiliar.Crear(Guid.Empty, datos, _fechaCreacion, _usuarioId));

        Assert.Equal(MiembroFamiliarInvalidoException.CodigoError, excepcion.Codigo);
        Assert.Equal(400, excepcion.EstadoHttp);
    }

    [Fact]
    public void Debe_RechazarElMiembroFamiliar_Cuando_FaltanLosDatos()
    {
        var excepcion = Assert.Throws<MiembroFamiliarInvalidoException>(
            () => MiembroFamiliar.Crear(_postulanteId, null!, _fechaCreacion, _usuarioId));

        Assert.Equal(MiembroFamiliarInvalidoException.CodigoError, excepcion.Codigo);
        Assert.Equal(400, excepcion.EstadoHttp);
    }
}
