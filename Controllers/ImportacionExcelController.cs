using AduanaDigital.Services;
using Microsoft.AspNetCore.Mvc;

namespace AduanaDigital.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ImportacionExcelController : ControllerBase
    {
        private readonly ExcelImportService _excelService;
        private readonly ILogger<ImportacionExcelController> _logger;

        public ImportacionExcelController(
            ExcelImportService excelService,
            ILogger<ImportacionExcelController> logger)
        {
            _excelService = excelService;
            _logger = logger;
        }

        /// <summary>
        /// Importa productos desde un archivo Excel
        /// </summary>
        /// <param name="archivo">Archivo Excel (.xlsx o .xls)</param>
        /// <returns>Resultado de la importación</returns>
        [HttpPost("importar")]
        [RequestSizeLimit(100_000_000)] // 100MB
        public async Task<IActionResult> ImportarDesdeExcel(IFormFile archivo)  // SIN [FromForm]
        {
            if (archivo == null || archivo.Length == 0)
            {
                return BadRequest(new
                {
                    mensaje = "El archivo es requerido",
                    exito = false
                });
            }

            // Validar extensión del archivo
            var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
            if (extension != ".xlsx" && extension != ".xls")
            {
                return BadRequest(new
                {
                    mensaje = "Solo se permiten archivos Excel (.xlsx, .xls)",
                    exito = false
                });
            }

            // Validar tamaño (50MB)
            if (archivo.Length > 50_000_000)
            {
                return BadRequest(new
                {
                    mensaje = "El archivo excede el tamaño máximo permitido (50MB)",
                    exito = false
                });
            }

            try
            {
                _logger.LogInformation(
                    "Iniciando importación de archivo: {FileName} ({Size} KB)",
                    archivo.FileName,
                    archivo.Length / 1024
                );

                var resultado = await _excelService.ImportarProductosDesdeExcel(archivo);

                _logger.LogInformation(
                    "Importación completada. Exitosos: {Exitosos}, Fallidos: {Fallidos}",
                    resultado.RegistrosExitosos,
                    resultado.RegistrosFallidos
                );

                if (resultado.Exitoso)
                {
                    return Ok(resultado);
                }
                else
                {
                    return BadRequest(resultado);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error durante la importación del archivo {FileName}", archivo.FileName);
                return StatusCode(500, new
                {
                    mensaje = $"Error al procesar el archivo: {ex.Message}",
                    exito = false
                });
            }
        }
    }
}