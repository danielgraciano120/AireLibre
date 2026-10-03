/*
 * SwaggerConfiguracion.cs — Configuración de Swagger / OpenAPI.
 *
 * Toma los comentarios /// <summary> de los controladores y de los contratos
 * (GenerateDocumentationFile=true en el .csproj) y los muestra como
 * descripciones en la página de Swagger.
 */

using System.Reflection;
using Microsoft.OpenApi.Models;

namespace AireLibre.Api.Swagger;

public static class SwaggerConfiguracion
{
    public static IServiceCollection AddSwaggerDocumentacion(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(opciones =>
        {
            opciones.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "STRONGBODY API",
                Version = "v1",
                Description = "API REST del grupo de CrossFit al aire libre STRONGBODY: grupos y horarios, " +
                              "reservas de cupo (máximo 20 por clase), planes, solicitudes, clientes, " +
                              "control mensual, pagos, inventario y notificaciones."
            });

            var archivoXml = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var rutaXml = Path.Combine(AppContext.BaseDirectory, archivoXml);
            if (File.Exists(rutaXml))
            {
                opciones.IncludeXmlComments(rutaXml);
            }
        });

        return services;
    }

    public static WebApplication UseSwaggerDocumentacion(this WebApplication app)
    {
        app.UseSwagger();
        app.UseSwaggerUI(opciones =>
        {
            opciones.SwaggerEndpoint("/swagger/v1/swagger.json", "STRONGBODY API v1");
            opciones.DocumentTitle = "STRONGBODY API";
        });

        return app;
    }
}
