using AduanaDigital.Data;
using AduanaDigital.Models;
using OfficeOpenXml;
using OfficeOpenXml.Drawing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;

namespace AduanaDigital.Services
{
    public class ExcelImportService
    {
        private readonly PackingListRepository _packingRepo;
        private readonly CloudflareR2Service _r2Service;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ExcelImportService> _logger;

        public ExcelImportService(
            PackingListRepository packingRepo,
            CloudflareR2Service r2Service,
            IConfiguration configuration,
            ILogger<ExcelImportService> logger)
        {
            _packingRepo   = packingRepo;
            _r2Service     = r2Service;
            _configuration = configuration;
            _logger        = logger;
        }

        public async Task<ResultadoImportacion> ImportarProductosDesdeExcel(IFormFile archivoExcel)
        {
            var resultado = new ResultadoImportacion();

            if (archivoExcel == null || archivoExcel.Length == 0)
            {
                resultado.Exitoso = false;
                resultado.Mensaje = "El archivo está vacío o no se recibió correctamente";
                return resultado;
            }

            try
            {
                _logger.LogInformation("Procesando archivo: {FileName} ({Size} bytes)",
                    archivoExcel.FileName, archivoExcel.Length);

                // 1. Crear cabecera del Packing List
                int packingListId = await _packingRepo.CrearPackingList(archivoExcel.FileName);
                resultado.PackingListId = packingListId;

                _logger.LogInformation("Packing List creado con ID: {Id}", packingListId);

                using var stream  = archivoExcel.OpenReadStream();
                using var package = new ExcelPackage(stream);

                if (package.Workbook.Worksheets.Count == 0)
                {
                    resultado.Exitoso = false;
                    resultado.Mensaje = "El archivo Excel no contiene hojas de trabajo";
                    return resultado;
                }

                var worksheet = package.Workbook.Worksheets.First();

                if (worksheet.Dimension == null)
                {
                    resultado.Exitoso = false;
                    resultado.Mensaje = "La hoja de trabajo está vacía";
                    return resultado;
                }

                int rowCount = worksheet.Dimension.Rows;

                if (rowCount < 2)
                {
                    resultado.Exitoso = false;
                    resultado.Mensaje = "El archivo no contiene datos (solo encabezados)";
                    return resultado;
                }

                _logger.LogInformation("Procesando {RowCount} filas", rowCount - 1);

                // 2. Insertar cada fila
                for (int row = 2; row <= rowCount; row++)
                {
                    try
                    {
                        // ── Imagen ────────────────────────────────────────────────────────
                        string fotoUrl = "https://via.placeholder.com/150";

                        try
                        {
                            var picture = worksheet.Drawings
                                .OfType<ExcelPicture>()
                                .FirstOrDefault(p =>
                                    p.From.Row == row - 1 &&
                                    p.From.Column >= 2 && p.From.Column <= 3);

                            if (picture != null)
                            {
                                byte[]? rawBytes = TryGetImageBytes(picture);

                                if (rawBytes != null && rawBytes.Length > 0)
                                {
                                    // Convertir a JPEG para uniformidad
                                    byte[] jpegBytes = ConvertToJpeg(rawBytes);

                                    // Nombre único: codigo_packingId_fila.jpg
                                    var codigo   = worksheet.Cells[row, 2].Value?.ToString() ?? $"item_{row}";
                                    var fileName = $"{SanitizarNombre(codigo)}_{packingListId}_{row}.jpg";

                                    // Subir a Cloudflare R2
                                    var urlSubida = await _r2Service.SubirImagenAsync(jpegBytes, fileName);

                                    if (!string.IsNullOrEmpty(urlSubida))
                                    {
                                        fotoUrl = urlSubida;
                                        _logger.LogInformation("Imagen subida para fila {Row}: {Url}", row, fotoUrl);
                                    }
                                    else
                                    {
                                        _logger.LogWarning("No se pudo subir imagen para fila {Row}, usando placeholder", row);
                                    }
                                }
                            }
                        }
                        catch (Exception exImg)
                        {
                            _logger.LogWarning(exImg, "No se pudo procesar imagen en fila {Row}", row);
                        }
                        // ─────────────────────────────────────────────────────────────────

                        var detalle = new PackingListDetalle
                        {
                            PackingListId  = packingListId,
                            Codigo         = worksheet.Cells[row, 2].Value?.ToString() ?? "",
                            FotoUrl        = fotoUrl,
                            NombreChino    = worksheet.Cells[row, 4].Value?.ToString() ?? "",
                            PcsPorCaja     = int.TryParse(worksheet.Cells[row, 5].Value?.ToString(), out int pcs) ? pcs : 0,
                            CbmPorCaja     = decimal.TryParse(worksheet.Cells[row, 6].Value?.ToString(), out decimal cbm) ? cbm : 0,
                            Explicacion    = worksheet.Cells[row, 7].Value?.ToString() ?? "",
                            TiendaContacto = worksheet.Cells[row, 8].Value?.ToString() ?? "",
                            NombreEspanol  = worksheet.Cells[row, 9].Value?.ToString() ?? "",
                            AutSant        = worksheet.Cells[row, 10].Value?.ToString() ?? "",
                            TotalCajas     = int.TryParse(worksheet.Cells[row, 11].Value?.ToString(), out int cajas) ? cajas : 0,
                            TotalCbm       = decimal.TryParse(worksheet.Cells[row, 12].Value?.ToString(), out decimal ttCbm) ? ttCbm : 0,
                            TotalRmb       = decimal.TryParse(worksheet.Cells[row, 13].Value?.ToString(), out decimal ttRmb) ? ttRmb : 0,
                        };

                        if (string.IsNullOrWhiteSpace(detalle.Codigo))
                        {
                            resultado.RegistrosFallidos++;
                            resultado.Errores.Add($"Fila {row}: El código es obligatorio");
                            continue;
                        }

                        var nuevoId = await _packingRepo.InsertarDetalle(detalle);

                        if (nuevoId > 0)
                        {
                            resultado.RegistrosExitosos++;
                            resultado.IdsInsertados.Add(nuevoId);
                        }
                        else
                        {
                            resultado.RegistrosFallidos++;
                            resultado.Errores.Add($"Fila {row}: No se pudo insertar en BD");
                        }
                    }
                    catch (Exception ex)
                    {
                        resultado.RegistrosFallidos++;
                        resultado.Errores.Add($"Fila {row}: {ex.Message}");
                        _logger.LogError(ex, "Error al procesar fila {Row}", row);
                    }
                }

                resultado.Exitoso = resultado.RegistrosExitosos > 0;
                resultado.Mensaje = $"Importación completada. Exitosos: {resultado.RegistrosExitosos}, Fallidos: {resultado.RegistrosFallidos}";
            }
            catch (Exception ex)
            {
                resultado.Exitoso  = false;
                resultado.Mensaje  = $"Error al procesar el archivo: {ex.Message}";
                _logger.LogError(ex, "Error crítico durante la importación");
            }

            return resultado;
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private byte[]? TryGetImageBytes(ExcelPicture picture)
        {
            try
            {
                return picture.Image?.ImageBytes;
            }
            catch { return null; }
        }

        private byte[] ConvertToJpeg(byte[] rawBytes)
        {
            using var inputMs  = new MemoryStream(rawBytes);
            using var image    = SixLabors.ImageSharp.Image.Load(inputMs);
            using var outputMs = new MemoryStream();
            image.SaveAsJpeg(outputMs, new JpegEncoder { Quality = 85 });
            return outputMs.ToArray();
        }

        /// <summary>
        /// Elimina caracteres inválidos para nombres de archivo/objeto en R2.
        /// </summary>
        private static string SanitizarNombre(string nombre)
        {
            var invalidos = Path.GetInvalidFileNameChars()
                .Concat(new[] { ' ', '#', '?', '&' })
                .ToArray();

            return string.Join("_", nombre.Split(invalidos, StringSplitOptions.RemoveEmptyEntries));
        }
    }

    public class ResultadoImportacion
    {
        public bool Exitoso { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public int RegistrosExitosos { get; set; }
        public int RegistrosFallidos { get; set; }
        public List<int> IdsInsertados { get; set; } = new();
        public List<string> Errores { get; set; } = new();
        public int PackingListId { get; set; }
    }
}