using AduanaDigital.Data;
using AduanaDigital.Models;
using Microsoft.AspNetCore.Mvc;

namespace AduanaDigital.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FacturasComercialesController : ControllerBase
    {
        private readonly FacturaComercialRepository _repo;
        private readonly ILogger<FacturasComercialesController> _logger;

        public FacturasComercialesController(FacturaComercialRepository repo, ILogger<FacturasComercialesController> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        /// GET /api/FacturasComerciales
        [HttpGet]
        public async Task<IActionResult> GetFacturas()
        {
            try { return Ok(await _repo.ObtenerFacturasComerciales()); }
            catch (Exception ex) { return StatusCode(500, new { exito = false, mensaje = ex.Message }); }
        }

        /// GET /api/FacturasComerciales/cliente/{clienteId}
        [HttpGet("cliente/{clienteId}")]
        public async Task<IActionResult> GetPorCliente(int clienteId)
        {
            try
            {
                var packingLists = await _repo.ObtenerPackingListsPorCliente(clienteId);
                var facturas = await _repo.ObtenerFacturasPorCliente(clienteId);
                return Ok(new { packingLists, facturas });
            }
            catch (Exception ex) { return StatusCode(500, new { exito = false, mensaje = ex.Message }); }
        }

        /// GET /api/FacturasComerciales/packing/{packingListId}/items
        /// Preview de ítems agrupados por código antes de generar la factura
        [HttpGet("packing/{packingListId}/items")]
        public async Task<IActionResult> GetItemsPackingList(int packingListId)
        {
            try
            {
                var items = await _repo.ObtenerItemsPackingListAgrupados(packingListId);
                return Ok(items);
            }
            catch (Exception ex) { return StatusCode(500, new { exito = false, mensaje = ex.Message }); }
        }

        /// POST /api/FacturasComerciales/generar
        /// Genera factura comercial completa desde un packing list
        [HttpPost("generar")]
        public async Task<IActionResult> GenerarFactura([FromBody] GenerarFacturaRequest request)
        {
            if (request.ClienteId <= 0)
                return BadRequest(new { exito = false, mensaje = "El clienteId es obligatorio" });
            if (request.ProveedorId <= 0)
                return BadRequest(new { exito = false, mensaje = "El proveedorId es obligatorio" });
            if (request.PackingListId <= 0)
                return BadRequest(new { exito = false, mensaje = "El packingListId es obligatorio" });

            try
            {
                var factura = await _repo.GenerarFacturaDesdePackingList(request);
                if (factura == null)
                    return StatusCode(500, new { exito = false, mensaje = "No se pudo generar la factura" });

                _logger.LogInformation("Factura generada: {Numero} desde PL {PackingListId}",
                    factura.Numero, request.PackingListId);
                return Ok(factura);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar factura desde packing list {Id}", request.PackingListId);
                return StatusCode(500, new { exito = false, mensaje = ex.Message });
            }
        }



        /// POST /api/FacturasComerciales/{id}/items
        [HttpPost("{id}/items")]
        public async Task<IActionResult> AgregarItem(int id, [FromBody] FacturaDetalleRow item)
        {
            if (id <= 0) return BadRequest(new { exito = false, mensaje = "ID de factura inválido" });
            if (string.IsNullOrWhiteSpace(item.Codigo))
                return BadRequest(new { exito = false, mensaje = "El código del ítem es obligatorio" });
            try
            {
                item.FacturaId = id;
                var nuevoId = await _repo.InsertarDetalleFactura(item);
                if (nuevoId <= 0)
                    return StatusCode(500, new { exito = false, mensaje = "No se pudo insertar el ítem" });
                item.Id = nuevoId;
                return Ok(item);
            }
            catch (Exception ex) { return StatusCode(500, new { exito = false, mensaje = ex.Message }); }
        }
    }
}