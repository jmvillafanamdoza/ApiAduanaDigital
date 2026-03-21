namespace AduanaDigital.Models
{
    public class Cliente
    {
        public int Id { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string? Contacto { get; set; }
        public string? TipoDocumento { get; set; }
        public string? NumeroDocumento { get; set; }
        public string? Celular { get; set; }
        public string? CorreoElectronico { get; set; }
    }

    public class FacturaComercial
    {
        public int Id { get; set; }
        public string Numero { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }
        public string Estado { get; set; } = string.Empty;
        public int CantidadItems { get; set; }
        public decimal TotalCajas { get; set; }
        public decimal TotalCbm { get; set; }
        public decimal TotalRmb { get; set; }
        public int? ClienteId { get; set; }
        public string? ClienteNombre { get; set; }
        public string? ClienteCodigo { get; set; }
        public string? Proveedor { get; set; }
        public List<FacturaDetalleRow> Items { get; set; } = new();
    }

    public class FacturaDetalleRow
    {
        public int Id { get; set; }
        public int FacturaId { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string AutSant { get; set; } = string.Empty;
        public int Ctns { get; set; }
        public int Pcs { get; set; }
        public decimal UnitPrice { get; set; }
        public string ImagenUrl { get; set; } = string.Empty;
        public string? HsCode { get; set; }
    }



    public class CrearClienteRequest
    {
        public string Nombre { get; set; } = string.Empty;
        public string Contacto { get; set; } = string.Empty;
        public string TipoDocumento { get; set; } = string.Empty;
        public string NumeroDocumento { get; set; } = string.Empty;
        public string Celular { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;
    }

    public class PackingListDetalle
    {
        public int PackingListId { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string FotoUrl { get; set; } = string.Empty;
        public string NombreChino { get; set; } = string.Empty;
        public int PcsPorCaja { get; set; }
        public decimal CbmPorCaja { get; set; }
        public string Explicacion { get; set; } = string.Empty;
        public string TiendaContacto { get; set; } = string.Empty;
        public string NombreEspanol { get; set; } = string.Empty;
        public string AutSant { get; set; } = string.Empty;
        public int TotalCajas { get; set; }
        public decimal TotalCbm { get; set; }
        public decimal TotalRmb { get; set; }
    }

    public class PackingListResumen
    {
        public int Id { get; set; }
        public string NombreArchivo { get; set; } = string.Empty;
        public DateTime FechaCarga { get; set; }
        public int TotalItems { get; set; }
    }
}

/// <summary>
/// Ítem agrupado del packing list para previsualizar antes de generar factura
/// </summary>
public class PackingListItemAgrupado
{
    public string Codigo { get; set; } = string.Empty;
    public string NombreEspanol { get; set; } = string.Empty;
    public string AutSant { get; set; } = string.Empty;
    public decimal TotalCajas { get; set; }
    public decimal PcsPorCaja { get; set; }
    public decimal CbmPorCaja { get; set; }
    public decimal TotalCbm { get; set; }
    public decimal TotalRmb { get; set; }
    public int Frecuencia { get; set; }
    public string FotoUrl { get; set; } = string.Empty;
}

/// <summary>
/// Request para generar factura desde packing list
/// </summary>
public class GenerarFacturaRequest
{
    public int ClienteId { get; set; }
    public int ProveedorId { get; set; }
    public int PackingListId { get; set; }
    public DateTime Fecha { get; set; } = DateTime.Now;
}