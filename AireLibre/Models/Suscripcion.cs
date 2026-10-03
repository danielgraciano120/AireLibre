/*
 * Suscripcion.cs — El plan que un cliente tiene vigente y su estado de pago.
 *
 * QUÉ ES: la unión entre un cliente y un plan, con las fechas y la marca de si
 * está al día. Es lo que alimenta la vista /panel/pagos.
 *
 * ACLARACIÓN IMPORTANTE: el sitio NO cobra ni procesa dinero. AlDia es una
 * marca que el profesor pone y quita a mano cuando la persona le paga en
 * efectivo. No hay recibos, ni montos pagados, ni historial de transacciones.
 *
 * FechaProximoPago es además lo que dispara los avisos: el sistema notifica al
 * profesor sobre los clientes cuya fecha de pago cae MAÑANA, para que pueda
 * recordárselo con un día de anticipación (ver /panel/notificaciones).
 */

namespace AireLibre.Models;

public class Suscripcion
{
    /// <summary>Identificador único dentro del almacén en memoria.</summary>
    public int Id { get; set; }

    /// <summary>Cliente al que pertenece la suscripción (ver Cliente.cs).</summary>
    public int ClienteId { get; set; }

    /// <summary>Plan contratado (ver Plan.cs).</summary>
    public int PlanId { get; set; }

    /// <summary>Día en que empezó a regir el plan.</summary>
    public DateOnly FechaInicio { get; set; }

    /// <summary>
    /// Día en que le toca volver a pagar. Se calcula sumando los días de
    /// duración del plan a la fecha de inicio.
    /// </summary>
    public DateOnly FechaProximoPago { get; set; }

    /// <summary>
    /// Verdadero si el profesor lo marcó como al día; falso si está en mora.
    /// Es una marca manual, no el resultado de ninguna transacción.
    /// </summary>
    public bool AlDia { get; set; }
}
