/*
 * ClienteService.cs — Reglas de negocio de clientes y solicitudes de plan.
 *
 * QUÉ HACE: concentra todo lo que tiene que ver con las personas del sistema.
 * Está organizado en cinco bloques, en este orden:
 *
 *   1. SOLICITUDES  — las que llegan del sitio público y su conversión en cliente.
 *   2. CLIENTES     — crear, actualizar y eliminar (el CRUD de /panel/clientes).
 *   3. PAGOS        — marcar quién está al día (/panel/pagos).
 *   4. AVISOS       — los pagos que vencen mañana (/panel/notificaciones).
 *   5. VALIDACIÓN   — comprobaciones compartidas por los bloques anteriores.
 *
 * CÓMO SE USA: las vistas lo reciben inyectado y nunca escriben directamente en
 * las listas de DatosService, para no saltarse las validaciones.
 *
 * REGLA TRANSVERSAL: el número de identificación es único entre clientes. Es lo
 * que evita registrar dos veces a la misma persona, y se comprueba en el
 * momento de guardar, dentro del candado (ver ValidarDocumentoLibre).
 */

using AireLibre.Models;

namespace AireLibre.Services;

public class ClienteService
{
    private readonly DatosService _datos;

    public ClienteService(DatosService datos)
    {
        _datos = datos;
    }

    /* =====================================================================
     * 1. SOLICITUDES
     * =================================================================== */

    /// <summary>
    /// Registra una solicitud de plan enviada desde el formulario público.
    ///
    /// OJO: esto NO crea un cliente. Crea un interesado, que el profesor
    /// revisa después en /panel/solicitudes y decide si convierte o descarta
    /// (ver SolicitudPlan.cs).
    /// </summary>
    public Resultado RegistrarSolicitud(string nombreCompleto, TipoDocumento tipoDocumento,
        string numeroIdentificacion, string telefono, int planId)
    {
        // --- Limpieza de la entrada -----------------------------------------
        nombreCompleto = (nombreCompleto ?? string.Empty).Trim();
        numeroIdentificacion = (numeroIdentificacion ?? string.Empty).Trim();
        telefono = (telefono ?? string.Empty).Trim();

        // --- Validaciones, una por una y con mensaje propio ------------------
        // Cada mensaje dice qué falta y qué hacer, nunca "dato inválido".
        if (nombreCompleto.Length < 3)
        {
            return Resultado.Error("Escribe tu nombre completo.");
        }

        if (numeroIdentificacion.Length < 5)
        {
            return Resultado.Error("Escribe tu número de identificación.");
        }

        // Solo dígitos: el número de documento no lleva puntos ni letras.
        if (!numeroIdentificacion.All(char.IsDigit))
        {
            return Resultado.Error("El número de identificación debe tener solo números, sin puntos ni espacios.");
        }

        if (telefono.Length < 7)
        {
            return Resultado.Error("Escribe un teléfono de contacto.");
        }

        var plan = _datos.BuscarPlan(planId);
        if (plan is null)
        {
            return Resultado.Error("Elige uno de los tres planes.");
        }

        // --- Zona crítica: revisar duplicados e insertar ---------------------
        // Va dentro del candado por la misma razón que las reservas: dos envíos
        // simultáneos podrían pasar los dos la revisión de duplicado.
        lock (_datos.Bloqueo)
        {
            // El número de identificación evita duplicados (regla del
            // enunciado). Si ya hay una solicitud PENDIENTE con ese documento,
            // no se crea otra: sería la misma persona insistiendo, y el
            // profesor terminaría con la misma solicitud repetida.
            var yaSolicito = _datos.Solicitudes.Any(s =>
                s.NumeroIdentificacion == numeroIdentificacion &&
                s.Estado == EstadoSolicitud.Pendiente);

            if (yaSolicito)
            {
                return Resultado.Error("Ya tenemos una solicitud tuya sin atender. El profesor se comunica contigo pronto.");
            }

            // Si el documento ya corresponde a un cliente registrado, tampoco
            // hace falta una solicitud: la persona ya está adentro.
            var yaEsCliente = _datos.Clientes.Any(c => c.NumeroIdentificacion == numeroIdentificacion);
            if (yaEsCliente)
            {
                return Resultado.Error("Ese documento ya está registrado como cliente. Habla con el profesor para cambiar de plan.");
            }

            _datos.Solicitudes.Add(new SolicitudPlan
            {
                Id = _datos.SiguienteIdSolicitud(),
                NombreCompleto = nombreCompleto,
                TipoDocumento = tipoDocumento,
                NumeroIdentificacion = numeroIdentificacion,
                Telefono = telefono,
                PlanId = planId,
                FechaSolicitud = DateTime.Now,
                Estado = EstadoSolicitud.Pendiente
            });
        }

        return Resultado.Ok($"Recibimos tu solicitud del plan {plan.Nombre}, {nombreCompleto}. El profesor te contacta al {telefono}.");
    }

