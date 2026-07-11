using System.Text.Json.Serialization;
using Pokekool.API.Extensions;
using Pokekool.API.Middleware;
using Pokekool.Application;
using Pokekool.Infrastructure;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// =====================================================
// Logging (Serilog)
// =====================================================
builder.Host.UseSerilog((ctx, sp, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console());

// =====================================================
// Services
// =====================================================
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddDbInitializerOnStartup();
}

builder.Services
    .AddControllers()
    .AddJsonOptions(opt =>
    {
        opt.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        opt.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddPokekoolSwagger();
builder.Services.AddPokekoolHealthChecks(builder.Configuration);
builder.Services.AddPokekoolAuthentication(builder.Configuration);

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? Array.Empty<string>();
builder.Services.AddCors(opt => opt.AddPolicy("default", p =>
    p.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

// =====================================================
// Pipeline HTTP
// =====================================================
var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UsePokekoolSwagger();
    app.MapGet("/", () => Results.Redirect("/swagger", permanent: false))
       .ExcludeFromDescription();
}

// Sert les modèles 3D (.glb) compilés dans wwwroot/models (module Stockage Physique + PLV boutique).
// .glb/.gltf ne font pas partie des types MIME connus par défaut → sinon 404 sur ces fichiers.
var contentTypeProvider = new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider();
contentTypeProvider.Mappings[".glb"] = "model/gltf-binary";
contentTypeProvider.Mappings[".gltf"] = "model/gltf+json";
app.UseStaticFiles(new Microsoft.AspNetCore.Builder.StaticFileOptions { ContentTypeProvider = contentTypeProvider });

app.UseCors("default");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapPokekoolHealthChecks();

Log.Information("Pokekool.API starting in {Environment}", app.Environment.EnvironmentName);
app.Run();

public partial class Program;
