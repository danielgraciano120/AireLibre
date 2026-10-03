/*
 * DatosService.cs — Almacén de datos en memoria. Hace las veces de base de datos.
 *
 * QUÉ ES: la única fuente de datos del sistema. Guarda todas las listas en
 * memoria RAM y las precarga con datos de ejemplo al arrancar la aplicación.
 * No hay base de datos, ni archivos, ni red.
 *
 * CÓMO SE USA: se registra en Program.cs como SINGLETON. Los demás servicios
 * (ReservaService, ClienteService, InventarioService) lo reciben inyectado y
 * trabajan sobre sus listas. Las vistas .razor NO deberían tocar este servicio
 * directamente para escribir: piden a los servicios de negocio, que son los que
 * conocen las reglas.
 *
 * POR QUÉ SINGLETON Y NO SCOPED: en Blazor Server, "scoped" dura lo que dura la
 * conexión de un usuario. Si el almacén fuera scoped, cada persona vería su
 * propia copia de los datos y las reservas de una no las vería la otra; además
 * todo se perdería al refrescar la página. Singleton = una sola copia
 * compartida por toda la aplicación, que es lo que necesita un sistema donde
 * dos profesores administran la misma información.
 *
 * ADVERTENCIA: al ser singleton, varios usuarios pueden escribir AL MISMO
 * TIEMPO. Por eso existe la propiedad Bloqueo: cualquier operación que primero
 * LEA y después ESCRIBA según lo leído (el caso típico es "contar reservas y,
 * si caben, insertar") debe hacerse dentro de lock(Bloqueo). Sin eso, dos
 * personas reservando en el mismo instante podrían pasarse del límite de 20.
 *
 * LÍMITE CONOCIDO: todo vive en RAM, así que al detener la aplicación los datos
 * se pierden y se vuelve a cargar el ejemplo. Es lo esperado en esta etapa; más
 * adelante este servicio se reemplaza por EF Core sin tocar las vistas.
 */

using AireLibre.Models;

namespace AireLibre.Services;

public class DatosService
{
    /* -----------------------------------------------------------------------
     * CANDADO DE CONCURRENCIA
     * --------------------------------------------------------------------- */

    /// <summary>
    /// Objeto usado como candado para las operaciones de leer-validar-escribir.
    /// Los servicios lo usan así:  lock (datos.Bloqueo) { ... }
    /// Ver la advertencia del encabezado del archivo.
    /// </summary>
    public object Bloqueo { get; } = new object();

    /* -----------------------------------------------------------------------
     * LAS LISTAS: cada una es el equivalente a una tabla.
     *
     * Son de solo lectura (get, sin set) para que nadie pueda reemplazar la
     * lista entera por otra; el contenido sí se agrega y se quita normalmente.
     * --------------------------------------------------------------------- */

    public List<Grupo> Grupos { get; } = new();
    public List<Horario> Horarios { get; } = new();
    public List<Plan> Planes { get; } = new();
    public List<Usuario> Usuarios { get; } = new();
    public List<Cliente> Clientes { get; } = new();
    public List<Suscripcion> Suscripciones { get; } = new();
    public List<ControlMensual> ControlesMensuales { get; } = new();
    public List<Reserva> Reservas { get; } = new();
    public List<SolicitudPlan> Solicitudes { get; } = new();
    public List<Herramienta> Herramientas { get; } = new();
    public List<Notificacion> Notificaciones { get; } = new();

    /* -----------------------------------------------------------------------
     * GENERACIÓN DE IDENTIFICADORES
     *
     * Cada lista que crece en tiempo de ejecución tiene su propio contador. Se
     * usa un contador que solo sube y NUNCA un "máximo + 1", porque el máximo
     * se reutilizaría después de borrar el último registro: un cliente nuevo
     * heredaría el id del borrado y se quedaría con sus pagos y sus controles
     * mensuales. Con un contador ascendente eso no puede pasar.
     *
     * Grupos, Horarios, Planes y Usuarios no tienen contador: son datos fijos
     * del sistema y no se crean desde la aplicación.
     * --------------------------------------------------------------------- */

    private int _ultimoIdCliente;
    private int _ultimoIdSuscripcion;
    private int _ultimoIdControl;
    private int _ultimoIdReserva;
    private int _ultimoIdSolicitud;
    private int _ultimoIdHerramienta;
    private int _ultimoIdNotificacion;

