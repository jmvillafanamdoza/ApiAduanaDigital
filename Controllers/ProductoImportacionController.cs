using AduanaDigital.Data;
using AduanaDigital.Models;
using Microsoft.AspNetCore.Mvc;

namespace AduanaDigital.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductoImportacionController : ControllerBase
    {
        private readonly ProductoImportacionRepository _repository;

        public ProductoImportacionController()
        {
            _repository = new ProductoImportacionRepository();
        }

        [HttpPost]
        public async Task<IActionResult> InsertarProducto([FromBody] ProductoImportacion producto)
        {
            try
            {
                var nuevoId = await _repository.InsertProductoImportacion(producto);
                
                if (nuevoId > 0)
                {
                    return Ok(new 
                    { 
                        mensaje = "Producto insertado correctamente", 
                        exito = true,
                        id = nuevoId 
                    });
                }
                else
                {
                    return BadRequest(new { mensaje = "No se pudo insertar el producto", exito = false });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = $"Error: {ex.Message}", exito = false });
            }
        }
    }
}