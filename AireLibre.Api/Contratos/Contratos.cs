/*
 * Contratos.cs — Lo que la API recibe y devuelve (DTOs).
 *
 * POR QUÉ NO SE DEVUELVEN LOS MODELOS TAL CUAL en todos los casos:
 *   - Usuario trae la contraseña: nunca debe salir por la API.
 *   - Varias respuestas necesitan datos calculados que el modelo no guarda
 *     (cupos ocupados de un horario, nombre del plan de una suscripción).
 *   - Las peticiones no deben dejar que el cliente invente el Id ni las fechas
 *     de registro: eso lo asigna el servicio.
 */

using AireLibre.Models;

namespace AireLibre.Api.Contratos;

/// <summary>Respuesta estándar de toda operación que cambia datos.</summary>
/// <param name="Exito">Si la operación se hizo.</param>
/// <param name="Mensaje">Texto para mostrarle a la persona.</param>
public record RespuestaApi(bool Exito, string Mensaje);

// --- Grupos y horarios ------------------------------------------------------

/// <summary>Un horario con su ocupación del día consultado.</summary>
public record HorarioRespuesta(
    int Id,
    int GrupoId,
    string Grupo,
    Jornada Jornada,
    string Hora,
    int CupoMaximo,
    int Ocupados,
    int Disponibles,
    DateOnly Fecha);

/// <summary>Un grupo con sus horarios.</summary>
public record GrupoRespuesta(
    int Id,
    string Nombre,
    Jornada Jornada,
    string Descripcion,
    IReadOnlyList<HorarioRespuesta> Horarios);

// --- Reservas ---------------------------------------------------------------

/// <summary>Datos para separar un cupo.</summary>
public class SepararCupoPeticion
{
    /// <summary>Horario elegido.</summary>
    /// <example>3</example>
    public int HorarioId { get; set; }

    /// <summary>Nombre completo de quien asiste.</summary>
    /// <example>Laura Gómez</example>
    public string NombreCompleto { get; set; } = string.Empty;

    /// <summary>Día de la clase. Si se omite, se usa hoy.</summary>
    /// <example>null</example>
    public DateOnly? Fecha { get; set; }
}

/// <summary>Una reserva con la descripción del horario.</summary>
public record ReservaRespuesta(
    int Id,
    int HorarioId,
    string Horario,
    string NombreCompleto,
    int? ClienteId,
    DateOnly FechaClase,
    DateTime FechaRegistro);

// --- Solicitudes ------------------------------------------------------------

/// <summary>Datos que deja un interesado desde la página de planes.</summary>
public class SolicitudPeticion
{
    /// <example>Mateo Rendón</example>
    public string NombreCompleto { get; set; } = string.Empty;

    /// <example>Cedula</example>
    public TipoDocumento TipoDocumento { get; set; } = TipoDocumento.Cedula;

    /// <summary>Solo números, sin puntos ni espacios.</summary>
    /// <example>1036789123</example>
    public string NumeroIdentificacion { get; set; } = string.Empty;

    /// <example>300 555 1234</example>
    public string Telefono { get; set; } = string.Empty;

    /// <example>3</example>
    public int PlanId { get; set; }
}

/// <summary>Datos que faltan para convertir una solicitud en cliente.</summary>
public class ConvertirSolicitudPeticion
{
    /// <summary>Estatura en metros (por ejemplo 1.72, no 172).</summary>
    /// <example>1.72</example>
    public decimal EstaturaMetros { get; set; }

    /// <example>70.5</example>
    public decimal PesoKg { get; set; }

    /// <example>Definicion</example>
    public Objetivo Objetivo { get; set; } = Objetivo.SaludYVitalidad;
}

// --- Clientes ---------------------------------------------------------------

/// <summary>Datos para crear o actualizar un cliente.</summary>
public class ClientePeticion
{
    /// <example>Sara Montoya Gil</example>
    public string NombreCompleto { get; set; } = string.Empty;

    /// <example>Cedula</example>
    public TipoDocumento TipoDocumento { get; set; } = TipoDocumento.Cedula;