    public int SiguienteIdCliente() => ++_ultimoIdCliente;
    public int SiguienteIdSuscripcion() => ++_ultimoIdSuscripcion;
    public int SiguienteIdControl() => ++_ultimoIdControl;
    public int SiguienteIdReserva() => ++_ultimoIdReserva;
    public int SiguienteIdSolicitud() => ++_ultimoIdSolicitud;
    public int SiguienteIdHerramienta() => ++_ultimoIdHerramienta;
    public int SiguienteIdNotificacion() => ++_ultimoIdNotificacion;

    /* -----------------------------------------------------------------------
     * BÚSQUEDAS POR IDENTIFICADOR
     *
     * Atajos que evitan repetir el mismo FirstOrDefault en cada vista. Todos
     * devuelven null si no encuentran nada (por eso el tipo lleva ?), así que
     * quien llama debe contemplar ese caso.
     * --------------------------------------------------------------------- */

    public Grupo? BuscarGrupo(int id) => Grupos.FirstOrDefault(g => g.Id == id);

    public Horario? BuscarHorario(int id) => Horarios.FirstOrDefault(h => h.Id == id);

    public Plan? BuscarPlan(int id) => Planes.FirstOrDefault(p => p.Id == id);

    public Cliente? BuscarCliente(int id) => Clientes.FirstOrDefault(c => c.Id == id);

    public Suscripcion? BuscarSuscripcionDeCliente(int clienteId) =>
        Suscripciones.FirstOrDefault(s => s.ClienteId == clienteId);

    /// <summary>Nombre del plan, o un guion si el plan ya no existe.</summary>
    public string NombrePlan(int planId) => BuscarPlan(planId)?.Nombre ?? "—";

    /// <summary>
    /// Texto completo de un horario para mostrarlo sin ambigüedad, por ejemplo
    /// "Grupo de la noche · 7:00 pm". Se usa en el panel y en las
    /// notificaciones, donde hace falta saber de qué clase se habla.
    /// </summary>
    public string DescribirHorario(int horarioId)
    {
        var horario = BuscarHorario(horarioId);
        if (horario is null) return "Horario desconocido";

        var grupo = BuscarGrupo(horario.GrupoId);
        return grupo is null ? horario.HoraTexto : $"{grupo.Nombre} · {horario.HoraTexto}";
    }

    /* -----------------------------------------------------------------------
     * CONSTRUCTOR
     * --------------------------------------------------------------------- */

    public DatosService()
    {
        CargarDatosDeEjemplo();
    }

    /* -----------------------------------------------------------------------
     * DATOS DE EJEMPLO
     *
     * Se cargan una sola vez, al crearse el singleton. Las fechas se calculan
     * SIEMPRE en relación con el día de hoy (hoy.AddDays(...)) y nunca con
     * fechas escritas a mano: así el sistema muestra información coherente sin
     * importar en qué día se ejecute. De eso dependen dos cosas del panel:
     * el contador de "cupos separados hoy" y los avisos de pagos que vencen
     * mañana.
     * --------------------------------------------------------------------- */

