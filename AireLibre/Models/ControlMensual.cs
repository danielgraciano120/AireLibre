/*
 * ControlMensual.cs — Una medición mensual del progreso de un cliente.
 *
 * QUÉ ES: la foto del cliente en un mes: cuánto pesaba y qué tanto había
 * avanzado hacia su objetivo. Un cliente tiene muchos controles, uno por mes.
 * Es lo que se ve en /panel/clientes/{id}.
 *
 * POR QUÉ NO SE ACTUALIZA EL PESO DEL CLIENTE: si el peso solo viviera en
 * Cliente.PesoKg, cada medición nueva borraría la anterior y no se podría ver
 * la evolución, que es justamente el punto. Cliente.PesoKg guarda el peso con
 * el que la persona llegó; estos registros guardan el recorrido.
 *
 * AvanceObjetivo es un porcentaje de 0 a 100 que estima el profesor a ojo. No
 * se calcula solo: es la apreciación de quien dicta la clase.
 */

namespace AireLibre.Models;

public class ControlMensual
{
    /// <summary>Identificador único dentro del almacén en memoria.</summary>
    public int Id { get; set; }

    /// <summary>Cliente medido (ver Cliente.cs).</summary>
    public int ClienteId { get; set; }

    /// <summary>Día en que se tomó la medición.</summary>
    public DateOnly Fecha { get; set; }

    /// <summary>Peso en kilogramos en esa fecha.</summary>
    public decimal PesoKg { get; set; }

    /// <summary>Avance hacia el objetivo, de 0 a 100. Lo estima el profesor.</summary>
    public int AvanceObjetivo { get; set; }

    /// <summary>Observación libre del profesor sobre el mes.</summary>
    public string Nota { get; set; } = string.Empty;

    /// <summary>
    /// Nombre del mes y año para mostrar en la tabla de progreso, por ejemplo
    /// "junio 2026". Se arma con la cultura española para que no salga en
    /// inglés si el servidor está configurado en otro idioma.
    /// </summary>
    public string MesTexto =>
        Fecha.ToString("MMMM yyyy", new System.Globalization.CultureInfo("es-CO"));
}
