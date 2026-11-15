using Ascenso.Becas.Application.Excepciones;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Ascenso.Becas.Application.Despacho;

/// <summary>
/// Ejecuta los validadores FluentValidation registrados para una solicitud.
/// </summary>
internal static class ValidadorSolicitud
{
    public static async Task ValidarAsync<T>(
        IServiceProvider proveedor,
        T solicitud,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(proveedor);

        var validador = proveedor.GetService<IValidator<T>>();

        if (validador is null)
        {
            return;
        }

        var resultado = await validador.ValidateAsync(solicitud, cancellationToken);

        if (resultado.IsValid)
        {
            return;
        }

        var errores = resultado.Errors
            .GroupBy(error => string.IsNullOrWhiteSpace(error.PropertyName)
                ? "solicitud"
                : error.PropertyName)
            .ToDictionary(
                grupo => grupo.Key,
                grupo => grupo.Select(error => error.ErrorMessage).ToArray(),
                StringComparer.Ordinal);

        throw new ValidacionAplicacionException(errores);
    }
}