    /// <summary>
    /// Convierte una solicitud pendiente en un cliente registrado.
    ///
    /// La solicitud trae solo cuatro datos (nombre, documento, teléfono y
    /// plan); la ficha de cliente necesita además estatura, peso y objetivo,
    /// que el profesor toma en la primera clase. Por eso esos tres llegan como
    /// parámetros y no salen de la solicitud.
    ///
    /// La solicitud NO se borra: se marca como Convertida, para que quede el
    /// rastro de por dónde entró cada cliente.
    /// </summary>
    public Resultado ConvertirSolicitudEnCliente(int solicitudId, decimal estaturaMetros,
        decimal pesoKg, Objetivo objetivo)
    {
        lock (_datos.Bloqueo)
        {
            var solicitud = _datos.Solicitudes.FirstOrDefault(s => s.Id == solicitudId);
            if (solicitud is null)
            {
                return Resultado.Error("Esa solicitud ya no existe.");
            }

            if (solicitud.Estado != EstadoSolicitud.Pendiente)
            {
                return Resultado.Error($"Esa solicitud ya fue atendida ({solicitud.Estado.Texto().ToLower()}).");
            }

            var cliente = new Cliente
            {
                NombreCompleto = solicitud.NombreCompleto,
                TipoDocumento = solicitud.TipoDocumento,
                NumeroIdentificacion = solicitud.NumeroIdentificacion,
                Telefono = solicitud.Telefono,
                EstaturaMetros = estaturaMetros,
                PesoKg = pesoKg,
                Objetivo = objetivo,
                PlanId = solicitud.PlanId
            };

            // Se reutiliza el alta normal para no duplicar las validaciones.
            // Como ya estamos dentro del candado y lock es reentrante en el
            // mismo hilo, volver a tomarlo adentro no bloquea.
            var resultado = CrearCliente(cliente);
            if (!resultado.Exito)
            {
                return resultado;
            }

            solicitud.Estado = EstadoSolicitud.Convertida;
            return Resultado.Ok($"{cliente.NombreCompleto} quedó registrado como cliente.");
        }
    }

    /// <summary>Marca una solicitud como descartada. Tampoco se borra: queda el rastro.</summary>
    public Resultado DescartarSolicitud(int solicitudId)
    {
        lock (_datos.Bloqueo)
        {
            var solicitud = _datos.Solicitudes.FirstOrDefault(s => s.Id == solicitudId);
            if (solicitud is null)
            {
                return Resultado.Error("Esa solicitud ya no existe.");
            }

            if (solicitud.Estado != EstadoSolicitud.Pendiente)
            {
                return Resultado.Error("Esa solicitud ya fue atendida.");
            }

            solicitud.Estado = EstadoSolicitud.Descartada;
            return Resultado.Ok($"Se descartó la solicitud de {solicitud.NombreCompleto}.");
        }
    }

    /* =====================================================================
     * 2. CLIENTES  (el CRUD de /panel/clientes)
     * =================================================================== */

