using Ascenso.Postulaciones.Domain.Excepciones;

namespace Ascenso.Postulaciones.Domain.Tests.Excepciones;

public sealed class ExcepcionesPostulanteTests
{
    [Fact]
    public void Debe_ExponerCodigoYEstatus_Cuando_SeCreanLasExcepcionesDeLaHu()
    {
        var casos = new (DomainException Excepcion, string Codigo, int EstadoHttp)[]
        {
            (new IdentidadDuplicadaException(), IdentidadDuplicadaException.CodigoError, 409),
            (new PerfilYaRegistradoException(), PerfilYaRegistradoException.CodigoError, 409),
            (new PostulanteNoEncontradoException(), PostulanteNoEncontradoException.CodigoError, 404),
            (new DatosPersonalesInvalidosException("Datos inválidos."), DatosPersonalesInvalidosException.CodigoError, 400),
            (new CondicionAcademicaInvalidaException(), CondicionAcademicaInvalidaException.CodigoError, 422),
            (new UniversidadNoElegibleException(), UniversidadNoElegibleException.CodigoError, 422),
            (new CarreraNoElegibleException(), CarreraNoElegibleException.CodigoError, 422),
            (new PeriodoAcademicoInvalidoException(), PeriodoAcademicoInvalidoException.CodigoError, 422),
            (new DependenciaNoDisponibleException(), DependenciaNoDisponibleException.CodigoError, 503),
            (new NoAutenticadoException(), NoAutenticadoException.CodigoError, 401),
            (new CatalogoInvalidoException("parentesco"), CatalogoInvalidoException.CodigoError, 422),
            (new DatosSocioeconomicosInvalidosException("Datos inválidos."), DatosSocioeconomicosInvalidosException.CodigoError, 400),
            (new MiembroFamiliarInvalidoException("Familiar inválido."), MiembroFamiliarInvalidoException.CodigoError, 400),
            (new CondicionLaboralIncoherenteException("Combinación inválida."), CondicionLaboralIncoherenteException.CodigoError, 422)
        };

        foreach (var (excepcion, codigo, estadoHttp) in casos)
        {
            Assert.Equal(codigo, excepcion.Codigo);
            Assert.Equal(estadoHttp, excepcion.EstadoHttp);
            Assert.False(string.IsNullOrWhiteSpace(excepcion.Message));
        }
    }

    [Fact]
    public void Debe_UsarUnMensajeEspecifico_Cuando_SeProporciona()
    {
        var excepcion = new IdentidadDuplicadaException("Documento repetido.");

        Assert.Equal("Documento repetido.", excepcion.Message);
        Assert.Equal("IDENTIDAD_DUPLICADA", excepcion.Codigo);
    }
}