    private void CargarDatosDeEjemplo()
    {
        var hoy = DateOnly.FromDateTime(DateTime.Today);

        // --- Grupos: dos, uno por jornada -----------------------------------
        Grupos.Add(new Grupo
        {
            Id = 1,
            Nombre = "Grupo de la mañana",
            Jornada = Jornada.Manana,
            Descripcion = "Para arrancar el día. Se entrena con el fresco, antes de la jornada laboral."
        });
        Grupos.Add(new Grupo
        {
            Id = 2,
            Nombre = "Grupo de la noche",
            Jornada = Jornada.Noche,
            Descripcion = "Para soltar el día. Dos horarios para acomodarse a la salida del trabajo."
        });

        // --- Horarios: uno en la mañana, dos en la noche. 20 cupos cada uno --
        Horarios.Add(new Horario { Id = 1, GrupoId = 1, Hora = new TimeOnly(8, 30), CupoMaximo = 20 });
        Horarios.Add(new Horario { Id = 2, GrupoId = 2, Hora = new TimeOnly(19, 0), CupoMaximo = 20 });
        Horarios.Add(new Horario { Id = 3, GrupoId = 2, Hora = new TimeOnly(20, 0), CupoMaximo = 20 });

        // --- Planes ---------------------------------------------------------
        Planes.Add(new Plan
        {
            Id = 1,
            Nombre = "Diario",
            DiasDuracion = 1,
            Precio = 12000,
            Descripcion = "Una sola clase. Para probar el grupo o para quien entrena de vez en cuando."
        });
        Planes.Add(new Plan
        {
            Id = 2,
            Nombre = "Quincenal",
            DiasDuracion = 15,
            Precio = 70000,
            Descripcion = "Quince días seguidos. El punto medio para quien todavía no quiere comprometer el mes."
        });
        Planes.Add(new Plan
        {
            Id = 3,
            Nombre = "Mensual",
            DiasDuracion = 30,
            Precio = 120000,
            Descripcion = "Treinta días. Es el plan de quien ya hizo del entrenamiento una rutina."
        });

        // --- Usuarios del panel ---------------------------------------------
        // Contraseñas en texto plano y quemadas en el código: decisión del
        // trabajo académico, no válida en un sistema real (ver Usuario.cs).
        Usuarios.Add(new Usuario { Id = 1, NombreUsuario = "profe", Contrasena = "aire2026", NombreMostrado = "Profesor" });
        Usuarios.Add(new Usuario { Id = 2, NombreUsuario = "profesora", Contrasena = "aire2026", NombreMostrado = "Profesora" });

        // --- Clientes -------------------------------------------------------
        AgregarClienteDeEjemplo(1, "Valentina Restrepo Ochoa", TipoDocumento.Cedula, "1017245896", "310 458 2310", 1.63m, 58.4m, Objetivo.Tonificar, 3, hoy.AddDays(-58));
        AgregarClienteDeEjemplo(2, "Andrés Felipe Cardona", TipoDocumento.Cedula, "71298455", "315 778 9021", 1.78m, 82.1m, Objetivo.Volumen, 3, hoy.AddDays(-92));
        AgregarClienteDeEjemplo(3, "Laura Sofía Marín", TipoDocumento.TarjetaIdentidad, "1094556712", "322 991 4477", 1.60m, 54.0m, Objetivo.Definicion, 2, hoy.AddDays(-31));
        AgregarClienteDeEjemplo(4, "Julián Ospina Vélez", TipoDocumento.Cedula, "8125476", "301 123 4567", 1.81m, 91.5m, Objetivo.Definicion, 3, hoy.AddDays(-124));
        AgregarClienteDeEjemplo(5, "Mariana Zapata Gil", TipoDocumento.Cedula, "1035998741", "318 334 5566", 1.68m, 63.2m, Objetivo.SaludYVitalidad, 3, hoy.AddDays(-15));
        AgregarClienteDeEjemplo(6, "Santiago Betancur Ruiz", TipoDocumento.Cedula, "1152447788", "300 667 7889", 1.74m, 70.8m, Objetivo.Volumen, 2, hoy.AddDays(-9));
        AgregarClienteDeEjemplo(7, "Daniela Arango Loaiza", TipoDocumento.Cedula, "43587112", "314 556 6778", 1.59m, 66.9m, Objetivo.SaludYVitalidad, 3, hoy.AddDays(-76));
        AgregarClienteDeEjemplo(8, "Camilo Herrera Duque", TipoDocumento.TarjetaIdentidad, "1027889654", "319 223 3445", 1.70m, 60.3m, Objetivo.Tonificar, 1, hoy.AddDays(-2));

        // --- Suscripciones --------------------------------------------------
        // Se reparten a propósito para que el panel tenga los tres casos:
        //   * al día con el pago lejos,
        //   * al día pero con el pago MAÑANA (dispara aviso),
        //   * en mora (cuenta en "pagos pendientes").
        AgregarSuscripcionDeEjemplo(1, 1, 3, hoy.AddDays(-25), hoy.AddDays(5), alDia: true);
        AgregarSuscripcionDeEjemplo(2, 2, 3, hoy.AddDays(-29), hoy.AddDays(1), alDia: true);   // paga mañana
        AgregarSuscripcionDeEjemplo(3, 3, 2, hoy.AddDays(-14), hoy.AddDays(1), alDia: false);  // paga mañana y está en mora
        AgregarSuscripcionDeEjemplo(4, 4, 3, hoy.AddDays(-40), hoy.AddDays(-10), alDia: false);
        AgregarSuscripcionDeEjemplo(5, 5, 3, hoy.AddDays(-10), hoy.AddDays(20), alDia: true);
        AgregarSuscripcionDeEjemplo(6, 6, 2, hoy.AddDays(-3), hoy.AddDays(12), alDia: true);
        AgregarSuscripcionDeEjemplo(7, 7, 3, hoy.AddDays(-33), hoy.AddDays(-3), alDia: false);
        AgregarSuscripcionDeEjemplo(8, 8, 1, hoy, hoy.AddDays(1), alDia: true);                // paga mañana

        // --- Controles mensuales --------------------------------------------
        // Solo para tres clientes con varios meses encima, que son los que
        // tienen algo que mostrar en la vista de detalle.
        AgregarControlDeEjemplo(1, 1, hoy.AddDays(-58), 61.5m, 10, "Primera medición. Arranca con trabajo de técnica.");
        AgregarControlDeEjemplo(2, 1, hoy.AddDays(-28), 59.8m, 35, "Buena adherencia, no ha faltado a una clase.");
        AgregarControlDeEjemplo(3, 1, hoy.AddDays(-2), 58.4m, 55, "Ya levanta la barra con técnica limpia.");

        AgregarControlDeEjemplo(4, 2, hoy.AddDays(-92), 78.0m, 15, "Quiere ganar masa. Se le sube la carga con cuidado.");
        AgregarControlDeEjemplo(5, 2, hoy.AddDays(-61), 79.9m, 40, "Subió peso sin perder movilidad.");
        AgregarControlDeEjemplo(6, 2, hoy.AddDays(-30), 81.2m, 60, "Muy constante. Se le suman dominadas con lastre.");
        AgregarControlDeEjemplo(7, 2, hoy.AddDays(-1), 82.1m, 75, "Cerca de la meta de volumen que se puso.");

        AgregarControlDeEjemplo(8, 4, hoy.AddDays(-124), 97.3m, 5, "Empieza desde cero, se prioriza la técnica.");
        AgregarControlDeEjemplo(9, 4, hoy.AddDays(-93), 95.1m, 20, "Bajó dos kilos. Le cuesta la parte aeróbica.");
        AgregarControlDeEjemplo(10, 4, hoy.AddDays(-62), 93.4m, 35, "Mejor resistencia, ya completa el circuito.");
        AgregarControlDeEjemplo(11, 4, hoy.AddDays(-31), 92.0m, 45, "Constante. Falta cuidar la alimentación.");
        AgregarControlDeEjemplo(12, 4, hoy.AddDays(-3), 91.5m, 50, "Se estancó un poco; se le cambia la rutina.");

        // --- Reservas de hoy ------------------------------------------------
        // Los tres horarios quedan con ocupaciones distintas a propósito, para
        // que el medidor de 20 marcas se vea diferente en cada uno. El de las
        // 8:00 pm queda con un solo cupo libre: así se puede probar en dos
        // clics que al llenarse aparece el mensaje y el botón se deshabilita.
        CargarReservasDeEjemplo(hoy);

        // --- Inventario -----------------------------------------------------
        Herramientas.Add(new Herramienta { Id = 1, Nombre = "Pesas rusas", Cantidad = 12, Estado = EstadoHerramienta.Bueno });
        Herramientas.Add(new Herramienta { Id = 2, Nombre = "Colchonetas", Cantidad = 18, Estado = EstadoHerramienta.Regular });
        Herramientas.Add(new Herramienta { Id = 3, Nombre = "Barras", Cantidad = 6, Estado = EstadoHerramienta.Bueno });
        Herramientas.Add(new Herramienta { Id = 4, Nombre = "Discos", Cantidad = 24, Estado = EstadoHerramienta.Bueno });
        Herramientas.Add(new Herramienta { Id = 5, Nombre = "Lazos", Cantidad = 15, Estado = EstadoHerramienta.ParaReponer });
        Herramientas.Add(new Herramienta { Id = 6, Nombre = "Cajones de salto", Cantidad = 4, Estado = EstadoHerramienta.Regular });

        // --- Solicitudes de plan --------------------------------------------
        Solicitudes.Add(new SolicitudPlan
        {
            Id = 1,
            NombreCompleto = "Sara Manuela Giraldo",
            TipoDocumento = TipoDocumento.Cedula,
            NumeroIdentificacion = "1044887723",
            Telefono = "312 445 6677",
            PlanId = 3,
            FechaSolicitud = DateTime.Today.AddDays(-1).AddHours(19).AddMinutes(42),
            Estado = EstadoSolicitud.Pendiente
        });
        Solicitudes.Add(new SolicitudPlan
        {
            Id = 2,
            NombreCompleto = "Jerónimo Castaño Mejía",
            TipoDocumento = TipoDocumento.TarjetaIdentidad,
            NumeroIdentificacion = "1098334512",
            Telefono = "320 118 9934",
            PlanId = 2,
            FechaSolicitud = DateTime.Today.AddHours(7).AddMinutes(15),
            Estado = EstadoSolicitud.Pendiente
        });
        Solicitudes.Add(new SolicitudPlan
        {
            Id = 3,
            NombreCompleto = "Paula Andrea Molina",
            TipoDocumento = TipoDocumento.Cedula,
            NumeroIdentificacion = "43991256",
            Telefono = "301 774 2211",
            PlanId = 1,
            FechaSolicitud = DateTime.Today.AddHours(10).AddMinutes(3),
            Estado = EstadoSolicitud.Pendiente
        });
        Solicitudes.Add(new SolicitudPlan
        {
            Id = 4,
            NombreCompleto = "Camilo Herrera Duque",
            TipoDocumento = TipoDocumento.TarjetaIdentidad,
            NumeroIdentificacion = "1027889654",
            Telefono = "319 223 3445",
            PlanId = 1,
            FechaSolicitud = DateTime.Today.AddDays(-2).AddHours(16),
            Estado = EstadoSolicitud.Convertida   // este ya es el cliente 8
        });

        // --- Notificaciones ya acumuladas -----------------------------------
        // Simulan las reservas que entraron antes de abrir el panel. Las de
        // pagos por vencer NO se guardan: se calculan al abrir la vista
        // (ver Notificacion.cs).
        CargarNotificacionesDeEjemplo();

        // --- Ajuste final de los contadores de id ---------------------------
        // Los datos de ejemplo se insertaron con ids escritos a mano, así que
        // los contadores hay que dejarlos por encima del mayor id usado. Si no,
        // el primer registro que cree un usuario repetiría un id existente.
        SincronizarContadores();
    }