    /// <summary>
    /// Registra un cliente nuevo y le abre su suscripción.
    ///
    /// La suscripción se crea aquí y no aparte porque un cliente sin
    /// suscripción no aparecería en la vista de pagos: quedaría invisible para
    /// el profesor. Nace en mora (AlDia = false), que es lo correcto: todavía
    /// no ha pagado nada.
    /// </summary>
    public Resultado CrearCliente(Cliente cliente)
    {
        LimpiarTextos(cliente);

        var validacion = ValidarDatosCliente(cliente);
        if (!validacion.Exito) return validacion;

        lock (_datos.Bloqueo)
        {
            var documentoLibre = ValidarDocumentoLibre(cliente.NumeroIdentificacion, clienteId: null);
            if (!documentoLibre.Exito) return documentoLibre;

            var plan = _datos.BuscarPlan(cliente.PlanId)!;   // ya validado arriba
            var hoy = DateOnly.FromDateTime(DateTime.Today);

            cliente.Id = _datos.SiguienteIdCliente();
            cliente.FechaRegistro = hoy;
            _datos.Clientes.Add(cliente);

            _datos.Suscripciones.Add(new Suscripcion
            {
                Id = _datos.SiguienteIdSuscripcion(),
                ClienteId = cliente.Id,
                PlanId = cliente.PlanId,
                FechaInicio = hoy,
                FechaProximoPago = hoy.AddDays(plan.DiasDuracion),
                AlDia = false
            });

            return Resultado.Ok($"{cliente.NombreCompleto} quedó registrado con el plan {plan.Nombre}.");
        }
    }

    /// <summary>
    /// Guarda los cambios de un cliente que ya existe.
    ///
    /// Recibe una COPIA de la ficha (la que se editó en el formulario) y pasa
    /// los valores al objeto guardado. Así, si la persona cancela a mitad de
    /// camino, la ficha original no quedó tocada.
    ///
    /// Si cambió de plan, la suscripción se actualiza también: dejarla con el
    /// plan viejo haría que la vista de pagos mostrara una cosa y la ficha otra.
    /// </summary>
    public Resultado ActualizarCliente(Cliente editado)
    {
        LimpiarTextos(editado);

        var validacion = ValidarDatosCliente(editado);
        if (!validacion.Exito) return validacion;

        lock (_datos.Bloqueo)
        {
            var guardado = _datos.BuscarCliente(editado.Id);
            if (guardado is null)
            {
                return Resultado.Error("Ese cliente ya no existe.");
            }

            // Se excluye al propio cliente de la revisión de duplicados: si no,
            // no podría guardarse a sí mismo sin cambiar el documento.
            var documentoLibre = ValidarDocumentoLibre(editado.NumeroIdentificacion, guardado.Id);
            if (!documentoLibre.Exito) return documentoLibre;

            guardado.NombreCompleto = editado.NombreCompleto;
            guardado.TipoDocumento = editado.TipoDocumento;
            guardado.NumeroIdentificacion = editado.NumeroIdentificacion;
            guardado.Telefono = editado.Telefono;
            guardado.EstaturaMetros = editado.EstaturaMetros;
            guardado.PesoKg = editado.PesoKg;
            guardado.Objetivo = editado.Objetivo;
            guardado.PlanId = editado.PlanId;

            var suscripcion = _datos.BuscarSuscripcionDeCliente(guardado.Id);
            if (suscripcion is not null)
            {
                suscripcion.PlanId = editado.PlanId;
            }

            return Resultado.Ok($"Se actualizaron los datos de {guardado.NombreCompleto}.");
        }
    }

    /// <summary>
    /// Elimina un cliente y todo lo que cuelga de él.
    ///
    /// QUÉ SE BORRA: su suscripción y sus controles mensuales, porque no tienen
    /// sentido sin el cliente y quedarían como basura apuntando a un id que ya
    /// no existe.
    ///
    /// QUÉ NO SE BORRA: sus reservas. Esa persona SÍ ocupó un cupo ese día, y
    /// borrarlas cambiaría el historial de asistencia. Lo que se hace es soltar
    /// el vínculo (ClienteId = null): la reserva conserva el nombre como texto
    /// y pasa a ser una reserva de alguien no registrado, que es exactamente lo
    /// que es a partir de ahora.
    /// </summary>
    public Resultado EliminarCliente(int clienteId)
    {
        lock (_datos.Bloqueo)
        {
            var cliente = _datos.BuscarCliente(clienteId);
            if (cliente is null)
            {
                return Resultado.Error("Ese cliente ya no existe.");
            }

            _datos.Suscripciones.RemoveAll(s => s.ClienteId == clienteId);
            _datos.ControlesMensuales.RemoveAll(c => c.ClienteId == clienteId);

            foreach (var reserva in _datos.Reservas.Where(r => r.ClienteId == clienteId))
            {
                reserva.ClienteId = null;
            }

            _datos.Clientes.Remove(cliente);
            return Resultado.Ok($"Se eliminó a {cliente.NombreCompleto} del registro de clientes.");
        }
    }

