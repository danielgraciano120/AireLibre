/*
 * Enumeraciones.cs — Listas cerradas de valores del dominio.
 *
 * QUÉ HACE: reúne en un solo archivo todos los conjuntos de valores fijos del
 * sistema (tipos de documento, objetivos, estados, jornadas). Se agrupan aquí
 * en vez de crear un archivo por enumeración porque son declaraciones de una
 * línea y verlas juntas ayuda a entender el vocabulario del dominio de un
 * vistazo.
 *
 * CÓMO SE USA: los modelos declaran propiedades de estos tipos. Para mostrarlos
 * en pantalla NUNCA se imprime el valor crudo (saldría "SaludYVitalidad"): se
 * llama al método de extensión .Texto(), que devuelve la versión legible en
 * español con tildes y espacios.
 *
 * NOTA: cada valor lleva un número explícito (= 1, = 2, ...). No es obligatorio
 * en C#, pero fija el valor numérico para que no cambie si alguien reordena la
 * lista. Importa el día que estos datos se guarden en base de datos.
 */

namespace AireLibre.Models;

/// <summary>Documento de identidad que presenta un cliente.</summary>
public enum TipoDocumento
{
    Cedula = 1,
    TarjetaIdentidad = 2
}

/// <summary>Meta que persigue el cliente con el entrenamiento.</summary>
public enum Objetivo
{
    Volumen = 1,
    Definicion = 2,
    Tonificar = 3,
    SaludYVitalidad = 4
}

/// <summary>Condición física en la que está una herramienta del inventario.</summary>
public enum EstadoHerramienta
{
    Bueno = 1,
    Regular = 2,
    ParaReponer = 3
}

/// <summary>
/// Momento del día en que entrena un grupo. Además de ser información del
/// dominio, define el color con el que el grupo se muestra en todo el sitio:
/// rojo para la mañana, grafito para la noche.
/// </summary>
public enum Jornada
{
    Manana = 1,
    Noche = 2
}

/// <summary>En qué va una solicitud de plan llegada desde el sitio público.</summary>
public enum EstadoSolicitud
{
    Pendiente = 1,
    Convertida = 2,
    Descartada = 3
}

/// <summary>Motivo por el que se generó un aviso para el profesor.</summary>
public enum TipoNotificacion
{
    CupoSeparado = 1,
    PagoPorVencer = 2
}

/// <summary>
/// Métodos de extensión para mostrar las enumeraciones en pantalla.
///
/// Se usan como si fueran métodos del propio valor:
///     <c>cliente.Objetivo.Texto()</c>  devuelve  "Salud y vitalidad"
///
/// Se resuelven con expresiones switch en vez de atributos y reflexión porque
/// así el código se lee de corrido y no hay nada oculto que explicar.
/// </summary>
public static class TextosDominio
{
    public static string Texto(this TipoDocumento valor) => valor switch
    {
        TipoDocumento.Cedula => "Cédula",
        TipoDocumento.TarjetaIdentidad => "Tarjeta de identidad",
        _ => valor.ToString()
    };

    /// <summary>Versión corta para tablas, donde el espacio es escaso.</summary>
    public static string TextoCorto(this TipoDocumento valor) => valor switch
    {
        TipoDocumento.Cedula => "C.C.",
        TipoDocumento.TarjetaIdentidad => "T.I.",
        _ => valor.ToString()
    };

    public static string Texto(this Objetivo valor) => valor switch
    {
        Objetivo.Volumen => "Volumen",
        Objetivo.Definicion => "Definición",
        Objetivo.Tonificar => "Tonificar",
        Objetivo.SaludYVitalidad => "Salud y vitalidad",
        _ => valor.ToString()
    };

    public static string Texto(this EstadoHerramienta valor) => valor switch
    {
        EstadoHerramienta.Bueno => "Bueno",
        EstadoHerramienta.Regular => "Regular",
        EstadoHerramienta.ParaReponer => "Para reponer",
        _ => valor.ToString()
    };

    public static string Texto(this Jornada valor) => valor switch
    {
        Jornada.Manana => "Mañana",
        Jornada.Noche => "Noche",
        _ => valor.ToString()
    };

    public static string Texto(this EstadoSolicitud valor) => valor switch
    {
        EstadoSolicitud.Pendiente => "Pendiente",
        EstadoSolicitud.Convertida => "Convertida en cliente",
        EstadoSolicitud.Descartada => "Descartada",
        _ => valor.ToString()
    };

    /// <summary>
    /// Sufijo de la clase CSS que colorea al grupo según su jornada.
    /// Devuelve "manana" o "noche", que se pegan a clases como
    /// <c>tarjeta--manana</c> o <c>tarjeta--noche</c> (ver app.css).
    /// </summary>
    public static string ClaseCss(this Jornada valor) =>
        valor == Jornada.Manana ? "manana" : "noche";

    /// <summary>
    /// Clase del distintivo con el que se pinta el estado de una herramienta.
    ///
    /// Los nombres son por SIGNIFICADO y no por color (--ok, --neutro,
    /// --alerta). Antes se llamaban por su color y, al cambiar la paleta del
    /// sitio a rojo, blanco y negro, quedaron mintiendo: una clase llamada
    /// "verde" pintando de rojo. Con nombres semánticos, cambiar la paleta no
    /// obliga a tocar este archivo.
    /// </summary>
    public static string ClaseDistintivo(this EstadoHerramienta valor) => valor switch
    {
        EstadoHerramienta.Bueno => "distintivo--ok",
        EstadoHerramienta.Regular => "distintivo--neutro",
        EstadoHerramienta.ParaReponer => "distintivo--alerta",
        _ => "distintivo--neutro"
    };
}
