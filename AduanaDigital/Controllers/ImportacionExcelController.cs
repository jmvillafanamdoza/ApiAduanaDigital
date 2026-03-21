using AduanaDigital.Data;
using AduanaDigital.Services;
using Microsoft.AspNetCore.Mvc;

namespace AduanaDigital.Controllers
{
    /// DTO para recibir archivo + clienteId en un solo FromForm
    public class ImportarExcelRequest
    {
        public IFormFile Archivo { get; set; } = null!;
        public int ClienteId { get; set; }
    }

    [ApiController]
    [Route("api/[controller]")]
    public class ImportacionExcelController : ControllerBase
    {
        private readonly ExcelImportService _excelService;
        private readonly FacturaComercialRepository _facturaRepo;
        private readonly ILogger<ImportacionExcelController> _logger;

        public ImportacionExcelController(
            ExcelImportService excelService,
            FacturaComercialRepository facturaRepo,
            ILogger<ImportacionExcelController> logger)
        {
            _excelService = excelService;
            _facturaRepo = facturaRepo;
            _logger = logger;
        }

        /// POST /api/ImportacionExcel/importar
        [HttpPost("importar")]
        [RequestSizeLimit(100_000_000)]
        public async Task<IActionResult> Importar([FromForm] ImportarExcelRequest request)
        {
            if (request.Archivo == null || request.Archivo.Length == 0)
                return BadRequest(new { exito = false, mensaje = "El archivo es requerido" });

            var extension = Path.GetExtension(request.Archivo.FileName).ToLowerInvariant();
            if (extension != ".xlsx" && extension != ".xls")
                return BadRequest(new { exito = false, mensaje = "Solo se permiten archivos Excel (.xlsx, .xls)" });

            if (request.Archivo.Length > 50_000_000)
                return BadRequest(new { exito = false, mensaje = "El archivo excede el tamaño máximo (50MB)" });

            if (request.ClienteId <= 0)
                return BadRequest(new { exito = false, mensaje = "El clienteId es obligatorio" });

            try
            {
                _logger.LogInformation("Importando: {FileName} para cliente {ClienteId}",
                    request.Archivo.FileName, request.ClienteId);

                var resultado = await _excelService.ImportarProductosDesdeExcel(request.Archivo);

                if (resultado.Exitoso && resultado.PackingListId > 0)
                {
                    await _facturaRepo.AsociarClientePackingList(request.ClienteId, resultado.PackingListId);
                    _logger.LogInformation("Packing List {Id} asociado al cliente {ClienteId}",
                        resultado.PackingListId, request.ClienteId);
                }

                return resultado.Exitoso ? Ok(resultado) : BadRequest(resultado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al importar {FileName}", request.Archivo.FileName);
                return StatusCode(500, new { exito = false, mensaje = $"Error: {ex.Message}" });
            }
        }
    }
}