    /// <summary>Todos los controles mensuales de un cliente, del más reciente al más antiguo.</summary>
    public List<ControlMensual> ControlesDe(int clienteId) =>
        _datos.ControlesMensuales
            .Where(c => c.ClienteId == clienteId)
            .OrderByDescending(c => c.Fecha)
            .ToList();

    /// <summary>
    /// Anota una medición mensual (peso y avance) de un cliente.
    /// El avance es la apreciación del profesor, de 0 a 100.
    /// </summary>
    public Resultado AgregarControlMensual(int clienteId, DateOnly fecha, decimal pesoKg,
        int avanceObjetivo, string nota)
    {
        if (pesoKg < 20 || pesoKg > 300)
        {
            return Resultado.Error("El peso debe estar entre 20 y 300 kilogramos.");
        }

        if (avanceObjetivo < 0 || avanceObjetivo > 100)
        {
            return Resultado.Error("El avance se mide de 0 a 100.");
        }

        lock (_datos.Bloqueo)
        {
            var cliente = _datos.BuscarCliente(clienteId);
            if (cliente is null)
            {
                return Resultado.Error("Ese cliente ya no existe.");
            }

            _datos.ControlesMensuales.Add(new ControlMensual
            {
                Id = _datos.SiguienteIdControl(),
                ClienteId = clienteId,
                Fecha = fecha,
                PesoKg = pesoKg,
                AvanceObjetivo = avanceObjetivo,
                Nota = (nota ?? string.Empty).Trim()
            });

            return Resultado.Ok($"Se registró la medición de {cliente.NombreCompleto}.");
        }
    }

    /// <summary>Borra una medición mensual, por si se anotó mal.</summary>
    public Resultado EliminarControlMensual(int controlId)
    {
        lock (_datos.Bloqueo)
        {
            var control = _datos.ControlesMensuales.FirstOrDefault(c => c.Id == controlId);
            if (control is null)
            {
                return Resultado.Error("Esa medición ya no existe.");
            }

            _datos.ControlesMensuales.Remove(control);
            return Resultado.Ok("Se eliminó la medición.");
        }
    }

    /* =====================================================================
     * 3. PAGOS
     *
     * RECORDATORIO: el sitio no cobra ni procesa dinero. Lo único que hay es
     * esta marca, que el profesor pone y quita a mano cuando le pagan en
     * efectivo (ver Suscripcion.cs).
     * =================================================================== */

    /// <summary>Marca a un cliente como al día o en mora.</summary>
    public Resultado MarcarPago(int clienteId, bool alDia)
    {
        lock (_datos.Bloqueo)
        {
            var suscripcion = _datos.BuscarSuscripcionDeCliente(clienteId);
            if (suscripcion is null)
            {
                return Resultado.Error("Ese cliente no tiene una suscripción abierta.");
            }

            suscripcion.AlDia = alDia;

            var cliente = _datos.BuscarCliente(clienteId);
            var nombre = cliente?.NombreCompleto ?? "El cliente";
            return Resultado.Ok(alDia
                ? $"{nombre} queda marcado como al día."
                : $"{nombre} queda marcado como en mora.");
        }
    }

    /* =====================================================================
     * 4. AVISOS DE PAGO
     * =================================================================== */

