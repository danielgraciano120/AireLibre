/*
 * SolicitudesController.cs — Interesados que llegan desde /planes.
 */

using AireLibre.Api.Contratos;
using AireLibre.Models;
using AireLibre.Services;
using Microsoft.AspNetCore.Mvc;

namespace AireLibre.Api.Controllers;

/// <summary>Solicitudes de plan: registrar, convertir en cliente o descartar.</summary>
[Route("api/solicitudes")]
[Tags("3. Planes y solicitudes")]
public class SolicitudesController : ControladorBase
{
    private readonly DatosService _datos;
    private readonly ClienteService _clientes;

    public SolicitudesController(DatosService datos, ClienteService clientes)
    {
        _datos = datos;
        _clientes = clientes;
    }

    /// <summary>Lista las solicitudes, las pendientes primero.</summary>
    /// <param name="estado">Filtra por estado (Pendiente, Convertida, Descartada).</param>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<SolicitudPlan>), StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<SolicitudPlan>> Listar([FromQuery] EstadoSolicitud? estado)
    {
        var solicitudes = _datos.Solicitudes
            .Where(s => estado is null || s.Estado == estado)
            .OrderBy(s => s.Estado)
            .ThenByDescending(s => s.FechaSolicitud)
            .ToList();

        return Ok(solicitudes);
    }

    /// <summary>Registra la solicitud de un interesado.</summary>
    /// <response code="200">Solicitud recibida.</response>
    /// <response code="400">Datos inválidos, documento repetido o ya es cliente.</response>
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(RespuestaApi), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespuestaApi), StatusCodes.Status400BadRequest)]
    public ActionResult<RespuestaApi> Registrar([FromBody] SolicitudPeticion peticion)
    {
        return Responder(_clientes.RegistrarSolicitud(
            peticion.NombreCompleto,
            peticion.TipoDocumento,
            peticion.NumeroIdentificacion,
            peticion.Telefono,
            peticion.PlanId));
    }

    /// <summary>Convierte una solicitud pendiente en cliente.</summary>
    /// <param name="id">Id de la solicitud.</param>
    /// <param name="peticion">Los datos que el formulario público no pide.</param>
    [HttpPost("{id:int}/convertir")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(RespuestaApi), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespuestaApi), StatusCodes.Status400BadRequest)]
    public ActionResult<RespuestaApi> Convertir(int id, [FromBody] ConvertirSolicitudPeticion peticion)
    {
        return Responder(_clientes.ConvertirSolicitudEnCliente(
            id, peticion.EstaturaMetros, peticion.PesoKg, peticion.Objetivo));
    }

    /// <summary>Descarta una solicitud pendiente.</summary>
    /// <param name="id">Id de la solicitud.</param>
    [HttpPost("{id:int}/descartar")]
    [ProducesResponseType(typeof(RespuestaApi), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespuestaApi), StatusCodes.Status400BadRequest)]
    public ActionResult<RespuestaApi> Descartar(int id)
    {
        return Responder(_clientes.DescartarSolicitud(id));
    }
}
