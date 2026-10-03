/*
 * SolicitudPlan.cs — Una petición de plan enviada desde el sitio público.
 *
 * QUÉ ES: lo que llega cuando alguien llena el formulario de /planes. NO es un
 * cliente todavía: es un interesado. El profesor la revisa en
 * /panel/solicitudes y decide si la convierte en cliente o la descarta.
 *
 * POR QUÉ NO SE CREA EL CLIENTE DE UNA VEZ: porque la ficha de cliente necesita
 * datos que el formulario público no pide (estatura, peso, objetivo) y porque
 * el profesor debe poder filtrar solicitudes que no van en serio. La solicitud
 * guarda solo los cuatro datos del formulario.
 *
 * El campo Estado deja el rastro de qué pasó con cada solicitud: una convertida
 * no se borra, se marca, para que quede el registro.
 */

namespace AireLibre.Models;

public class SolicitudPlan
{
    /// <summary>Identificador único dentro del almacén en memoria.</summary>
    public int Id { get; set; }

    public string NombreCompleto { get; set; } = string.Empty;

    public TipoDocumento TipoDocumento { get; set; } = TipoDocumento.Cedula;

    public string NumeroIdentificacion { get; set; } = string.Empty;

    public string Telefono { get; set; } = string.Empty;

    /// <summary>Plan que le interesa a la persona (ver Plan.cs).</summary>
    public int PlanId { get; set; }

    /// <summary>Momento en que se envió el formulario.</summary>
    public DateTime FechaSolicitud { get; set; } = DateTime.Now;

    /// <summary>
    /// En qué va la solicitud. Nace Pendiente; el profesor la pasa a Convertida
    /// o Descartada. Las Pendientes son las que cuenta el panel de resumen.
    /// </summary>
    public EstadoSolicitud Estado { get; set; } = EstadoSolicitud.Pendiente;
}
