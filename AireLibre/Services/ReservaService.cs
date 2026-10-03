/*
 * ReservaService.cs — Reglas de negocio de la separación de cupos.
 *
 * QUÉ HACE: es el único lugar autorizado para crear una reserva. Se encarga de
 * contar los cupos, validar que no se pase del máximo y generar el aviso para
 * el profesor.
 *
 * CÓMO SE USA: las vistas lo reciben inyectado y llaman a SepararCupo(). Nunca
 * deben agregar una Reserva directamente a DatosService.Reservas, porque se
 * saltarían la validación.
 *
 *     var resultado = ReservaService.SepararCupo(horarioId, nombre, fecha);
 *     if (resultado.Exito) { ... } else { mostrar resultado.Mensaje }
 *
 * ------------------------------------------------------------------------
 * LA VALIDACIÓN DEL LÍMITE DE 20 CUPOS  (esto es lo importante del archivo)
 * ------------------------------------------------------------------------
 *
 * El enunciado exige que el tope de 20 personas se valide EN EL SERVICIO y no
 * solo en la pantalla. La razón es concreta: si la única defensa fuera el
 * botón deshabilitado de la vista, bastaría con abrir dos pestañas al mismo
 * tiempo para meter la reserva 21. La pantalla es una comodidad para el
 * usuario; la regla vive aquí.
 *
 * El método SepararCupo hace las tres cosas en este orden:
 *
 *   1. CUENTA las reservas que ya existen para ese horario y esa fecha.
 *   2. COMPARA ese número contra el CupoMaximo del horario (20).
 *   3. Solo si cabe, INSERTA la nueva reserva.
 *
 * Los tres pasos van DENTRO de un lock(_datos.Bloqueo). Sin el candado existe
 * la siguiente carrera: dos personas reservan en el mismo instante cuando
 * queda un solo cupo; las dos cuentan 19, las dos concluyen que cabe, y las
 * dos insertan. Resultado: 21 personas en una clase de 20. El candado obliga a
 * que la segunda espere a que la primera termine de insertar, y así la segunda
 * cuenta 20 y recibe el rechazo.
 *
 * Contar en el momento (y no llevar un campo "cuposOcupados" que se va sumando)
 * significa que el número nunca se puede desincronizar de la realidad: si se
 * borra una reserva, el cupo se libera solo.
 */

using AireLibre.Models;

namespace AireLibre.Services;

public class ReservaService
{
    private readonly DatosService _datos;

    // El almacén llega por inyección de dependencias (ver Program.cs).
    public ReservaService(DatosService datos)
    {
        _datos = datos;
    }

    /// <summary>
    /// Cuántas personas tienen cupo separado en ese horario para esa fecha.
    /// Se calcula contando las reservas existentes, no leyendo un contador.
    /// </summary>
    public int ContarOcupados(int horarioId, DateOnly fecha) =>
        _datos.Reservas.Count(r => r.HorarioId == horarioId && r.FechaClase == fecha);

    /// <summary>
    /// Cuántos cupos quedan libres. Nunca devuelve negativo, por si algún día
    /// se bajara el CupoMaximo de un horario que ya tiene más gente inscrita.
    /// </summary>
    public int CuposDisponibles(int horarioId, DateOnly fecha)
    {
        var horario = _datos.BuscarHorario(horarioId);
        if (horario is null) return 0;

        var libres = horario.CupoMaximo - ContarOcupados(horarioId, fecha);
        return libres < 0 ? 0 : libres;
    }

    /// <summary>
    /// Verdadero si todavía cabe alguien. La vista lo usa para deshabilitar el
    /// botón, pero NO es la validación real: la de verdad está en SepararCupo.
    /// </summary>
    public bool HayCupo(int horarioId, DateOnly fecha) => CuposDisponibles(horarioId, fecha) > 0;

    /// <summary>
    /// Separa un cupo para una persona en un horario y una fecha.
    /// Devuelve si se pudo y el mensaje que se le muestra a la persona.
    /// </summary>
    /// <param name="horarioId">Horario elegido.</param>
    /// <param name="nombreCompleto">Nombre de quien asiste, tal como lo escribió.</param>
    /// <param name="fecha">Día de la clase.</param>
    public Resultado SepararCupo(int horarioId, string nombreCompleto, DateOnly fecha)
    {
        // --- Validación del nombre, antes de tocar el almacén ---------------
        // Se quitan los espacios sobrantes de los extremos para que "  Ana  "
        // no se guarde con basura ni pase como nombre válido un puro espacio.
        nombreCompleto = (nombreCompleto ?? string.Empty).Trim();

        if (nombreCompleto.Length < 3)
        {
            return Resultado.Error("Escribe el nombre completo de quien va a asistir.");
        }

        // --- Zona crítica: contar, comparar e insertar sin interrupciones ---
        // Ver la explicación larga en el encabezado del archivo.
        lock (_datos.Bloqueo)
        {
            var horario = _datos.BuscarHorario(horarioId);
            if (horario is null)
            {
                return Resultado.Error("El horario que elegiste ya no está disponible.");
            }

            // 1. CONTAR las reservas que ya existen para ese horario y fecha.
            var ocupados = ContarOcupados(horarioId, fecha);

            // 2. COMPARAR contra el máximo del horario (20).
            if (ocupados >= horario.CupoMaximo)
            {
                return Resultado.Error(
                    $"La clase de las {horario.HoraTexto} ya completó los {horario.CupoMaximo} cupos. Elige otro horario.");
            }

            // Si quien reserva coincide con un cliente registrado, se guarda
            // también su id. Si no coincide, queda nulo: se puede reservar sin
            // estar registrado (ver Reserva.cs).
            var cliente = _datos.Clientes.FirstOrDefault(c =>
                string.Equals(c.NombreCompleto, nombreCompleto, StringComparison.OrdinalIgnoreCase));

            // 3. INSERTAR, ahora sí, con la certeza de que cabe.
            var reserva = new Reserva
            {
                Id = _datos.SiguienteIdReserva(),
                HorarioId = horarioId,
                NombreCompleto = nombreCompleto,
                ClienteId = cliente?.Id,
                FechaClase = fecha,
                FechaRegistro = DateTime.Now
            };
            _datos.Reservas.Add(reserva);

            // Al separar un cupo se genera un aviso para el profesor, que dice
            // quién reservó y para cuál horario. Va dentro del mismo candado
            // para que no pueda quedar una reserva sin su aviso.
            _datos.Notificaciones.Add(new Notificacion
            {
                Id = _datos.SiguienteIdNotificacion(),
                Tipo = TipoNotificacion.CupoSeparado,
                Mensaje = $"{nombreCompleto} separó un cupo para {_datos.DescribirHorario(horarioId)}.",
                FechaHora = reserva.FechaRegistro,
                Leida = false
            });

            var quedan = horario.CupoMaximo - (ocupados + 1);
            var aviso = quedan == 0
                ? "Con este quedó completa la clase."
                : $"Quedan {quedan} cupos.";

            return Resultado.Ok(
                $"Listo, {nombreCompleto}. Tu cupo quedó separado para {_datos.DescribirHorario(horarioId)}. {aviso}");
        }
    }
}
