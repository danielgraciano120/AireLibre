/*
 * ReservasController.cs — Separar cupo y consultar quién reservó.
 *
 * El límite de 20 lo valida ReservaService.SepararCupo (con candado). Este
 * controlador no cuenta nada: si se piden 21 reservas, la 21 sale con 400.
 */

using AireLibre.Api.Contratos;
using AireLibre.Services;
using Microsoft.AspNetCore.Mvc;

namespace AireLibre.Api.Controllers;

/// <summary>Reservas de cupo (máximo 20 por clase).</summary>
[Route("api/reservas")]
[Tags("2. Reservas")]
public class ReservasController : ControladorBase
{
    private readonly DatosService _datos;
    private readonly ReservaService _reservas;

    public ReservasController(DatosService datos, ReservaService reservas)
    {
        _datos = datos;
        _reservas = reservas;
    }

    /// <summary>Lista las reservas de un día, opcionalmente de un solo horario.</summary>
    /// <param name="fecha">Día de la clase. Si se omite, hoy.</param>
    /// <param name="horarioId">Filtra por horario.</param>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ReservaRespuesta>), StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<ReservaRespuesta>> Listar([FromQuery] DateOnly? fecha, [FromQuery] int? horarioId)
    {
        var dia = fecha ?? Hoy;

        var reservas = _datos.Reservas
            .Where(r => r.FechaClase == dia)
            .Where(r => horarioId is null || r.HorarioId == horarioId)
            .OrderBy(r => r.HorarioId)
            .ThenBy(r => r.FechaRegistro)
            .Select(r => new ReservaRespuesta(
                r.Id,
                r.HorarioId,
                _datos.DescribirHorario(r.HorarioId),
                r.NombreCompleto,
                r.ClienteId,
                r.FechaClase,
                r.FechaRegistro))
            .ToList();

        return Ok(reservas);
    }

    /// <summary>Separa un cupo en una clase.</summary>
    /// <remarks>
    /// Devuelve 400 si el nombre es muy corto, si el horario no existe o si la
    /// clase ya completó sus 20 cupos. El horario de las 8:00 pm arranca con
    /// 19 de 20 ocupados: sirve para probar el tope con dos peticiones.
    /// </remarks>
    /// <response code="200">Cupo separado.</response>
    /// <response code="400">No se pudo separar (ver el mensaje).</response>
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(RespuestaApi), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespuestaApi), StatusCodes.Status400BadRequest)]
    public ActionResult<RespuestaApi> SepararCupo([FromBody] SepararCupoPeticion peticion)
    {
        return Responder(_reservas.SepararCupo(peticion.HorarioId, peticion.NombreCompleto, peticion.Fecha ?? Hoy));
    }
}
