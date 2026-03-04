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

// Registrar servicios en el contenedor de DI
builder.Services.AddSingleton<ProductoImportacionRepository>();
builder.Services.AddScoped<ExcelImportService>();

// Configurar límite de tamaño de archivos
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 100_000_000; // 100MB
    options.ValueLengthLimit = int.MaxValue;
    options.MultipartHeadersLengthLimit = int.MaxValue;
});

// Kestrel request size (extra)
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 100_000_000; // 100MB
});

// Add services to the container.
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
            traceId,
            context.Request.Path,
            context.Request.Method,
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
        context.Request.Method,
        context.Request.Path,
        traceId,
        context.Request.ContentLength);

    try
    {
        await next();
        logger.LogInformation("Outgoing {StatusCode} TraceId={TraceId}",
            context.Response.StatusCode,
            traceId);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Request failed TraceId={TraceId}", traceId);
        throw;
    }
});

// Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Process-level crash capture
AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
{
    Console.Error.WriteLine($"[FATAL] UnhandledException: {e.ExceptionObject}");
};

TaskScheduler.UnobservedTaskException += (sender, e) =>
{
    Console.Error.WriteLine($"[FATAL] UnobservedTaskException: {e.Exception}");
    e.SetObserved();
};

app.Run();

// Filtro personalizado para Swagger que maneja IFormFile
public class FileUploadOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var fileParameters = context.MethodInfo.GetParameters()
            .Where(p => p.ParameterType == typeof(IFormFile))
            .ToList();

        if (fileParameters.Any())
        {
            operation.RequestBody = new OpenApiRequestBody
            {
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    ["multipart/form-data"] = new OpenApiMediaType
                    {
                        Schema = new OpenApiSchema
                        {
                            Type = "object",
                            Properties = fileParameters.ToDictionary(
                                p => p.Name!,
                                p => new OpenApiSchema
                                {
                                    Type = "string",
                                    Format = "binary"
                                }
                            ),
                            Required = new HashSet<string>(fileParameters.Select(p => p.Name!))
                        }
                    }
                }
            };
        }
    }
}