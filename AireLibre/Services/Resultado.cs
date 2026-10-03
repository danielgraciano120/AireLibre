/*
 * Resultado.cs — Lo que devuelve cualquier operación de los servicios.
 *
 * QUÉ ES: un par "¿salió bien?" + "qué le digo a la persona". Todos los
 * servicios del sistema devuelven esto, de modo que las vistas siempre tratan
 * las respuestas de la misma forma:
 *
 *     var resultado = Servicio.LoQueSea(...);
 *     if (resultado.Exito) { ... } else { mostrar resultado.Mensaje }
 *
 * POR QUÉ UN SOLO TIPO Y NO UNO POR SERVICIO: al principio cada servicio tenía
 * el suyo (ResultadoReserva, ResultadoSolicitud, ResultadoSesion) con los
 * mismos dos campos y el mismo comportamiento. Eran tres nombres para la misma
 * idea: más para leer, más para explicar y nada que ganar.
 *
 * POR QUÉ UN RECORD: es un dato inmutable de solo lectura, sin comportamiento
 * propio. Un record deja eso explícito y evita escribir el constructor y las
 * propiedades a mano.
 *
 * POR QUÉ LOS ATAJOS Ok() Y Error(): leer `Resultado.Error("...")` dice lo que
 * pasa mucho más rápido que `new Resultado(false, "...")`, donde hay que
 * acordarse de qué significa ese false suelto.
 */

namespace AireLibre.Services;

/// <param name="Exito">Verdadero si la operación se completó.</param>
/// <param name="Mensaje">Texto ya redactado para mostrarle a la persona.</param>
public record Resultado(bool Exito, string Mensaje)
{
    /// <summary>La operación salió bien.</summary>
    public static Resultado Ok(string mensaje) => new(true, mensaje);

    /// <summary>La operación no se pudo hacer, y este es el motivo.</summary>
    public static Resultado Error(string mensaje) => new(false, mensaje);
}