    /* -----------------------------------------------------------------------
     * AYUDANTES PRIVADOS DE CARGA
     *
     * Existen solo para que el método de arriba se lea como una lista de datos
     * y no como una pared de inicializadores repetidos.
     * --------------------------------------------------------------------- */

    private void AgregarClienteDeEjemplo(int id, string nombre, TipoDocumento tipoDocumento,
        string identificacion, string telefono, decimal estatura, decimal peso,
        Objetivo objetivo, int planId, DateOnly fechaRegistro)
    {
        Clientes.Add(new Cliente
        {
            Id = id,
            NombreCompleto = nombre,
            TipoDocumento = tipoDocumento,
            NumeroIdentificacion = identificacion,
            Telefono = telefono,
            EstaturaMetros = estatura,
            PesoKg = peso,
            Objetivo = objetivo,
            PlanId = planId,
            FechaRegistro = fechaRegistro
        });
    }

    private void AgregarSuscripcionDeEjemplo(int id, int clienteId, int planId,
        DateOnly inicio, DateOnly proximoPago, bool alDia)
    {
        Suscripciones.Add(new Suscripcion
        {
            Id = id,
            ClienteId = clienteId,
            PlanId = planId,
            FechaInicio = inicio,
            FechaProximoPago = proximoPago,
            AlDia = alDia
        });
    }

