using AduanaDigital.Data;
using AduanaDigital.Models;
using Microsoft.AspNetCore.Http;
using OfficeOpenXml;
using OfficeOpenXml.Drawing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;

namespace AduanaDigital.Services
{
    public class ExcelImportService
    {
        private readonly ProductoImportacionRepository _repository;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ExcelImportService> _logger;

        public ExcelImportService(
            ProductoImportacionRepository repository,
            IConfiguration configuration,
            ILogger<ExcelImportService> _logger)
        {
            _repository = repository;
            _configuration = configuration;
            this._logger = _logger;
        }

        /// <summary>
        /// Importa productos desde un archivo Excel enviado como FormData
        /// </summary>
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

                using (var stream = archivoExcel.OpenReadStream())
                using (var package = new ExcelPackage(stream))
                {
                    if (package.Workbook.Worksheets.Count == 0)
                    {
                        resultado.Exitoso = false;
                        resultado.Mensaje = "El archivo Excel no contiene hojas de trabajo";
                        return resultado;
                    }

                    // Usar First() en lugar de índice para mayor compatibilidad
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

                    // Procesar cada fila
                    for (int row = 2; row <= rowCount; row++)
                    {
                        try
                        {
                            string fotoUrl = "https://via.placeholder.com/150";

                            // Intentar extraer y subir la imagen
                            try
                            {
                                var picture = worksheet.Drawings
                                    .OfType<ExcelPicture>()
                                    .FirstOrDefault(p =>
                                        p.From.Row == row - 1 &&
                                        p.From.Column >= 2 && p.From.Column <= 3);

                                if (picture?.Image != null)
                                {
                                    string codigo = worksheet.Cells[row, 2].Value?.ToString() ?? Guid.NewGuid().ToString();
                                    string fileName = $"{codigo}.jpg";

                                    // Convertir imagen a bytes usando ImageSharp
                                    byte[] imageBytes = ConvertImageToBytes(picture.Image);

                                    // Aquí puedes implementar la subida a Azure o Cloudinary
                                    // fotoUrl = await _storageService.SubirImagenDesdeBytesAsync(imageBytes, fileName);
                                    
                                    _logger.LogInformation("Imagen encontrada en fila {Row} ({Size} bytes)", row, imageBytes.Length);
                                }
                            }
                            catch (Exception exImg)
                            {
                                _logger.LogWarning(exImg, "No se pudo procesar imagen en fila {Row}", row);
                            }

                            var producto = new ProductoImportacion
                            {
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

                            // Validar que al menos tenga código
                            if (string.IsNullOrWhiteSpace(producto.Codigo))
                            {
                                resultado.RegistrosFallidos++;
                                resultado.Errores.Add($"Fila {row}: El código es obligatorio");
                                continue;
                            }

                            // Insertar el producto
                            var nuevoId = await _repository.InsertProductoImportacion(producto);

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
            }
            catch (Exception ex)
            {
                resultado.Exitoso = false;
                resultado.Mensaje = $"Error al procesar el archivo: {ex.Message}";
                _logger.LogError(ex, "Error crítico durante la importación");
            }

            return resultado;
        }

        /// <summary>
        /// Convierte un objeto Image de System.Drawing a bytes usando ImageSharp
        /// </summary>
        private byte[] ConvertImageToBytes(System.Drawing.Image drawingImage)
        {
            using (var ms = new MemoryStream())
            {
                // Guardar la imagen de System.Drawing en un stream temporal
                drawingImage.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                ms.Position = 0;

                // Cargar con ImageSharp y volver a guardar
                using (var image = SixLabors.ImageSharp.Image.Load(ms))
                {
                    using (var outputMs = new MemoryStream())
                    {
                        image.SaveAsJpeg(outputMs);
                        return outputMs.ToArray();
                    }
                }
            }
        }
    }

    public class ResultadoImportacion
    {
        public bool Exitoso { get; set; }
        public string Mensaje { get; set; }
        public int RegistrosExitosos { get; set; }
        public int RegistrosFallidos { get; set; }
        public List<int> IdsInsertados { get; set; } = new();
        public List<string> Errores { get; set; } = new();
    }
}