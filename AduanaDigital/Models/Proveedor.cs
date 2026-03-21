namespace AduanaDigital.Models
{
    public class Proveedor
    {
        public int Id { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string? Contacto { get; set; }
        public string? TipoDocumento { get; set; }
        public string? NumeroDocumento { get; set; }
        public string? Celular { get; set; }
        public string? Correo { get; set; }
        public string? PaisOrigen { get; set; }
        public string? Direccion { get; set; }
    }

    public class CrearProveedorRequest
    {
        public string Nombre { get; set; } = string.Empty;
        public string Contacto { get; set; } = string.Empty;
        public string TipoDocumento { get; set; } = string.Empty;
        public string NumeroDocumento { get; set; } = string.Empty;
        public string Celular { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;
        public string PaisOrigen { get; set; } = string.Empty;
        public string Direccion { get; set; } = string.Empty;
    }
}