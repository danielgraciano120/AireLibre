/*
 * InventarioController.cs — CRUD de herramientas.
 */

using AireLibre.Api.Contratos;
using AireLibre.Models;
using AireLibre.Services;
using Microsoft.AspNetCore.Mvc;

namespace AireLibre.Api.Controllers;

/// <summary>Inventario de herramientas e implementos.</summary>
[Route("api/herramientas")]
[Tags("6. Inventario")]
public class InventarioController : ControladorBase
{
    private readonly DatosService _datos;
    private readonly InventarioService _inventario;

    public InventarioController(DatosService datos, InventarioService inventario)
    {
        _datos = datos;
        _inventario = inventario;
    }

    /// <summary>Lista el inventario en orden alfabético.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<Herramienta>), StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<Herramienta>> Listar() => Ok(_inventario.Listar());

    /// <summary>Totales: tipos de herramienta, unidades y cuántas hay que reponer.</summary>
    [HttpGet("resumen")]
    [ProducesResponseType(typeof(ResumenInventarioRespuesta), StatusCodes.Status200OK)]
    public ActionResult<ResumenInventarioRespuesta> Resumen() =>
        Ok(new ResumenInventarioRespuesta(
            _datos.Herramientas.Count,
            _inventario.TotalUnidades(),
            _inventario.ContarParaReponer()));

    /// <summary>Obtiene una herramienta.</summary>
    /// <param name="id">Id de la herramienta.</param>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(Herramienta), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespuestaApi), StatusCodes.Status404NotFound)]
    public ActionResult<Herramienta> Obtener(int id)
    {
        var herramienta = _datos.Herramientas.FirstOrDefault(h => h.Id == id);
        return herramienta is null ? NoEncontrado($"No existe la herramienta {id}.") : Ok(herramienta);
    }

    /// <summary>Agrega una herramienta. El nombre no se puede repetir.</summary>
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(RespuestaApi), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespuestaApi), StatusCodes.Status400BadRequest)]
    public ActionResult<RespuestaApi> Crear([FromBody] HerramientaPeticion peticion)
    {
        return Responder(_inventario.Crear(peticion.AHerramienta()));
    }

    /// <summary>Actualiza una herramienta.</summary>
    /// <param name="id">Id de la herramienta.</param>
    /// <param name="peticion">Datos nuevos.</param>
    [HttpPut("{id:int}")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(RespuestaApi), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespuestaApi), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RespuestaApi), StatusCodes.Status404NotFound)]
    public ActionResult<RespuestaApi> Actualizar(int id, [FromBody] HerramientaPeticion peticion)
    {
        if (_datos.Herramientas.All(h => h.Id != id)) return NoEncontrado($"No existe la herramienta {id}.");

        return Responder(_inventario.Actualizar(peticion.AHerramienta(id)));
    }

    /// <summary>Elimina una herramienta.</summary>
    /// <param name="id">Id de la herramienta.</param>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(RespuestaApi), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespuestaApi), StatusCodes.Status404NotFound)]
    public ActionResult<RespuestaApi> Eliminar(int id)
    {
        if (_datos.Herramientas.All(h => h.Id != id)) return NoEncontrado($"No existe la herramienta {id}.");

        return Responder(_inventario.Eliminar(id));
    }
}
