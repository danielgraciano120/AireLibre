/*
 * Usuario.cs — Una cuenta con acceso al panel de administración.
 *
 * QUÉ ES: uno de los dos profesores que administran el sitio. Ambos ven y
 * pueden hacer exactamente lo mismo: el sistema NO separa información por
 * profesor y no maneja roles ni permisos.
 *
 * AVISO SOBRE LA CONTRASEÑA: se guarda en texto plano y los usuarios están
 * quemados en el código (ver DatosService.cs). Es una decisión deliberada del
 * trabajo académico, que pide autenticación sencilla sin ASP.NET Core Identity
 * y sin base de datos. NO es aceptable en un sistema real: ahí las contraseñas
 * se guardan con un hash y una sal, nunca legibles.
 */

namespace AireLibre.Models;

public class Usuario
{
    /// <summary>Identificador único dentro del almacén en memoria.</summary>
    public int Id { get; set; }

    /// <summary>Nombre con el que inicia sesión: "profe" o "profesora".</summary>
    public string NombreUsuario { get; set; } = string.Empty;

    /// <summary>Contraseña en texto plano. Ver el aviso del encabezado.</summary>
    public string Contrasena { get; set; } = string.Empty;

    /// <summary>Nombre que se muestra en el panel una vez iniciada la sesión.</summary>
    public string NombreMostrado { get; set; } = string.Empty;
}
