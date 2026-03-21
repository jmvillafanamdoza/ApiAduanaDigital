using AduanaDigital.Data;
using AduanaDigital.Models;
using Microsoft.AspNetCore.Mvc;

namespace AduanaDigital.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProveedoresController : ControllerBase
    {
        private readonly ProveedorRepository _repo;
        private readonly ILogger<ProveedoresController> _logger;

        public ProveedoresController(ProveedorRepository repo, ILogger<ProveedoresController> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        /// GET /api/Proveedores
        [HttpGet]
        public async Task<IActionResult> GetProveedores()
        {
            try
            {
                var proveedores = await _repo.ObtenerProveedores();
                return Ok(proveedores);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener proveedores");
                return StatusCode(500, new { exito = false, mensaje = ex.Message });
            }
        }

        /// POST /api/Proveedores
        [HttpPost]
        public async Task<IActionResult> CrearProveedor([FromBody] CrearProveedorRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Nombre))
                return BadRequest(new { exito = false, mensaje = "El nombre es obligatorio" });

            try
            {
                var proveedor = await _repo.InsertarProveedor(request);
                if (proveedor == null)
                    return StatusCode(500, new { exito = false, mensaje = "No se pudo crear el proveedor" });

                _logger.LogInformation("Proveedor creado: {Nombre} ({Codigo})", proveedor.Nombre, proveedor.Codigo);
                return Ok(proveedor);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear proveedor");
                var mensaje = ex.Message.Contains("Ya existe") ? ex.Message : "Error al crear el proveedor";
                return StatusCode(500, new { exito = false, mensaje });
            }
        }
    }
}