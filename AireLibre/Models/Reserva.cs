/*
 * Reserva.cs — Un cupo separado para una clase.
 *
 * QUÉ ES: el registro de que alguien apartó un puesto en un horario y una
 * fecha concretos.
 *
 * DECISIÓN DE DISEÑO CLAVE: quien reserva NO necesita estar registrado como
 * cliente. Por eso la reserva guarda el nombre completo como texto plano
 * (NombreCompleto) y la relación con un cliente es OPCIONAL (ClienteId es
 * int?, o sea, admite nulo).
 *   - Si reserva alguien de la calle -> NombreCompleto lleno, ClienteId nulo.
 *   - Si reserva un cliente registrado -> además se guarda su ClienteId.
 * Nunca se debe asumir que ClienteId tiene valor.
 *
 * POR QUÉ HAY FECHA Y HORARIO SEPARADOS: el horario dice a qué hora es la
 * clase (8:30 am), y FechaClase dice de qué día es ese cupo. Sin la fecha no se
 * podría responder "cuántos cupos se separaron hoy", que es uno de los
 * contadores del panel.
 */

namespace AireLibre.Models;

public class Reserva
{
    /// <summary>Identificador único dentro del almacén en memoria.</summary>
    public int Id { get; set; }

    /// <summary>Horario para el que se separó el cupo (ver Horario.cs).</summary>
    public int HorarioId { get; set; }

    /// <summary>
    /// Nombre de quien reserva, tal cual lo escribió. Es texto y no una
    /// referencia porque se reserva sin registrarse.
    /// </summary>
    public string NombreCompleto { get; set; } = string.Empty;

    /// <summary>
    /// Cliente registrado que reservó, si lo hay. Nulo cuando reserva alguien
    /// que no está en la base de clientes, que es el caso habitual.
    /// </summary>
    public int? ClienteId { get; set; }

    /// <summary>Día de la clase para la que se separó el cupo.</summary>
    public DateOnly FechaClase { get; set; }

    /// <summary>Momento exacto en que se hizo la reserva. Sirve para ordenarlas.</summary>
    public DateTime FechaRegistro { get; set; } = DateTime.Now;
}
