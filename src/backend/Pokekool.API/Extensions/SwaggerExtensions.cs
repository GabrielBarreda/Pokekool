using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Pokekool.API.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Pokekool.API.Extensions;

public static class SwaggerExtensions
{
    public static IServiceCollection AddPokekoolSwagger(this IServiceCollection services)
    {
        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
        })
        .AddApiExplorer(opts =>
        {
            opts.GroupNameFormat = "'v'VVV";
            opts.SubstituteApiVersionInUrl = true;
        });

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            // Évite un 500 quand deux types ont le même nom simple dans des namespaces différents
            // (ex. PagedResult<X> vs PagedResult<Y> résolus au même schéma).
            options.CustomSchemaIds(type => type.FullName);

            // Évite un 500 si deux actions ont le même operationId généré — garde la première.
            options.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());
        });
        services.TryAddEnumerable(ServiceDescriptor.Transient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>());

        return services;
    }

    public static WebApplication UsePokekoolSwagger(this WebApplication app)
    {
        var provider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();

        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            foreach (var description in provider.ApiVersionDescriptions)
            {
                options.SwaggerEndpoint(
                    $"/swagger/{description.GroupName}/swagger.json",
                    $"Pokekool API {description.GroupName.ToUpperInvariant()}");
            }
            options.RoutePrefix = "swagger";
            options.DocumentTitle = "Pokekool API";
            options.DisplayRequestDuration();
            options.EnablePersistAuthorization();
        });

        return app;
    }
}