    private void AgregarControlDeEjemplo(int id, int clienteId, DateOnly fecha,
        decimal peso, int avance, string nota)
    {
        ControlesMensuales.Add(new ControlMensual
        {
            Id = id,
            ClienteId = clienteId,
            Fecha = fecha,
            PesoKg = peso,
            AvanceObjetivo = avance,
            Nota = nota
        });
    }

    /// <summary>
    /// Crea las reservas del día de hoy repartidas entre los tres horarios.
    /// Los nombres salen de una lista fija y se van tomando en orden; los ocho
    /// primeros corresponden a clientes registrados, así que a esas reservas se
    /// les pone también el ClienteId para mostrar que la relación existe pero
    /// es opcional (ver Reserva.cs).
    /// </summary>
    private void CargarReservasDeEjemplo(DateOnly hoy)
    {
        // Cuántas reservas lleva cada horario. El de las 8:00 pm queda en 19
        // de 20 para poder probar el tope sin tener que reservar veinte veces.
        var ocupacionPorHorario = new[] { (HorarioId: 1, Cantidad: 7), (HorarioId: 2, Cantidad: 16), (HorarioId: 3, Cantidad: 19) };

        var nombres = new[]
        {
            "Valentina Restrepo Ochoa", "Andrés Felipe Cardona", "Laura Sofía Marín", "Julián Ospina Vélez",
            "Mariana Zapata Gil", "Santiago Betancur Ruiz", "Daniela Arango Loaiza", "Camilo Herrera Duque",
            "Isabella Gómez Trujillo", "Sebastián Álvarez Pineda", "Manuela Ríos Cardona", "Tomás Escobar Uribe",
            "Sofía Jaramillo Muñoz", "Emmanuel Quintero Salas", "Antonia Vargas Bedoya", "Martín Agudelo Correa",
            "Salomé Hincapié Ortiz", "Alejandro Naranjo Vera", "Gabriela Londoño Serna", "Nicolás Piedrahíta Ramos",
            "Luciana Mesa Toro", "Simón Calderón Ávila", "Emilia Franco Duque", "Matías Velásquez Rendón",
            "Renata Osorio Cano", "Felipe Guzmán Tabares", "Amelia Torres Villa", "Joaquín Suárez Palacio",
            "Catalina Ceballos Ruiz", "Samuel Montoya Grisales", "Elena Duarte Salazar", "Miguel Ángel Peláez",
            "Julieta Bernal Acosta", "Andrés Camilo Rojas", "Paulina Estrada Henao", "David Santiago Cuartas",
            "Mariángel Pérez Soto", "Lucas Zuluaga Medina", "Alejandra Buitrago Ríos", "Óscar Iván Mejía",
            "Natalia Ospina Cárdenas", "Juan José Ramírez Lopera"
        };

        var siguienteId = 1;
        var indiceNombre = 0;

        foreach (var (horarioId, cantidad) in ocupacionPorHorario)
        {
            for (var i = 0; i < cantidad; i++)
            {
                var nombre = nombres[indiceNombre];

                // Si el nombre coincide con un cliente registrado, se enlaza.
                // Si no, ClienteId queda nulo: es alguien que reservó sin
                // registrarse, que es el caso normal.
                var cliente = Clientes.FirstOrDefault(c => c.NombreCompleto == nombre);

                Reservas.Add(new Reserva
                {
                    Id = siguienteId,
                    HorarioId = horarioId,
                    NombreCompleto = nombre,
                    ClienteId = cliente?.Id,
                    FechaClase = hoy,
                    // Se reparten a lo largo de la mañana para que el orden por
                    // fecha de registro tenga sentido en el panel.
                    FechaRegistro = DateTime.Today.AddHours(6).AddMinutes(siguienteId * 7)
                });

                siguienteId++;
                indiceNombre++;
            }
        }
    }

