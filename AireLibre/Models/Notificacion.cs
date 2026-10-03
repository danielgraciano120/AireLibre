/*
 * Notificacion.cs — Un aviso dirigido a los profesores.
 *
 * QUÉ ES: el mensaje que aparece en /panel/notificaciones. Hay dos motivos por
 * los que nace una notificación:
 *
 *   1. CupoSeparado  — alguien acaba de separar un cupo. Se genera sola, en el
 *                      mismo momento en que se guarda la reserva, desde
 *                      ReservaService. Dice quién reservó y para cuál horario.
 *   2. PagoPorVencer — a un cliente le toca pagar mañana. NO se guarda: se
 *                      calcula al abrir la vista comparando la fecha de próximo
 *                      pago de cada suscripción contra la fecha de mañana.
 *
 * Por eso en el almacén solo se acumulan las del primer tipo. Las del segundo
 * se arman al vuelo y siempre reflejan el día en que se consultan, que es lo
 * correcto: un aviso de pago guardado ayer quedaría desactualizado hoy.
 *
 * El mensaje se guarda ya redactado (texto listo para mostrar) y no como
 * plantilla con datos sueltos, para que el aviso conserve lo que decía en su
 * momento aunque después cambien los datos que lo originaron.
 */

namespace AireLibre.Models;

public class Notificacion
{
    /// <summary>Identificador único dentro del almacén en memoria.</summary>
    public int Id { get; set; }

    /// <summary>Motivo del aviso: cupo separado o pago por vencer.</summary>
    public TipoNotificacion Tipo { get; set; }

    /// <summary>Texto ya redactado, listo para mostrar en pantalla.</summary>
    public string Mensaje { get; set; } = string.Empty;

    /// <summary>Momento en que se generó. Las más recientes van primero.</summary>
    public DateTime FechaHora { get; set; } = DateTime.Now;

    /// <summary>Verdadero cuando un profesor ya la marcó como vista.</summary>
    public bool Leida { get; set; }
}
