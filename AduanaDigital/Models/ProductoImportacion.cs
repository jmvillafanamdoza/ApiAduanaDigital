namespace AduanaDigital.Models
{
    public class ProductoImportacion
    {
        public string Codigo { get; set; }
        public string FotoUrl { get; set; }
        public string NombreChino { get; set; }
        public int PcsPorCaja { get; set; }
        public decimal CbmPorCaja { get; set; }
        public string Explicacion { get; set; }
        public string TiendaContacto { get; set; }
        public string NombreEspanol { get; set; }
        public string AutSant { get; set; }
        public int TotalCajas { get; set; }
        public decimal TotalCbm { get; set; }
        public decimal TotalRmb { get; set; }
    }
}