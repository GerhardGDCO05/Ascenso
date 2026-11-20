using Ascenso.Becas.Domain.Ports;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Ascenso.Becas.Infrastructure.Archivos;

/// <summary>
/// Registra el generador de cartas y el almacenamiento de archivos según el
/// proveedor configurado en <c>Storage:Provider</c>.
/// </summary>
public static class RegistroArchivos
{
    public static IServiceCollection AgregarArchivos(
        this IServiceCollection servicios,
        IConfiguration configuracion)
    {
        ArgumentNullException.ThrowIfNull(servicios);
        ArgumentNullException.ThrowIfNull(configuracion);

        var opciones = OpcionesAlmacenamiento.Leer(configuracion);

        servicios.AddSingleton(opciones);
        servicios.AddSingleton<ICartaOfertaPdfGenerator, CartaOfertaPdfGenerator>();
        servicios.AddSingleton<IPlantillaCompromisoPdfGenerator, PlantillaCompromisoPdfGenerator>();

        var proveedor = opciones.Proveedor.Trim();

        if (string.Equals(proveedor, ProveedoresAlmacenamiento.Local, StringComparison.OrdinalIgnoreCase))
        {
            servicios.AddSingleton<IAlmacenamientoArchivos, AlmacenamientoLocalArchivos>();
            return servicios;
        }

        if (string.Equals(proveedor, ProveedoresAlmacenamiento.Firebase, StringComparison.OrdinalIgnoreCase)
            || string.Equals(proveedor, "GoogleCloudStorage", StringComparison.OrdinalIgnoreCase)
            || string.Equals(proveedor, "Gcs", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(opciones.Firebase.Bucket))
            {
                throw new InvalidOperationException(
                    "El bucket de Firebase es obligatorio cuando el proveedor es Firebase.");
            }

            servicios.AddSingleton(_ => CrearClienteStorage(opciones.Firebase));
            servicios.AddSingleton<IAlmacenObjetosNube, AlmacenObjetosGoogleCloudStorage>();
            servicios.AddSingleton<IAlmacenamientoArchivos, AlmacenamientoFirebaseArchivos>();
            return servicios;
        }

        throw new InvalidOperationException(
            $"El proveedor de almacenamiento '{opciones.Proveedor}' no está soportado.");
    }

    private static StorageClient CrearClienteStorage(OpcionesAlmacenamientoFirebase opciones)
    {
        var credencial = string.IsNullOrWhiteSpace(opciones.RutaCredenciales)
            ? GoogleCredential.GetApplicationDefault()
            : CredentialFactory
                .FromFile<ServiceAccountCredential>(opciones.RutaCredenciales.Trim())
                .ToGoogleCredential();

        return StorageClient.Create(credencial);
    }
}
