/*
 * Program.cs — Arranque y configuración de la aplicación.
 *
 * QUÉ HACE: crea la aplicación web, registra los servicios en el contenedor de
 * inyección de dependencias, arma el canal de peticiones HTTP y la pone a
 * escuchar. Es el primer archivo que se ejecuta.
 *
 * DOS COSAS QUE NO SE DEBEN CAMBIAR SIN SABER POR QUÉ:
 *
 *   1. AddInteractiveServerComponents() + AddInteractiveServerRenderMode() son
 *      lo que habilita el modo interactivo por servidor. Junto con el
 *      @rendermode="InteractiveServer" de App.razor, son lo que hace que los
 *      botones respondan. Si se quita alguno, las páginas se dibujan pero los
 *      @onclick no hacen nada.
 *
 *   2. DatosService se registra como SINGLETON (AddSingleton), no como Scoped.
 *      Es el almacén de datos en memoria y debe ser uno solo, compartido por
 *      toda la aplicación. La explicación completa está en DatosService.cs.
 */

using AireLibre.Components;
using AireLibre.Services;

var builder = WebApplication.CreateBuilder(args);

// --- Servicios de Blazor ---------------------------------------------------
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// --- Servicios propios del sistema -----------------------------------------

// Almacén de datos en memoria: UNA sola instancia para toda la aplicación.
builder.Services.AddSingleton<DatosService>();

// Servicios de negocio. Van como Scoped (una instancia por conexión de
// usuario) y no como Singleton porque NO guardan estado: solo leen y escriben
// sobre DatosService, que sí es único. Scoped es el registro por defecto
// razonable para un servicio sin estado, y deja abierta la puerta a que más
// adelante dependan de algo propio de cada usuario.
builder.Services.AddScoped<ReservaService>();
builder.Services.AddScoped<ClienteService>();
builder.Services.AddScoped<InventarioService>();

// La sesión SÍ tiene que ser Scoped obligatoriamente, no por convención: como
// singleton habría una sola sesión para toda la aplicación y, apenas un
// profesor iniciara sesión, cualquiera entraría al panel ya autenticado.
// La explicación completa está en SesionService.cs.
builder.Services.AddScoped<SesionService>();

var app = builder.Build();

// --- Canal de peticiones HTTP ----------------------------------------------
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
