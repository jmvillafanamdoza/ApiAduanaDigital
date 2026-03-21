using System.Diagnostics;
using AduanaDigital.Data;
using AduanaDigital.Services;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

var builder = WebApplication.CreateBuilder(args);

// LOGGING
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Inicializar ConnectionFactory con la configuración
ConnectionFactory.Initialize(builder.Configuration);

// ── Registrar servicios ──────────────────────────────────────
builder.Services.AddSingleton<ProductoImportacionRepository>();
builder.Services.AddSingleton<PackingListRepository>();
builder.Services.AddSingleton<FacturaComercialRepository>();
builder.Services.AddSingleton<ProveedorRepository>();
builder.Services.AddScoped<ExcelImportService>();

// Configurar límite de tamaño de archivos
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 100_000_000;
    options.ValueLengthLimit = int.MaxValue;
    options.MultipartHeadersLengthLimit = int.MaxValue;
});

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 100_000_000;
});

// ── CORS ─────────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:5173",
                "http://localhost:3000",
                "https://localhost:44361"
            )
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Aduana Digital API",
        Version = "v1",
        Description = "API para gestión de productos de importación"
    });
    c.OperationFilter<FileUploadOperationFilter>();

    // Resolver conflictos de rutas en Swagger (por si acaso)
    c.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());
});

var app = builder.Build();

// Global exception handler
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var feature = context.Features.Get<IExceptionHandlerFeature>();
        var ex = feature?.Error;
        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;
        var logger = context.RequestServices.GetRequiredService<ILoggerFactory>()
            .CreateLogger("GlobalException");

        logger.LogError(ex,
            "Unhandled exception. TraceId={TraceId} Path={Path} Method={Method} Query={QueryString}",
            traceId, context.Request.Path, context.Request.Method,
            context.Request.QueryString.ToString());

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new
        {
            error = "Ocurrió un error inesperado",
            traceId,
            path = context.Request.Path.ToString()
        });
    });
});

// Request logging
app.Use(async (context, next) =>
{
    var logger = context.RequestServices.GetRequiredService<ILoggerFactory>()
        .CreateLogger("RequestLogger");
    var traceId = Activity.Current?.Id ?? context.TraceIdentifier;

    logger.LogInformation("Incoming {Method} {Path} TraceId={TraceId} ContentLength={Len}",
        context.Request.Method, context.Request.Path, traceId, context.Request.ContentLength);

    try
    {
        await next();
        logger.LogInformation("Outgoing {StatusCode} TraceId={TraceId}",
            context.Response.StatusCode, traceId);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Request failed TraceId={TraceId}", traceId);
        throw;
    }
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("FrontendPolicy");
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
    Console.Error.WriteLine($"[FATAL] UnhandledException: {e.ExceptionObject}");

TaskScheduler.UnobservedTaskException += (sender, e) =>
{
    Console.Error.WriteLine($"[FATAL] UnobservedTaskException: {e.Exception}");
    e.SetObserved();
};

app.Run();

// ── Filtro Swagger para multipart/form-data ──────────────────
// Maneja IFormFile + parámetros FromForm adicionales (ej: clienteId)
public class FileUploadOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var parameters = context.MethodInfo.GetParameters();

        var fileParams = parameters
            .Where(p => p.ParameterType == typeof(IFormFile))
            .ToList();

        var formParams = parameters
            .Where(p => p.CustomAttributes.Any(a =>
                a.AttributeType.Name == "FromFormAttribute"))
            .ToList();

        // Solo actuar si hay al menos un IFormFile
        if (!fileParams.Any()) return;

        var properties = new Dictionary<string, OpenApiSchema>();
        var required = new HashSet<string>();

        // Agregar IFormFile params como binary
        foreach (var p in fileParams)
        {
            properties[p.Name!] = new OpenApiSchema { Type = "string", Format = "binary" };
            required.Add(p.Name!);
        }

        // Agregar otros FromForm params (ej: clienteId como integer)
        foreach (var p in formParams.Where(p => p.ParameterType != typeof(IFormFile)))
        {
            var schema = p.ParameterType == typeof(int) || p.ParameterType == typeof(long)
                ? new OpenApiSchema { Type = "integer" }
                : new OpenApiSchema { Type = "string" };

            properties[p.Name!] = schema;
        }

        operation.RequestBody = new OpenApiRequestBody
        {
            Content = new Dictionary<string, OpenApiMediaType>
            {
                ["multipart/form-data"] = new OpenApiMediaType
                {
                    Schema = new OpenApiSchema
                    {
                        Type = "object",
                        Properties = properties,
                        Required = required
                    }
                }
            }
        };

        // Limpiar parámetros duplicados que Swagger agrega automáticamente
        var toRemove = operation.Parameters
            .Where(p => properties.ContainsKey(p.Name))
            .ToList();
        foreach (var p in toRemove)
            operation.Parameters.Remove(p);
    }
}