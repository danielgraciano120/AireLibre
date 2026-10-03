/*
 * Program.cs — Arranque de la API REST de STRONGBODY.
 *
 * QUÉ HACE: levanta una Web API aparte del sitio Blazor, con Swagger para
 * probar cada endpoint desde el navegador (https://localhost:7062/swagger).
 *
 * DE DÓNDE SALEN LOS DATOS Y LAS REGLAS: de los MISMOS Models/ y Services/ del
 * proyecto AireLibre, enlazados desde el .csproj. La API no tiene reglas
 * propias: los controladores solo traducen HTTP <-> servicio, igual que las
 * vistas .razor solo traducen clics <-> servicio.
 *
 * OJO: el almacén sigue siendo memoria (DatosService). La API corre en su
 * propio proceso, así que tiene SU PROPIA copia de los datos de ejemplo: lo
 * que se cree desde Swagger no aparece en el sitio Blazor y al revés.
 * Compartir datos exige una base de datos (la migración a EF Core pendiente).
 */

using System.Text.Json.Serialization;
using AireLibre.Api.Swagger;
using AireLibre.Services;

var builder = WebApplication.CreateBuilder(args);

// --- Servicios del dominio (mismo registro que en el sitio Blazor) ---------
// DatosService es SINGLETON: un solo almacén para todas las peticiones.
builder.Services.AddSingleton<DatosService>();
builder.Services.AddScoped<ReservaService>();
builder.Services.AddScoped<ClienteService>();
builder.Services.AddScoped<InventarioService>();
builder.Services.AddScoped<SesionService>();

// --- API REST ---------------------------------------------------------------
builder.Services
    .AddControllers()
    .AddJsonOptions(opciones =>
    {
        // Los enums viajan como texto ("Cedula", "Manana") y no como números
        opciones.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddSwaggerDocumentacion();

// CORS abierto para poder consumir la API desde otro origen durante el desarrollo
builder.Services.AddCors(opciones =>
    opciones.AddDefaultPolicy(politica =>
        politica.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

// Swagger siempre activo: es la herramienta para probar la API
app.UseSwaggerDocumentacion();

app.UseHttpsRedirection();
app.UseCors();
app.MapControllers();

// La raíz manda directo a Swagger
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

app.Run();
