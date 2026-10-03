/*
 * InventarioService.cs — Reglas de negocio del inventario de herramientas.
 *
 * QUÉ HACE: el alta, la edición y la baja de los implementos con los que se
 * entrena (pesas rusas, colchonetas, barras, discos, lazos, cajones de salto).
 * Es lo que alimenta /panel/inventario.
 *
 * CÓMO SE USA: la vista lo recibe inyectado y nunca escribe directamente en
 * DatosService.Herramientas, para no saltarse las validaciones.
 *
 * RECORDATORIO DEL MODELO: una Herramienta es un TIPO, no una unidad. "Pesas
 * rusas, cantidad 12" es un solo registro con doce unidades, y el estado
 * describe cómo está el lote completo (ver Herramienta.cs). Por eso el nombre
 * no se puede repetir: dos registros llamados "Colchonetas" serían el mismo
 * lote contado dos veces, y el profesor no sabría cuál es el bueno.
 */

using AireLibre.Models;

namespace AireLibre.Services;

public class InventarioService
{
    private readonly DatosService _datos;

    public InventarioService(DatosService datos)
    {
        _datos = datos;
    }

    /// <summary>El inventario completo, en orden alfabético.</summary>
    public List<Herramienta> Listar() =>
        _datos.Herramientas.OrderBy(h => h.Nombre).ToList();

    /// <summary>
    /// Cuántas herramientas están marcadas para reponer. Es el dato que hace
    /// falta ver de un vistazo: son las que hay que salir a comprar.
    /// </summary>
    public int ContarParaReponer() =>
        _datos.Herramientas.Count(h => h.Estado == EstadoHerramienta.ParaReponer);

    /// <summary>Suma de todas las unidades del inventario.</summary>
    public int TotalUnidades() => _datos.Herramientas.Sum(h => h.Cantidad);

    /// <summary>Agrega una herramienta nueva al inventario.</summary>
    public Resultado Crear(Herramienta herramienta)
    {
        herramienta.Nombre = (herramienta.Nombre ?? string.Empty).Trim();

        var validacion = Validar(herramienta);
        if (!validacion.Exito) return validacion;

        lock (_datos.Bloqueo)
        {
            var nombreLibre = ValidarNombreLibre(herramienta.Nombre, herramientaId: null);
            if (!nombreLibre.Exito) return nombreLibre;

            herramienta.Id = _datos.SiguienteIdHerramienta();
            _datos.Herramientas.Add(herramienta);

            return Resultado.Ok($"Se agregó \"{herramienta.Nombre}\" al inventario.");
        }
    }

    /// <summary>
    /// Guarda los cambios de una herramienta que ya existe.
    ///
    /// Recibe una COPIA (la que se editó en el formulario) y pasa los valores
    /// al registro guardado, para que cancelar a mitad de camino no deje el
    /// original tocado.
    /// </summary>
    public Resultado Actualizar(Herramienta editada)
    {
        editada.Nombre = (editada.Nombre ?? string.Empty).Trim();

        var validacion = Validar(editada);
        if (!validacion.Exito) return validacion;

        lock (_datos.Bloqueo)
        {
            var guardada = _datos.Herramientas.FirstOrDefault(h => h.Id == editada.Id);
            if (guardada is null)
            {
                return Resultado.Error("Esa herramienta ya no está en el inventario.");
            }

            // Se excluye ella misma de la revisión de nombre repetido: si no,
            // no podría guardarse sin cambiarle el nombre.
            var nombreLibre = ValidarNombreLibre(editada.Nombre, guardada.Id);
            if (!nombreLibre.Exito) return nombreLibre;

            guardada.Nombre = editada.Nombre;
            guardada.Cantidad = editada.Cantidad;
            guardada.Estado = editada.Estado;

            return Resultado.Ok($"Se actualizó \"{guardada.Nombre}\".");
        }
    }

    /// <summary>Saca una herramienta del inventario.</summary>
    public Resultado Eliminar(int herramientaId)
    {
        lock (_datos.Bloqueo)
        {
            var herramienta = _datos.Herramientas.FirstOrDefault(h => h.Id == herramientaId);
            if (herramienta is null)
            {
                return Resultado.Error("Esa herramienta ya no está en el inventario.");
            }

            _datos.Herramientas.Remove(herramienta);
            return Resultado.Ok($"Se eliminó \"{herramienta.Nombre}\" del inventario.");
        }
    }

    // --- Validación compartida por Crear y Actualizar ------------------------

    private static Resultado Validar(Herramienta herramienta)
    {
        if (herramienta.Nombre.Length < 3)
        {
            return Resultado.Error("Escribe el nombre de la herramienta.");
        }

        // Cero es válido: sirve para dejar registrado un implemento que se
        // acabó por completo y hay que volver a comprar.
        if (herramienta.Cantidad < 0)
        {
            return Resultado.Error("La cantidad no puede ser negativa.");
        }

        if (herramienta.Cantidad > 9999)
        {
            return Resultado.Error("Esa cantidad no parece correcta. Revisa el número.");
        }

        return Resultado.Ok(string.Empty);
    }

    /// <summary>
    /// Comprueba que no haya otra herramienta con el mismo nombre. La
    /// comparación no distingue mayúsculas: "Lazos" y "lazos" son el mismo lote.
    /// </summary>
    private Resultado ValidarNombreLibre(string nombre, int? herramientaId)
    {
        var repetida = _datos.Herramientas.Any(h =>
            string.Equals(h.Nombre, nombre, StringComparison.OrdinalIgnoreCase) &&
            h.Id != herramientaId);

        return repetida
            ? Resultado.Error($"Ya hay una herramienta llamada \"{nombre}\" en el inventario.")
            : Resultado.Ok(string.Empty);
    }
}
