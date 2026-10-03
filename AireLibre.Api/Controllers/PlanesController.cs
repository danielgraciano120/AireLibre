/*
 * PlanesController.cs — Los planes (Diario, Quincenal, Mensual).
 */

using AireLibre.Api.Contratos;
using AireLibre.Models;
using AireLibre.Services;
using Microsoft.AspNetCore.Mvc;

namespace AireLibre.Api.Controllers;

/// <summary>Planes de pago.</summary>
[Route("api/planes")]
[Tags("3. Planes y solicitudes")]
public class PlanesController : ControladorBase
{
    private readonly DatosService _datos;

    public PlanesController(DatosService datos)
    {
        _datos = datos;
    }

    /// <summary>Lista los planes.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<Plan>), StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<Plan>> Listar() =>
        Ok(_datos.Planes.OrderBy(p => p.DiasDuracion).ToList());

    /// <summary>Obtiene un plan.</summary>
    /// <param name="id">Id del plan.</param>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(Plan), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespuestaApi), StatusCodes.Status404NotFound)]
    public ActionResult<Plan> Obtener(int id)
    {
        var plan = _datos.BuscarPlan(id);
        return plan is null ? NoEncontrado($"No existe el plan {id}.") : Ok(plan);
    }
}
