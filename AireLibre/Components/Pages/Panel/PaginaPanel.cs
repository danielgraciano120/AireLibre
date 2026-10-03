/*
 * PaginaPanel.cs — Clase base de todas las páginas del panel de administración.
 *
 * QUÉ HACE: comprueba en OnInitialized si hay sesión abierta y, si no la hay,
 * redirige a /login. Es el candado de las páginas protegidas.
 *
 * CÓMO SE USA: la página del panel la hereda con @inherits, y ya queda
 * protegida sin escribir nada más:
 *
 *     @page "/panel/clientes"
 *     @layout PanelLayout
 *     @inherits PaginaPanel
 *
 * POR QUÉ UNA CLASE BASE Y NO LA MISMA COMPROBACIÓN EN CADA PÁGINA: son ocho
 * páginas protegidas. Copiada ocho veces, basta con olvidarla en una para dejar
 * el panel abierto por esa puerta. Aquí está escrita una sola vez y no se puede
 * olvidar: si la página hereda de PaginaPanel, queda protegida.
 *
 * ------------------------------------------------------------------------
 * CUIDADO AL SOBRESCRIBIR OnInitialized
 * ------------------------------------------------------------------------
 *
 * Si una página necesita inicializar algo propio, DEBE llamar primero a
 * base.OnInitialized(). Si no lo hace, se salta la comprobación de sesión y esa
 * página queda desprotegida:
 *
 *     protected override void OnInitialized()
 *     {
 *         base.OnInitialized();          // <-- imprescindible, va de primero
 *         if (!Sesion.HaySesion) return; // <-- no sigas si va a redirigir
 *
 *         _clientes = Datos.Clientes.ToList();
 *     }
 *
 * Las dos líneas van juntas: base.OnInitialized() dispara la redirección, pero
 * la redirección no interrumpe el método, así que sin el return la página
 * seguiría cargando datos que nunca se van a mostrar.
 */

using AireLibre.Services;
using Microsoft.AspNetCore.Components;

namespace AireLibre.Components.Pages.Panel;

public abstract class PaginaPanel : ComponentBase
{
    /// <summary>
    /// La sesión del usuario. Las páginas que heredan la reciben ya inyectada y
    /// pueden usarla, por ejemplo para mostrar el nombre de quien entró.
    /// </summary>
    [Inject] protected SesionService Sesion { get; set; } = default!;

    /// <summary>Se usa para redirigir a /login cuando no hay sesión.</summary>
    [Inject] protected NavigationManager Navegacion { get; set; } = default!;

    /// <summary>
    /// Comprobación de sesión. Se ejecuta al crearse la página, antes del
    /// primer dibujado.
    /// </summary>
    protected override void OnInitialized()
    {
        /*
         * PRIMERO: no decidir nada durante el prerrenderizado.
         *
         * Blazor dibuja cada página DOS veces: una al responder la petición
         * HTTP (prerrenderizado) y otra cuando se conecta el circuito. Los
         * servicios Scoped no son la misma instancia en los dos momentos, así
         * que en el prerrenderizado SesionService está recién creado y vacío,
         * AUNQUE la persona sí tenga la sesión abierta.
         *
         * Sin esta línea el login quedaba inservible: uno entraba con las
         * credenciales correctas y el prerrenderizado del panel, al no ver
         * sesión, lo devolvía a /login. Un rebote infinito.
         *
         * RendererInfo.IsInteractive es falso en el prerrenderizado y verdadero
         * en el circuito. Al salirse temprano, la comprobación real se hace en
         * la segunda pasada, que es la que tiene la sesión de verdad.
         *
         * Mientras tanto no se filtra nada: las páginas del panel envuelven su
         * contenido en @if (Sesion.HaySesion), así que en el prerrenderizado
         * dibujan la estructura vacía.
         */
        if (!RendererInfo.IsInteractive)
        {
            return;
        }

        // SEGUNDO: ya en el circuito, aquí sí se sabe la verdad.
        if (!Sesion.HaySesion)
        {
            // replace: true reemplaza la entrada en el historial del navegador
            // en vez de agregar una nueva. Así, al pulsar "atrás" desde /login,
            // no se vuelve a la página protegida para rebotar otra vez.
            Navegacion.NavigateTo("/login", replace: true);
        }
    }
}
