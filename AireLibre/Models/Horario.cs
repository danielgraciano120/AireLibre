/*
 * Horario.cs — Una hora concreta de clase, con su cupo.
 *
 * QUÉ ES: el objeto sobre el que realmente se reserva. Hay tres en el sistema:
 * 8:30 am (grupo de la mañana), 7:00 pm y 8:00 pm (grupo de la noche).
 *
 * REGLA DE NEGOCIO IMPORTANTE: cada horario admite máximo 20 personas. Ese
 * límite vive aquí, en la propiedad CupoMaximo, para que sea un dato del
 * horario y no un número suelto repetido por el código. Quién hace cumplir el
 * límite es ReservaService: cuenta las reservas existentes ANTES de insertar
 * una nueva. Ver ReservaService.cs.
 *
 * NOTA SOBRE LA HORA: se guarda como TimeOnly (solo hora, sin fecha) porque la
 * clase se repite todos los días. Para mostrarla se usa HoraTexto, que arma el
 * formato "8:30 am" a mano en vez de depender de la cultura del servidor
 * (que en inglés imprimiría "8:30 AM" y en español "8:30 a. m.").
 */

namespace AireLibre.Models;

public class Horario
{
    /// <summary>Identificador único dentro del almacén en memoria.</summary>
    public int Id { get; set; }

    /// <summary>Grupo al que pertenece este horario (ver Grupo.cs).</summary>
    public int GrupoId { get; set; }

    /// <summary>Hora de inicio de la clase, sin fecha.</summary>
    public TimeOnly Hora { get; set; }

    /// <summary>
    /// Número máximo de personas admitidas en esta clase. Vale 20 por defecto,
    /// que es la regla del enunciado.
    /// </summary>
    public int CupoMaximo { get; set; } = 20;

    /// <summary>
    /// La hora escrita como la lee una persona: "8:30 am", "7:00 pm".
    /// Se calcula, no se guarda, así nunca queda desfasada de Hora.
    /// </summary>
    public string HoraTexto
    {
        get
        {
            // Paso de reloj de 24 horas a reloj de 12: las 0 y las 12 se
            // muestran como 12, el resto es el residuo de dividir entre 12.
            var hora12 = Hora.Hour % 12 == 0 ? 12 : Hora.Hour % 12;
            var sufijo = Hora.Hour < 12 ? "am" : "pm";
            return $"{hora12}:{Hora.Minute:00} {sufijo}";
        }
    }
}
