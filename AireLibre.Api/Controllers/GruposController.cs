/*
 * GruposController.cs — Grupos (mañana / noche) y sus horarios con cupos.
 */

using AireLibre.Api.Contratos;
using AireLibre.Models;
using AireLibre.Services;
using Microsoft.AspNetCore.Mvc;

namespace AireLibre.Api.Controllers;

/// <summary>Grupos y horarios, con la ocupación de cada clase.</summary>
[Route("api/grupos")]
[Tags("1. Grupos y horarios")]
public class GruposController : ControladorBase
{
    private readonly DatosService _datos;
    private readonly ReservaService _reservas;

    public GruposController(DatosService datos, ReservaService reservas)
    {
        _datos = datos;
        _reservas = reservas;
    }

    /// <summary>Lista los grupos con sus horarios y los cupos libres del día.</summary>
    /// <param name="fecha">Día a consultar (formato 2026-10-03). Si se omite, hoy.</param>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<GrupoRespuesta>), StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<GrupoRespuesta>> Listar([FromQuery] DateOnly? fecha)
    {
        var dia = fecha ?? Hoy;

        var grupos = _datos.Grupos
            .OrderBy(g => g.Jornada)
            .Select(g => new GrupoRespuesta(
                g.Id,
                g.Nombre,
                g.Jornada,
                g.Descripcion,
                _datos.Horarios
                    .Where(h => h.GrupoId == g.Id)
                    .OrderBy(h => h.Hora)
                    .Select(h => AHorarioRespuesta(h, dia))
                    .ToList()))
            .ToList();

        return Ok(grupos);
    }

    /// <summary>Lista todos los horarios con su ocupación.</summary>
    /// <param name="fecha">Día a consultar. Si se omite, hoy.</param>
    [HttpGet("/api/horarios")]
    [ProducesResponseType(typeof(IEnumerable<HorarioRespuesta>), StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<HorarioRespuesta>> ListarHorarios([FromQuery] DateOnly? fecha)
    {
        var dia = fecha ?? Hoy;
        return Ok(_datos.Horarios.OrderBy(h => h.Hora).Select(h => AHorarioRespuesta(h, dia)).ToList());
    }

    /// <summary>Obtiene un horario con su ocupación.</summary>
    /// <param name="id">Id del horario.</param>
    /// <param name="fecha">Día a consultar. Si se omite, hoy.</param>
    [HttpGet("/api/horarios/{id:int}")]
    [ProducesResponseType(typeof(HorarioRespuesta), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespuestaApi), StatusCodes.Status404NotFound)]
    public ActionResult<HorarioRespuesta> ObtenerHorario(int id, [FromQuery] DateOnly? fecha)
    {
        var horario = _datos.BuscarHorario(id);
        if (horario is null) return NoEncontrado($"No existe el horario {id}.");

        return Ok(AHorarioRespuesta(horario, fecha ?? Hoy));
    }

    private HorarioRespuesta AHorarioRespuesta(Horario h, DateOnly dia)
    {
        var grupo = _datos.BuscarGrupo(h.GrupoId);
        return new HorarioRespuesta(
            h.Id,
            h.GrupoId,
            grupo?.Nombre ?? "—",
            grupo?.Jornada ?? Jornada.Manana,
            h.HoraTexto,
            h.CupoMaximo,
            _reservas.ContarOcupados(h.Id, dia),
            _reservas.CuposDisponibles(h.Id, dia),
            dia);
    }
}
