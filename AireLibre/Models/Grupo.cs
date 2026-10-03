/*
 * Grupo.cs — Un grupo de entrenamiento.
 *
 * QUÉ ES: la agrupación mayor de las clases. En el sistema existen dos grupos:
 * el de la mañana y el de la noche. Un grupo NO tiene hora propia: las horas
 * están en sus Horario (ver Horario.cs). El grupo de la mañana tiene un solo
 * horario (8:30 am) y el de la noche tiene dos (7:00 pm y 8:00 pm).
 *
 * POR QUÉ ESTÁ SEPARADO DE HORARIO: porque el grupo es lo que el cliente
 * reconoce ("entreno en el de la noche") y lo que da el color en pantalla,
 * mientras que el cupo de 20 personas se cuenta por horario, no por grupo.
 */

namespace AireLibre.Models;

public class Grupo
{
    /// <summary>Identificador único dentro del almacén en memoria.</summary>
    public int Id { get; set; }

    /// <summary>Nombre visible, por ejemplo "Grupo de la mañana".</summary>
    public string Nombre { get; set; } = string.Empty;

    /// <summary>
    /// Momento del día del grupo. Define también el color con el que el grupo
    /// aparece en todo el sitio (verde la mañana, azul noche la noche).
    /// </summary>
    public Jornada Jornada { get; set; }

    /// <summary>Frase corta que describe el grupo en el sitio público.</summary>
    public string Descripcion { get; set; } = string.Empty;
}
