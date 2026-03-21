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
        private readonly IConfiguration _configuration;
        private readonly ILogger<ExcelImportService> _logger;

        public ExcelImportService(
            PackingListRepository packingRepo,
            IConfiguration configuration,
            ILogger<ExcelImportService> logger)
        {
            _packingRepo = packingRepo;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<ResultadoImportacion>  ImportarProductosDesdeExcel(IFormFile archivoExcel)
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

                using var stream = archivoExcel.OpenReadStream();
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

                // 2. Insertar cada fila en TADI_PACKING_LIST_DETALLE
                for (int row = 2; row <= rowCount; row++)
                {
                    try
                    {
                        string fotoUrl = "https://via.placeholder.com/150";

                        // Intentar extraer imagen (sin romper el flujo si falla)
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
                                    // TODO: subir a Azure/Cloudinary y asignar a fotoUrl
                                    _logger.LogInformation("Imagen encontrada en fila {Row}", row);
                                }
                            }
                        }
                        catch (Exception exImg)
                        {
                            _logger.LogWarning(exImg, "No se pudo procesar imagen en fila {Row}", row);
                        }

                        var detalle = new PackingListDetalle
                        {
                            PackingListId = packingListId,
                            Codigo = worksheet.Cells[row, 2].Value?.ToString() ?? "",
                            FotoUrl = fotoUrl,
                            NombreChino = worksheet.Cells[row, 4].Value?.ToString() ?? "",
                            PcsPorCaja = int.TryParse(worksheet.Cells[row, 5].Value?.ToString(), out int pcs) ? pcs : 0,
                            CbmPorCaja = decimal.TryParse(worksheet.Cells[row, 6].Value?.ToString(), out decimal cbm) ? cbm : 0,
                            Explicacion = worksheet.Cells[row, 7].Value?.ToString() ?? "",
                            TiendaContacto = worksheet.Cells[row, 8].Value?.ToString() ?? "",
                            NombreEspanol = worksheet.Cells[row, 9].Value?.ToString() ?? "",
                            AutSant = worksheet.Cells[row, 10].Value?.ToString() ?? "",
                            TotalCajas = int.TryParse(worksheet.Cells[row, 11].Value?.ToString(), out int cajas) ? cajas : 0,
                            TotalCbm = decimal.TryParse(worksheet.Cells[row, 12].Value?.ToString(), out decimal ttCbm) ? ttCbm : 0,
                            TotalRmb = decimal.TryParse(worksheet.Cells[row, 13].Value?.ToString(), out decimal ttRmb) ? ttRmb : 0
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
                resultado.Exitoso = false;
                resultado.Mensaje = $"Error al procesar el archivo: {ex.Message}";
                _logger.LogError(ex, "Error crítico durante la importación");
            }

            return resultado;
        }

        private byte[]? TryGetImageBytes(ExcelPicture picture)
        {
            try
            {
                var imageObj = picture.Image;
                if (imageObj == null) return null;

                var prop = imageObj.GetType().GetProperty("ImageBytes");
                if (prop != null) return prop.GetValue(imageObj) as byte[];

                var streamProp = imageObj.GetType().GetProperty("Stream");
                if (streamProp?.GetValue(imageObj) is Stream imgStream)
                {
                    using var ms = new MemoryStream();
                    imgStream.CopyTo(ms);
                    return ms.ToArray();
                }

                return null;
            }
            catch { return null; }
        }

        private byte[] ConvertToJpeg(byte[] rawBytes)
        {
            using var inputMs = new MemoryStream(rawBytes);
            using var image = SixLabors.ImageSharp.Image.Load(inputMs);
            using var outputMs = new MemoryStream();
            image.SaveAsJpeg(outputMs);
            return outputMs.ToArray();
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