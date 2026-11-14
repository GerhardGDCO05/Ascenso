using Ascenso.Programas.Application.Universidades.DTOs;
using Ascenso.Programas.Domain.Enums;
using Ascenso.Programas.Domain.Universidades.Agregados;
using Ascenso.Programas.Domain.Universidades.Entidades;

namespace Ascenso.Programas.Application.Universidades;

internal static class UniversidadMapeador
{
    public static UniversidadResumenDto AResumenDto(this UniversidadAliada universidad)
        => new(
            universidad.Id,
            universidad.CodigoInstitucional,
            universidad.Nombre,
            universidad.Estado.ACodigo(),
            universidad.TieneConvenioVigente(),
            universidad.Carreras.Count,
            universidad.Version);

    public static UniversidadDetalleDto ADetalleDto(this UniversidadAliada universidad)
        => new(
            universidad.Id,
            universidad.CodigoInstitucional,
            universidad.Nombre,
            universidad.Direccion,
            new ContactoEnlaceDto(
                universidad.ContactoEnlace.Nombre,
                universidad.ContactoEnlace.Email,
                universidad.ContactoEnlace.Telefono),
            universidad.CuentaBancariaInstitucional is null
                ? null
                : new CuentaBancariaInstitucionalDto(
                    universidad.CuentaBancariaInstitucional.Banco,
                    universidad.CuentaBancariaInstitucional.TipoCuenta,
                    universidad.CuentaBancariaInstitucional.NumeroCuentaCifrado,
                    universidad.CuentaBancariaInstitucional.RifTitular),
            universidad.UsuarioEnlaceId,
            universidad.Estado.ACodigo(),
            universidad.TieneConvenioVigente(),
            universidad.Version,
            universidad.FechaCreacion,
            universidad.FechaUltimaModificacion,
            universidad.Convenios
                .OrderByDescending(c => c.FechaInicio)
                .Select(ADto)
                .ToList(),
            universidad.Carreras
                .OrderBy(c => c.CodigoCarrera, StringComparer.Ordinal)
                .Select(ADto)
                .ToList());

    public static ConvenioDto ADto(this Convenio convenio)
        => new(
            convenio.Id,
            convenio.UniversidadAliadaId,
            convenio.NumeroConvenio,
            convenio.TerminosResumen,
            convenio.FechaInicio,
            convenio.FechaFin,
            convenio.UrlDocumentoPdfFirebase,
            convenio.PermiteIntercambioAutomaticoDatos,
            convenio.Estado.ACodigo(),
            convenio.FechaCreacion);

    public static CarreraElegibleDto ADto(this CarreraElegible carrera)
        => new(
            carrera.Id,
            carrera.UniversidadAliadaId,
            carrera.CodigoCarrera,
            carrera.Nombre,
            carrera.DuracionSemestres,
            carrera.Activa);

    public static CostoMatriculaReferenciaDto ADto(this CostoMatriculaReferencia costo)
        => new(
            costo.Id,
            costo.CarreraElegibleId,
            costo.PeriodoAcademicoId,
            costo.NumeroVersion,
            costo.MontoReferenciaMatricula,
            costo.Moneda,
            costo.FechaVigenciaDesde,
            costo.FechaVigenciaHasta,
            costo.Estado.ACodigo());
}
