namespace Ascenso.Becas.Application.Otorgamiento.Puertos;

/// <summary>
/// Registra las emisiones bloqueadas por falta de configuración local de
/// <c>MS-PRG</c>. La etiqueta <c>recurso</c> distingue entre programa, costo
/// de matrícula y convenio para orientar la alerta operativa.
/// </summary>
public interface IMetricaConfiguracionFaltante
{
    public const string RecursoPrograma = "programa";

    public const string RecursoCostoMatricula = "costo_matricula";

    public const string RecursoConvenio = "convenio";

    void Registrar(string recurso);
}
