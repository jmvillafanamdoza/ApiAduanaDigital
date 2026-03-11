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

        [HttpGet("health")]
        public IActionResult Health()
        {
            return Ok(new
            {
                exito = true,
                mensaje = "DocumentController activo"
            });
        }

        [HttpPost("upload")]
        [RequestSizeLimit(50_000_000)] // 50MB
        public IActionResult Upload(IFormFile archivo)
        {
            if (archivo == null || archivo.Length == 0)
            {
                return BadRequest(new
                {
                    exito = false,
                    mensaje = "El archivo es requerido"
                });
            }

            _logger.LogInformation(
                "Archivo recibido en DocumentController: {FileName} ({Size} bytes)",
                archivo.FileName,
                archivo.Length
            );

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