    /// <summary>Solo números, sin puntos ni espacios.</summary>
    /// <example>1152334455</example>
    public string NumeroIdentificacion { get; set; } = string.Empty;

    /// <example>311 222 3344</example>
    public string Telefono { get; set; } = string.Empty;

    /// <summary>Estatura en metros (por ejemplo 1.65, no 165).</summary>
    /// <example>1.65</example>
    public decimal EstaturaMetros { get; set; }

    /// <example>61.2</example>
    public decimal PesoKg { get; set; }

    /// <example>Tonificar</example>
    public Objetivo Objetivo { get; set; } = Objetivo.SaludYVitalidad;

    /// <example>3</example>
    public int PlanId { get; set; }

    internal Cliente ACliente(int id = 0) => new()
    {
        Id = id,
        NombreCompleto = NombreCompleto,
        TipoDocumento = TipoDocumento,
        NumeroIdentificacion = NumeroIdentificacion,
        Telefono = Telefono,
        EstaturaMetros = EstaturaMetros,
        PesoKg = PesoKg,
        Objetivo = Objetivo,
        PlanId = PlanId
    };
}

/// <summary>Respuesta al crear un cliente: el mensaje y el cliente con su Id asignado.</summary>
public record ClienteCreadoRespuesta(bool Exito, string Mensaje, Cliente Cliente);

/// <summary>Datos de una medición mensual.</summary>
public class ControlMensualPeticion
{
    /// <summary>Fecha de la medición. Si se omite, se usa hoy.</summary>
    /// <example>null</example>
    public DateOnly? Fecha { get; set; }

    /// <example>59.8</example>
    public decimal PesoKg { get; set; }

    /// <summary>Avance hacia el objetivo, de 0 a 100.</summary>
    /// <example>40</example>
    public int AvanceObjetivo { get; set; }

    /// <example>Bajó medidas de cintura.</example>
    public string Nota { get; set; } = string.Empty;
}

// --- Pagos ------------------------------------------------------------------

/// <summary>Estado de pago de un cliente.</summary>
public record PagoRespuesta(
    int ClienteId,
    string Cliente,
    int PlanId,
    string Plan,
    DateOnly FechaInicio,
    DateOnly FechaProximoPago,
    bool AlDia);

/// <summary>Marca un cliente como al día o en mora.</summary>
public class MarcarPagoPeticion
{
    /// <example>true</example>
    public bool AlDia { get; set; }
}

// --- Inventario -------------------------------------------------------------

/// <summary>Datos de una herramienta del inventario.</summary>
public class HerramientaPeticion
{
    /// <example>Kettlebell 16 kg</example>
    public string Nombre { get; set; } = string.Empty;

    /// <example>6</example>
    public int Cantidad { get; set; }

    /// <example>Bueno</example>
    public EstadoHerramienta Estado { get; set; } = EstadoHerramienta.Bueno;

    internal Herramienta AHerramienta(int id = 0) => new()
    {
        Id = id,
        Nombre = Nombre,
        Cantidad = Cantidad,
        Estado = Estado
    };
}

/// <summary>Totales del inventario.</summary>
public record ResumenInventarioRespuesta(int Herramientas, int Unidades, int ParaReponer);

// --- Sesión -----------------------------------------------------------------

/// <summary>Credenciales de un profesor.</summary>
public class LoginPeticion
{
    /// <example>profe</example>
    public string NombreUsuario { get; set; } = string.Empty;

    /// <example>aire2026</example>
    public string Contrasena { get; set; } = string.Empty;
}

/// <summary>Usuario autenticado, sin la contraseña.</summary>
public record UsuarioRespuesta(int Id, string NombreUsuario, string NombreMostrado);

// --- Resumen ----------------------------------------------------------------

/// <summary>Los contadores del panel.</summary>
public record ResumenRespuesta(
    int CuposSeparadosHoy,
    int ClientesRegistrados,
    int PagosPendientes,
    int SolicitudesSinAtender);
