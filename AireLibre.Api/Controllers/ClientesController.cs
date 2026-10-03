/*
 * ClientesController.cs — CRUD de clientes, control mensual y pagos.
 */

using AireLibre.Api.Contratos;
using AireLibre.Models;
using AireLibre.Services;
using Microsoft.AspNetCore.Mvc;

namespace AireLibre.Api.Controllers;

/// <summary>Clientes, su control mensual y su estado de pago.</summary>
[Route("api/clientes")]
[Tags("4. Clientes")]
public class ClientesController : ControladorBase
{
    private readonly DatosService _datos;
    private readonly ClienteService _clientes;

    public ClientesController(DatosService datos, ClienteService clientes)
    {
        _datos = datos;
        _clientes = clientes;
    }

    // --- CRUD -----------------------------------------------------------------

    /// <summary>Lista los clientes, con búsqueda por nombre o documento.</summary>
    /// <param name="buscar">Texto a buscar en el nombre o el número de documento.</param>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<Cliente>), StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<Cliente>> Listar([FromQuery] string? buscar)
    {
        var texto = (buscar ?? string.Empty).Trim();

        var clientes = _datos.Clientes
            .Where(c => texto.Length == 0 ||
                        c.NombreCompleto.Contains(texto, StringComparison.OrdinalIgnoreCase) ||
                        c.NumeroIdentificacion.Contains(texto))
            .OrderBy(c => c.NombreCompleto)
            .ToList();

        return Ok(clientes);
    }

    /// <summary>Obtiene un cliente.</summary>
    /// <param name="id">Id del cliente.</param>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(Cliente), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespuestaApi), StatusCodes.Status404NotFound)]
    public ActionResult<Cliente> Obtener(int id)
    {
        var cliente = _datos.BuscarCliente(id);
        return cliente is null ? NoEncontrado($"No existe el cliente {id}.") : Ok(cliente);
    }

    /// <summary>Registra un cliente. Se le abre la suscripción en mora automáticamente.</summary>
    /// <response code="201">Cliente creado.</response>
    /// <response code="400">Datos inválidos o documento repetido.</response>
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(ClienteCreadoRespuesta), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(RespuestaApi), StatusCodes.Status400BadRequest)]
    public ActionResult Crear([FromBody] ClientePeticion peticion)
    {
        var cliente = peticion.ACliente();
        var resultado = _clientes.CrearCliente(cliente);

        if (!resultado.Exito) return BadRequest(new RespuestaApi(false, resultado.Mensaje));

        return CreatedAtAction(nameof(Obtener), new { id = cliente.Id },
            new ClienteCreadoRespuesta(true, resultado.Mensaje, cliente));
    }

    /// <summary>Actualiza los datos de un cliente.</summary>
    /// <param name="id">Id del cliente.</param>
    /// <param name="peticion">Datos nuevos.</param>
    [HttpPut("{id:int}")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(RespuestaApi), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespuestaApi), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RespuestaApi), StatusCodes.Status404NotFound)]
    public ActionResult<RespuestaApi> Actualizar(int id, [FromBody] ClientePeticion peticion)
    {
        if (_datos.BuscarCliente(id) is null) return NoEncontrado($"No existe el cliente {id}.");

        return Responder(_clientes.ActualizarCliente(peticion.ACliente(id)));
    }

    /// <summary>Elimina un cliente. Borra su suscripción y controles, pero NO sus reservas.</summary>
    /// <param name="id">Id del cliente.</param>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(RespuestaApi), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespuestaApi), StatusCodes.Status404NotFound)]
    public ActionResult<RespuestaApi> Eliminar(int id)
    {
        if (_datos.BuscarCliente(id) is null) return NoEncontrado($"No existe el cliente {id}.");

        return Responder(_clientes.EliminarCliente(id));
    }

    // --- Control mensual ----------------------------------------------------

    /// <summary>Historial de controles mensuales, del más reciente al más antiguo.</summary>
    /// <param name="id">Id del cliente.</param>
    [HttpGet("{id:int}/controles")]
    [ProducesResponseType(typeof(IEnumerable<ControlMensual>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespuestaApi), StatusCodes.Status404NotFound)]
    public ActionResult<IEnumerable<ControlMensual>> Controles(int id)
    {
        if (_datos.BuscarCliente(id) is null) return NoEncontrado($"No existe el cliente {id}.");

        return Ok(_clientes.ControlesDe(id));
    }

    /// <summary>Registra una medición mensual.</summary>
    /// <param name="id">Id del cliente.</param>
    /// <param name="peticion">Peso, avance (0 a 100) y nota.</param>
    [HttpPost("{id:int}/controles")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(RespuestaApi), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespuestaApi), StatusCodes.Status400BadRequest)]
    public ActionResult<RespuestaApi> AgregarControl(int id, [FromBody] ControlMensualPeticion peticion)
    {
        return Responder(_clientes.AgregarControlMensual(
            id, peticion.Fecha ?? Hoy, peticion.PesoKg, peticion.AvanceObjetivo, peticion.Nota));
    }

    /// <summary>Elimina una medición mensual.</summary>
    /// <param name="controlId">Id del control.</param>
    [HttpDelete("controles/{controlId:int}")]
    [ProducesResponseType(typeof(RespuestaApi), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespuestaApi), StatusCodes.Status400BadRequest)]
    public ActionResult<RespuestaApi> EliminarControl(int controlId)
    {
        return Responder(_clientes.EliminarControlMensual(controlId));
    }

    // --- Pagos ----------------------------------------------------------------

    /// <summary>Estado de pago de todos los clientes, los pendientes primero.</summary>
    /// <param name="soloPendientes">true para ver solo los que están en mora.</param>
    [HttpGet("/api/pagos")]
    [Tags("5. Pagos")]
    [ProducesResponseType(typeof(IEnumerable<PagoRespuesta>), StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<PagoRespuesta>> Pagos([FromQuery] bool soloPendientes = false)
    {
        var pagos = _datos.Suscripciones
            .Where(s => !soloPendientes || !s.AlDia)
            .OrderBy(s => s.AlDia)
            .ThenBy(s => s.FechaProximoPago)
            .Select(s => new PagoRespuesta(
                s.ClienteId,
                _datos.BuscarCliente(s.ClienteId)?.NombreCompleto ?? "—",
                s.PlanId,
                _datos.NombrePlan(s.PlanId),
                s.FechaInicio,
                s.FechaProximoPago,
                s.AlDia))
            .ToList();

        return Ok(pagos);
    }

    /// <summary>Marca a un cliente como al día o en mora.</summary>
    /// <param name="id">Id del cliente.</param>
    /// <param name="peticion">alDia: true o false.</param>
    [HttpPut("{id:int}/pago")]
    [Tags("5. Pagos")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(RespuestaApi), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespuestaApi), StatusCodes.Status400BadRequest)]
    public ActionResult<RespuestaApi> MarcarPago(int id, [FromBody] MarcarPagoPeticion peticion)
    {
        return Responder(_clientes.MarcarPago(id, peticion.AlDia));
    }
}
