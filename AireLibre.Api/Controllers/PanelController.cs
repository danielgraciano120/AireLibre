/*
 * PanelController.cs — Resumen del panel, notificaciones e inicio de sesión.
 *
 * SOBRE EL LOGIN: la API no guarda sesión (cada petición es independiente).
 * POST /api/sesion/login solo VALIDA las credenciales con SesionService y
 * devuelve el usuario sin la contraseña. Proteger los demás endpoints exigiría
 * tokens (JWT), que queda fuera de este trabajo.
 */

using AireLibre.Api.Contratos;
using AireLibre.Models;
using AireLibre.Services;
using Microsoft.AspNetCore.Mvc;

namespace AireLibre.Api.Controllers;

/// <summary>Resumen, notificaciones y validación de credenciales.</summary>
[Route("api")]
[Tags("7. Panel")]
public class PanelController : ControladorBase
{
    private readonly DatosService _datos;
    private readonly ClienteService _clientes;
    private readonly SesionService _sesion;

    public PanelController(DatosService datos, ClienteService clientes, SesionService sesion)
    {
        _datos = datos;
        _clientes = clientes;
        _sesion = sesion;
    }

    /// <summary>Los cuatro contadores del panel.</summary>
    [HttpGet("resumen")]
    [ProducesResponseType(typeof(ResumenRespuesta), StatusCodes.Status200OK)]
    public ActionResult<ResumenRespuesta> Resumen() =>
        Ok(new ResumenRespuesta(
            _datos.Reservas.Count(r => r.FechaClase == Hoy),
            _datos.Clientes.Count,
            _datos.Suscripciones.Count(s => !s.AlDia),
            _datos.Solicitudes.Count(s => s.Estado == EstadoSolicitud.Pendiente)));

    /// <summary>
    /// Notificaciones: las de cupo separado (guardadas) más los avisos de pago
    /// de mañana (calculados al vuelo, con Id = 0).
    /// </summary>
    /// <param name="soloNoLeidas">true para ver solo las no leídas.</param>
    [HttpGet("notificaciones")]
    [ProducesResponseType(typeof(IEnumerable<Notificacion>), StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<Notificacion>> Notificaciones([FromQuery] bool soloNoLeidas = false)
    {
        var todas = _datos.Notificaciones
            .Concat(_clientes.AvisosDePagoManana())
            .Where(n => !soloNoLeidas || !n.Leida)
            .OrderByDescending(n => n.FechaHora)
            .ToList();

        return Ok(todas);
    }

    /// <summary>Marca una notificación guardada como leída.</summary>
    /// <param name="id">Id de la notificación (los avisos de pago tienen Id 0 y no se marcan).</param>
    [HttpPut("notificaciones/{id:int}/leida")]
    [ProducesResponseType(typeof(RespuestaApi), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespuestaApi), StatusCodes.Status404NotFound)]
    public ActionResult<RespuestaApi> MarcarLeida(int id)
    {
        var notificacion = _datos.Notificaciones.FirstOrDefault(n => n.Id == id);
        if (notificacion is null) return NoEncontrado($"No existe la notificación {id}.");

        // Igual que en la vista: es cambiar un booleano, no hay regla que validar
        notificacion.Leida = true;
        return Ok(new RespuestaApi(true, "Notificación marcada como leída."));
    }

    /// <summary>Valida las credenciales de un profesor.</summary>
    /// <remarks>Usuarios de prueba: <c>profe</c> o <c>profesora</c>, contraseña <c>aire2026</c>.</remarks>
    /// <response code="200">Credenciales correctas.</response>
    /// <response code="401">Usuario o contraseña incorrectos.</response>
    [HttpPost("sesion/login")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(UsuarioRespuesta), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespuestaApi), StatusCodes.Status401Unauthorized)]
    public ActionResult<UsuarioRespuesta> Login([FromBody] LoginPeticion peticion)
    {
        var resultado = _sesion.IniciarSesion(peticion.NombreUsuario, peticion.Contrasena);
        if (!resultado.Exito || _sesion.UsuarioActual is null)
        {
            return Unauthorized(new RespuestaApi(false, resultado.Mensaje));
        }

        var u = _sesion.UsuarioActual;
        return Ok(new UsuarioRespuesta(u.Id, u.NombreUsuario, u.NombreMostrado));
    }
}