    /// <summary>
    /// Deja algunos avisos de cupo separado ya acumulados, para que la vista de
    /// notificaciones no arranque vacía. Se toman las tres reservas más
    /// recientes del día.
    /// </summary>
    private void CargarNotificacionesDeEjemplo()
    {
        var id = 1;

        foreach (var reserva in Reservas.OrderByDescending(r => r.FechaRegistro).Take(3).Reverse())
        {
            Notificaciones.Add(new Notificacion
            {
                Id = id++,
                Tipo = TipoNotificacion.CupoSeparado,
                Mensaje = $"{reserva.NombreCompleto} separó un cupo para {DescribirHorario(reserva.HorarioId)}.",
                FechaHora = reserva.FechaRegistro,
                Leida = false
            });
        }
    }

    /// <summary>
    /// Pone cada contador de id justo encima del mayor id que quedó cargado en
    /// su lista. Se llama una sola vez, al final de la carga de ejemplo.
    /// </summary>
    private void SincronizarContadores()
    {
        _ultimoIdCliente = Clientes.Count == 0 ? 0 : Clientes.Max(c => c.Id);
        _ultimoIdSuscripcion = Suscripciones.Count == 0 ? 0 : Suscripciones.Max(s => s.Id);
        _ultimoIdControl = ControlesMensuales.Count == 0 ? 0 : ControlesMensuales.Max(c => c.Id);
        _ultimoIdReserva = Reservas.Count == 0 ? 0 : Reservas.Max(r => r.Id);
        _ultimoIdSolicitud = Solicitudes.Count == 0 ? 0 : Solicitudes.Max(s => s.Id);
        _ultimoIdHerramienta = Herramientas.Count == 0 ? 0 : Herramientas.Max(h => h.Id);
        _ultimoIdNotificacion = Notificaciones.Count == 0 ? 0 : Notificaciones.Max(n => n.Id);
    }
}
