/*
 * Plan.cs — Una modalidad de pago del entrenamiento.
 *
 * QUÉ ES: uno de los tres planes que ofrece el grupo (Diario, Quincenal,
 * Mensual). Se muestran en el sitio público (/planes) y se asignan a cada
 * cliente.
 *
 * SOBRE EL PRECIO: es solo informativo, para mostrarlo en la vista de planes.
 * El sitio NO realiza transacciones de dinero: no cobra, no integra pasarela de
 * pago y no lleva contabilidad. Lo único que existe sobre pagos es la marca
 * manual de "al día / en mora" que pone el profesor (ver Suscripcion.cs).
 *
 * DiasDuracion es lo que permite calcular la fecha del próximo pago al
 * renovar una suscripción.
 */

namespace AireLibre.Models;

public class Plan
{
    /// <summary>Identificador único dentro del almacén en memoria.</summary>
    public int Id { get; set; }

    /// <summary>Nombre visible: "Diario", "Quincenal" o "Mensual".</summary>
    public string Nombre { get; set; } = string.Empty;

    /// <summary>Cuántos días cubre el plan: 1, 15 o 30.</summary>
    public int DiasDuracion { get; set; }

    /// <summary>Valor de referencia en pesos. Solo se muestra; no se cobra.</summary>
    public decimal Precio { get; set; }

    /// <summary>Frase que explica para quién es el plan, en el sitio público.</summary>
    public string Descripcion { get; set; } = string.Empty;
}
