using System.Security.Cryptography;
using System.Text;

namespace Ascenso.Becas.Application.Correlacion;

/// <summary>
/// Convierte el identificador textual de correlación, que puede no ser GUID,
/// en un UUID determinista apto para el contexto de dominio.
/// </summary>
public static class CorrelacionOperacion
{
    private static readonly Guid _respaldo =
        new("0190f0a0-0000-7000-8000-000000000002");

    public static Guid ADomain(string? correlacion)
    {
        if (Guid.TryParse(correlacion, out var guid) && guid != Guid.Empty)
        {
            return guid;
        }

        var valor = string.IsNullOrWhiteSpace(correlacion)
            ? "sin-correlacion"
            : correlacion.Trim();

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(valor));
        var dominio = new Guid(hash.AsSpan(0, 16));

        return dominio == Guid.Empty ? _respaldo : dominio;
    }
}