    /// <summary>
    /// Los avisos de los clientes a quienes les toca pagar MAÑANA.
    ///
    /// POR QUÉ SE CALCULAN Y NO SE GUARDAN: un aviso guardado ayer diría hoy
    /// "paga mañana" cuando en realidad el plazo ya venció. Calculándolos al
    /// abrir la vista, siempre corresponden al día en que se consultan.
    ///
    /// Por eso van con Id = 0: no están en el almacén, se arman al vuelo.
    /// </summary>
    public List<Notificacion> AvisosDePagoManana()
    {
        var manana = DateOnly.FromDateTime(DateTime.Today).AddDays(1);

        return _datos.Suscripciones
            .Where(s => s.FechaProximoPago == manana)
            .Select(s =>
            {
                var cliente = _datos.BuscarCliente(s.ClienteId);
                var nombre = cliente?.NombreCompleto ?? "Un cliente";
                var plan = _datos.NombrePlan(s.PlanId);

                return new Notificacion
                {
                    Id = 0,
                    Tipo = TipoNotificacion.PagoPorVencer,
                    Mensaje = $"A {nombre} le vence mañana el pago del plan {plan}.",
                    FechaHora = DateTime.Today,
                    Leida = false
                };
            })
            .ToList();
    }

    /* =====================================================================
     * 5. VALIDACIÓN COMPARTIDA
     *
     * Están aquí, en métodos privados, porque el alta y la actualización
     * comprueban exactamente lo mismo. Escritas dos veces, un día una de las
     * dos se quedaría atrás.
     * =================================================================== */

    /// <summary>Quita los espacios sobrantes de los campos de texto.</summary>
    private static void LimpiarTextos(Cliente cliente)
    {
        cliente.NombreCompleto = (cliente.NombreCompleto ?? string.Empty).Trim();
        cliente.NumeroIdentificacion = (cliente.NumeroIdentificacion ?? string.Empty).Trim();
        cliente.Telefono = (cliente.Telefono ?? string.Empty).Trim();
    }

    /// <summary>
    /// Comprueba que los datos de la ficha tengan sentido. Los rangos de
    /// estatura y peso no son caprichosos: atajan el error de digitación de
    /// escribir la estatura en centímetros (172) en vez de metros (1.72).
    /// </summary>
    private Resultado ValidarDatosCliente(Cliente cliente)
    {
        if (cliente.NombreCompleto.Length < 3)
        {
            return Resultado.Error("Escribe el nombre completo del cliente.");
        }

        if (cliente.NumeroIdentificacion.Length < 5)
        {
            return Resultado.Error("Escribe el número de identificación.");
        }

        if (!cliente.NumeroIdentificacion.All(char.IsDigit))
        {
            return Resultado.Error("El número de identificación debe tener solo números, sin puntos ni espacios.");
        }

        if (cliente.Telefono.Length < 7)
        {
            return Resultado.Error("Escribe un teléfono de contacto.");
        }

        if (cliente.EstaturaMetros < 1.0m || cliente.EstaturaMetros > 2.5m)
        {
            return Resultado.Error("La estatura va en metros, entre 1.00 y 2.50. Por ejemplo: 1.72.");
        }

        if (cliente.PesoKg < 20 || cliente.PesoKg > 300)
        {
            return Resultado.Error("El peso debe estar entre 20 y 300 kilogramos.");
        }

        if (_datos.BuscarPlan(cliente.PlanId) is null)
        {
            return Resultado.Error("Elige uno de los tres planes.");
        }

        return Resultado.Ok(string.Empty);
    }

    /// <summary>
    /// Comprueba que el documento no esté usado por otro cliente.
    /// </summary>
    /// <param name="clienteId">
    /// Al actualizar, el id del cliente que se está guardando, para excluirlo
    /// de la revisión. Al crear, null.
    /// </param>
    private Resultado ValidarDocumentoLibre(string numeroIdentificacion, int? clienteId)
    {
        var repetido = _datos.Clientes.Any(c =>
            c.NumeroIdentificacion == numeroIdentificacion && c.Id != clienteId);

        return repetido
            ? Resultado.Error($"Ya hay un cliente registrado con el documento {numeroIdentificacion}.")
            : Resultado.Ok(string.Empty);
    }
}
