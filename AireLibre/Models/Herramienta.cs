/*
 * Herramienta.cs — Un implemento del inventario.
 *
 * QUÉ ES: cada tipo de elemento con el que se entrena (pesas rusas,
 * colchonetas, barras, discos, lazos, cajones de salto). Se administra en
 * /panel/inventario con altas, cambios y bajas.
 *
 * IMPORTANTE: una Herramienta representa un TIPO, no una unidad. "Pesas rusas,
 * cantidad 12" es un solo registro, no doce. Por eso el estado es uno solo para
 * todo el lote: describe cómo está el conjunto. Si un lote se parte en dos
 * condiciones distintas, se crean dos registros.
 *
 * Como el grupo entrena al aire libre, todo se transporta y se desgasta rápido:
 * por eso el estado "Para reponer" se pinta en naranja de alerta.
 */

namespace AireLibre.Models;

public class Herramienta
{
    /// <summary>Identificador único dentro del almacén en memoria.</summary>
    public int Id { get; set; }

    /// <summary>Nombre del implemento, por ejemplo "Pesas rusas".</summary>
    public string Nombre { get; set; } = string.Empty;

    /// <summary>Cuántas unidades hay de este implemento.</summary>
    public int Cantidad { get; set; }

    /// <summary>Condición del lote: Bueno, Regular o Para reponer.</summary>
    public EstadoHerramienta Estado { get; set; } = EstadoHerramienta.Bueno;

    /// <summary>
    /// Devuelve una copia independiente de este registro, para que el
    /// formulario de edición trabaje sobre ella y no sobre el original.
    /// La razón completa está en Cliente.Copia().
    /// </summary>
    public Herramienta Copia() => new()
    {
        Id = Id,
        Nombre = Nombre,
        Cantidad = Cantidad,
        Estado = Estado
    };
}
