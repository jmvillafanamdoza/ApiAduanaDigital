using Microsoft.AspNetCore.Mvc;

namespace AduanaDigital.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DocumentController : ControllerBase
    {
        private readonly ILogger<DocumentController> _logger;

        public DocumentController(ILogger<DocumentController> logger)
        {
            _logger = logger;
        }

        /// GET /api/Document/health
        [HttpGet("health")]
        public IActionResult Health()
        {
            return Ok(new { exito = true, mensaje = "API Aduana Digital activa" });
        }

        /// POST /api/Document/upload
        [HttpPost("upload")]
        [RequestSizeLimit(50_000_000)]
        public IActionResult Upload(IFormFile archivo)
        {
            if (archivo == null || archivo.Length == 0)
                return BadRequest(new { exito = false, mensaje = "El archivo es requerido" });

            _logger.LogInformation("Archivo recibido: {FileName} ({Size} bytes)", archivo.FileName, archivo.Length);

            return Ok(new
            {
                exito = true,
                mensaje = "Archivo recibido correctamente",
                archivo = archivo.FileName,
                tamanio = archivo.Length
            });
        }
    }
}