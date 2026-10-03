/*
 * SesionService.cs — Autenticación sencilla del panel.
 *
 * QUÉ HACE: guarda quién inició sesión y valida usuario y contraseña contra los
 * dos usuarios quemados del almacén (profe / profesora, ambos con "aire2026").
 *
 * NO USA ASP.NET Core Identity, ni cookies, ni tokens, ni base de datos. Es lo
 * que pide el enunciado: autenticación sencilla y explicable.
 *
 * ------------------------------------------------------------------------
 * POR QUÉ ES SCOPED Y NO SINGLETON  (esto es lo importante del archivo)
 * ------------------------------------------------------------------------
 *
 * DatosService sí es singleton, porque los datos son los mismos para todos.
 * La sesión es lo contrario: es de cada quien. Si SesionService fuera
 * singleton, habría UNA sola sesión para toda la aplicación: en el momento en
 * que un profesor iniciara sesión, cualquier persona que abriera el sitio desde
 * otro computador entraría al panel ya autenticada. Sería un agujero, no un
 * detalle.
 *
 * Scoped en Blazor Server significa "una instancia por circuito", y un circuito
 * es la conexión de una pestaña del navegador con el servidor. Así cada quien
 * tiene su propia sesión.
 *
 * ------------------------------------------------------------------------
 * LÍMITE CONOCIDO: LA SESIÓN NO SOBREVIVE A UN F5
 * ------------------------------------------------------------------------
 *
 * Como la sesión vive en memoria dentro del circuito, al refrescar la página
 * (F5) o al escribir una dirección del panel directamente en la barra del
 * navegador, se crea un circuito nuevo, con un SesionService nuevo y vacío: hay
 * que volver a iniciar sesión. Navegar por los enlaces del sitio sí la
 * conserva, porque no se recarga la página.
 *
 * Que la sesión sobreviva a un F5 exigiría guardarla fuera del circuito (una
 * cookie de autenticación o el almacenamiento del navegador), que es
 * justamente la plomería que el enunciado pidió evitar.
 *
 * ------------------------------------------------------------------------
 * AVISO DE SEGURIDAD
 * ------------------------------------------------------------------------
 *
 * Las contraseñas se comparan en texto plano contra valores escritos en el
 * código. En un sistema real esto no se hace nunca: se guarda un hash con sal
 * (por ejemplo con BCrypt o PBKDF2) y se compara el hash, de modo que ni
 * siquiera quien tiene acceso a la base de datos pueda leer las contraseñas.
 * Aquí se deja así a propósito, porque el enunciado pide autenticación sencilla
 * y sin librerías externas.
 */

using AireLibre.Models;

namespace AireLibre.Services;

public class SesionService
{
    private readonly DatosService _datos;

    public SesionService(DatosService datos)
    {
        _datos = datos;
    }

    /// <summary>
    /// Quién tiene la sesión abierta, o null si no hay nadie. Solo este
    /// servicio puede cambiarlo (el set es privado), para que ninguna vista
    /// pueda "iniciar sesión" asignándolo a mano y saltarse la validación.
    /// </summary>
    public Usuario? UsuarioActual { get; private set; }

    /// <summary>Verdadero si hay alguien autenticado. Es lo que consultan las páginas del panel.</summary>
    public bool HaySesion => UsuarioActual is not null;

    /// <summary>Nombre para mostrar en el panel, o cadena vacía si no hay sesión.</summary>
    public string NombreMostrado => UsuarioActual?.NombreMostrado ?? string.Empty;

    /// <summary>
    /// Valida las credenciales y, si son correctas, abre la sesión.
    /// </summary>
    public Resultado IniciarSesion(string nombreUsuario, string contrasena)
    {
        nombreUsuario = (nombreUsuario ?? string.Empty).Trim();
        contrasena = contrasena ?? string.Empty;

        if (nombreUsuario.Length == 0 || contrasena.Length == 0)
        {
            return Resultado.Error("Escribe el usuario y la contraseña.");
        }

        // El usuario se compara sin distinguir mayúsculas ("Profe" entra igual
        // que "profe"), pero la contraseña se compara EXACTA: distinguir
        // mayúsculas es parte de lo que hace fuerte a una contraseña.
        var usuario = _datos.Usuarios.FirstOrDefault(u =>
            string.Equals(u.NombreUsuario, nombreUsuario, StringComparison.OrdinalIgnoreCase) &&
            u.Contrasena == contrasena);

        if (usuario is null)
        {
            // El mensaje no dice si falló el usuario o la contraseña. Decirlo
            // le confirmaría a un desconocido cuáles usuarios existen.
            return Resultado.Error("Usuario o contraseña incorrectos.");
        }

        UsuarioActual = usuario;
        return Resultado.Ok($"Hola, {usuario.NombreMostrado}.");
    }

    /// <summary>Cierra la sesión. A partir de aquí el panel vuelve a estar bloqueado.</summary>
    public void CerrarSesion()
    {
        UsuarioActual = null;
    }
}
