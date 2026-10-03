/*
 * ControladorBase.cs — Lo que comparten todos los controladores.
 *
 * Los servicios devuelven siempre un Resultado (Exito + Mensaje). Aquí se
 * traduce a HTTP en un solo lugar: Exito -> 200, fallo -> 400 con el mensaje.
 * Así ningún controlador decide por su cuenta qué código devolver.
 */

using AireLibre.Api.Contratos;
using AireLibre.Services;
using Microsoft.AspNetCore.Mvc;

namespace AireLibre.Api.Controllers;

[ApiController]
[Produces("application/json")]
public abstract class ControladorBase : ControllerBase
{
    protected static DateOnly Hoy => DateOnly.FromDateTime(DateTime.Today);

    /// <summary>Exito -> 200 OK · fallo -> 400 Bad Request. Ambos con el mensaje del servicio.</summary>
    protected ActionResult<RespuestaApi> Responder(Resultado resultado)
    {
        var respuesta = new RespuestaApi(resultado.Exito, resultado.Mensaje);
        return resultado.Exito ? Ok(respuesta) : BadRequest(respuesta);
    }

    /// <summary>404 con el mismo formato de respuesta que el resto de la API.</summary>
    protected ActionResult NoEncontrado(string mensaje) =>
        NotFound(new RespuestaApi(false, mensaje));
}
