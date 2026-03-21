using AduanaDigital.Data;
using AduanaDigital.Models;
using Microsoft.AspNetCore.Mvc;

namespace AduanaDigital.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ClientesController : ControllerBase
    {
        private readonly FacturaComercialRepository _repo;
        private readonly ILogger<ClientesController> _logger;

        public ClientesController(FacturaComercialRepository repo, ILogger<ClientesController> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        /// GET /api/Clientes
        [HttpGet]
        public async Task<IActionResult> GetClientes()
        {
            try
            {
                var clientes = await _repo.ObtenerClientes();
                return Ok(clientes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener clientes");
                return StatusCode(500, new { exito = false, mensaje = ex.Message });
            }
        }

        /// POST /api/Clientes
        [HttpPost]
        public async Task<IActionResult> CrearCliente([FromBody] CrearClienteRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Nombre))
                return BadRequest(new { exito = false, mensaje = "El nombre es obligatorio" });

            if (string.IsNullOrWhiteSpace(request.NumeroDocumento))
                return BadRequest(new { exito = false, mensaje = "El número de documento es obligatorio" });

            try
            {
                var cliente = await _repo.InsertarCliente(request);
                if (cliente == null)
                    return StatusCode(500, new { exito = false, mensaje = "No se pudo crear el cliente" });

                _logger.LogInformation("Cliente creado: {Nombre} ({Codigo})", cliente.Nombre, cliente.Codigo);
                return Ok(cliente);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear cliente");
                // Mensaje amigable para documentos duplicados
                var mensaje = ex.Message.Contains("Ya existe")
                    ? ex.Message
                    : "Error al crear el cliente";
                return StatusCode(500, new { exito = false, mensaje });
            }
        }
    }
}