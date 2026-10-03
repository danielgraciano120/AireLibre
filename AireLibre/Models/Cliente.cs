/*
 * Cliente.cs — Una persona registrada en el grupo.
 *
 * QUÉ ES: la ficha de quien entrena de forma habitual. Se crea desde el panel
 * (/panel/clientes) o convirtiendo una solicitud de plan (/panel/solicitudes).
 *
 * REGLA DE NEGOCIO: el número de identificación es único cuando existe, y es lo
 * que se usa para no registrar dos veces a la misma persona. Quien hace cumplir
 * esa regla es ClienteService, no este modelo.
 *
 * UNIDADES: la estatura va en metros (1.72) y el peso en kilogramos (68.5). Se
 * usa decimal y no double porque son cantidades que se muestran al usuario y
 * decimal no arrastra los errores de redondeo del punto flotante binario.
 */

namespace AireLibre.Models;

public class Cliente
{
    /// <summary>Identificador único dentro del almacén en memoria.</summary>
    public int Id { get; set; }

    public string NombreCompleto { get; set; } = string.Empty;

    public TipoDocumento TipoDocumento { get; set; } = TipoDocumento.Cedula;

    /// <summary>Número del documento. Único entre todos los clientes.</summary>
    public string NumeroIdentificacion { get; set; } = string.Empty;

    public string Telefono { get; set; } = string.Empty;

    /// <summary>Estatura en metros, por ejemplo 1.72.</summary>
    public decimal EstaturaMetros { get; set; }

    /// <summary>
    /// Peso en kilogramos al momento de registrarse. El peso que va cambiando
    /// mes a mes NO se sobrescribe aquí: se anota en ControlMensual, para no
    /// perder el histórico (ver ControlMensual.cs).
    /// </summary>
    public decimal PesoKg { get; set; }

    public Objetivo Objetivo { get; set; } = Objetivo.SaludYVitalidad;

    /// <summary>Plan que tiene contratado (ver Plan.cs).</summary>
    public int PlanId { get; set; }

    public DateOnly FechaRegistro { get; set; }

    /// <summary>
    /// Índice de masa corporal: peso dividido entre la estatura al cuadrado.
    /// Se calcula, no se guarda, para que siempre corresponda a los datos
    /// actuales. Devuelve 0 si no hay estatura, que evita dividir por cero.
    /// </summary>
    public decimal Imc =>
        EstaturaMetros <= 0 ? 0 : Math.Round(PesoKg / (EstaturaMetros * EstaturaMetros), 1);

    /// <summary>
    /// Devuelve una copia independiente de esta ficha.
    ///
    /// PARA QUÉ SIRVE: el formulario de edición del panel trabaja sobre la
    /// copia, no sobre el objeto guardado. Sin esto, cada tecla que se escribe
    /// en el formulario cambiaría el cliente en vivo, y pulsar "Cancelar" no
    /// desharía nada porque el cambio ya estaría hecho.
    /// </summary>
    public Cliente Copia() => new()
    {
        Id = Id,
        NombreCompleto = NombreCompleto,
        TipoDocumento = TipoDocumento,
        NumeroIdentificacion = NumeroIdentificacion,
        Telefono = Telefono,
        EstaturaMetros = EstaturaMetros,
        PesoKg = PesoKg,
        Objetivo = Objetivo,
        PlanId = PlanId,
        FechaRegistro = FechaRegistro
    };
}
