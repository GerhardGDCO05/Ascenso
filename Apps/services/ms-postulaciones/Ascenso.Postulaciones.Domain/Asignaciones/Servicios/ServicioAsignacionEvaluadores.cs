using Ascenso.Postulaciones.Domain.Asignaciones.Agregados;
using Ascenso.Postulaciones.Domain.Comites.Agregados;
using Ascenso.Postulaciones.Domain.Comites.Entidades;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Excepciones;

namespace Ascenso.Postulaciones.Domain.Asignaciones.Servicios;

/// <summary>
/// Servicio de dominio puro para asignar atómicamente una postulación a evaluadores activos
/// de un comité, cumpliendo el mínimo de evaluadores vigentes independientes (RN-POS-04)
/// y el balanceo determinista por carga abierta.
/// </summary>
public static class ServicioAsignacionEvaluadores
{
    public const int MinimoEvaluadoresVigentes = 2;

    public static IReadOnlyList<AsignacionEvaluacion> Asignar(
        ComiteEvaluador comite,
        Guid postulacionId,
        IReadOnlyCollection<Guid>? usuariosSolicitados,
        int? cantidad,
        IReadOnlyCollection<AsignacionEvaluacion> vigentes,
        IReadOnlyDictionary<Guid, int> cargaPorMiembro,
        DateTimeOffset ahora,
        DateTimeOffset fechaLimite,
        Guid usuarioAsignaId)
    {
        ArgumentNullException.ThrowIfNull(comite);
        ArgumentNullException.ThrowIfNull(vigentes);
        ArgumentNullException.ThrowIfNull(cargaPorMiembro);

        if (ahora == default)
        {
            throw new ArgumentException(
                "La fecha de asignación es obligatoria.",
                nameof(ahora));
        }

        if (usuarioAsignaId == Guid.Empty)
        {
            throw new ArgumentException(
                "El usuario que realiza la asignación es obligatorio.",
                nameof(usuarioAsignaId));
        }

        if (!comite.Activo)
        {
            throw new ComiteInactivoException();
        }

        if (postulacionId == Guid.Empty)
        {
            throw new AsignacionEvaluacionInvalidaException(
                "El identificador de la postulación es obligatorio.");
        }

        if (fechaLimite == default || fechaLimite <= ahora)
        {
            throw new AsignacionEvaluacionInvalidaException(
                "La fecha límite de evaluación debe ser posterior a la fecha de asignación.");
        }

        var tieneUsuarios = usuariosSolicitados is { Count: > 0 };
        var tieneCantidad = cantidad.HasValue;

        if (tieneUsuarios == tieneCantidad)
        {
            throw new AsignacionEvaluacionInvalidaException(
                "Debe indicar una lista de evaluadores o una cantidad para selección automática, pero no ambos.");
        }

        if (tieneCantidad && cantidad!.Value <= 0)
        {
            throw new AsignacionEvaluacionInvalidaException(
                "La cantidad de evaluadores a asignar debe ser mayor que cero.");
        }

        var asignacionesVigentes = vigentes
            .Where(asignacion => asignacion.Estado != EstadoAsignacionEvaluacion.ReasignadaPorConflicto)
            .ToList();

        if (asignacionesVigentes.Any(asignacion => asignacion.PostulacionId != postulacionId))
        {
            throw new AsignacionEvaluacionInvalidaException(
                "Las asignaciones vigentes proporcionadas no corresponden a la postulación indicada.");
        }

        if (asignacionesVigentes.Any(asignacion => asignacion.ComiteEvaluadorId != comite.Id))
        {
            throw new AsignacionEvaluacionInvalidaException(
                "La postulación ya tiene asignaciones vigentes en un comité evaluador diferente.");
        }

        var evaluadoresVigentes = asignacionesVigentes
            .Select(asignacion => asignacion.UsuarioEvaluadorId)
            .ToHashSet();

        var miembrosSeleccionados = tieneUsuarios
            ? SeleccionarManual(comite, usuariosSolicitados!, evaluadoresVigentes)
            : SeleccionarAutomatico(comite, cantidad!.Value, evaluadoresVigentes, cargaPorMiembro);

        if (asignacionesVigentes.Count + miembrosSeleccionados.Count < MinimoEvaluadoresVigentes)
        {
            throw new EvaluadoresInsuficientesException();
        }

        var nuevasAsignaciones = new List<AsignacionEvaluacion>(miembrosSeleccionados.Count);
        foreach (var miembro in miembrosSeleccionados)
        {
            nuevasAsignaciones.Add(AsignacionEvaluacion.Crear(
                comite,
                miembro.Id,
                postulacionId,
                ahora,
                fechaLimite,
                usuarioAsignaId));
        }

        return nuevasAsignaciones.AsReadOnly();
    }

    private static List<MiembroComite> SeleccionarManual(
        ComiteEvaluador comite,
        IReadOnlyCollection<Guid> usuariosSolicitados,
        HashSet<Guid> evaluadoresVigentes)
    {
        var miembrosActivosPorUsuario = comite.Miembros
            .Where(miembro => miembro.Activo)
            .ToDictionary(miembro => miembro.UsuarioEvaluadorId);

        var vistos = new HashSet<Guid>();
        var seleccionados = new List<MiembroComite>();

        foreach (var usuarioId in usuariosSolicitados)
        {
            if (usuarioId == Guid.Empty)
            {
                throw new AsignacionEvaluacionInvalidaException(
                    "Los identificadores de los evaluadores no pueden estar vacíos.");
            }

            if (!vistos.Add(usuarioId))
            {
                continue;
            }

            if (!miembrosActivosPorUsuario.TryGetValue(usuarioId, out var miembro))
            {
                throw new EvaluadorNoMiembroActivoException();
            }

            if (evaluadoresVigentes.Contains(usuarioId))
            {
                throw new AsignacionDuplicadaException();
            }

            seleccionados.Add(miembro);
        }

        return seleccionados;
    }

    private static List<MiembroComite> SeleccionarAutomatico(
        ComiteEvaluador comite,
        int cantidad,
        HashSet<Guid> evaluadoresVigentes,
        IReadOnlyDictionary<Guid, int> cargaPorMiembro)
    {
        var candidatos = comite.Miembros
            .Where(miembro => miembro.Activo && !evaluadoresVigentes.Contains(miembro.UsuarioEvaluadorId))
            .OrderBy(miembro => cargaPorMiembro.TryGetValue(miembro.Id, out var carga) ? carga : 0)
            .ThenBy(miembro => miembro.FechaAsignacion)
            .ThenBy(miembro => miembro.Id)
            .ToList();

        if (candidatos.Count < cantidad)
        {
            throw new EvaluadoresInsuficientesException(
                "No hay suficientes evaluadores activos elegibles en el comité para completar la asignación.");
        }

        return candidatos.Take(cantidad).ToList();
    }
}
