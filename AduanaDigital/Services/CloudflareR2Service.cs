using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;

namespace AduanaDigital.Services
{
    public class CloudflareR2Service
    {
        private readonly AmazonS3Client _s3Client;
        private readonly string _bucketName;
        private readonly string _publicUrl;
        private readonly ILogger<CloudflareR2Service> _logger;

        public CloudflareR2Service(IConfiguration configuration, ILogger<CloudflareR2Service> logger)
        {
            _logger = logger;

            var accountId = configuration["Cloudflare:AccountId"]!;
            var accessKey = configuration["Cloudflare:AccessKeyId"]!;
            var secretKey = configuration["Cloudflare:SecretAccessKey"]!;
            _bucketName = configuration["Cloudflare:BucketName"]!;
            _publicUrl = configuration["Cloudflare:PublicUrl"]!.TrimEnd('/');

            var credentials = new BasicAWSCredentials(accessKey, secretKey);
            var config = new AmazonS3Config
            {
                ServiceURL = $"https://{accountId}.r2.cloudflarestorage.com",
                ForcePathStyle = true,
                AuthenticationRegion = "auto",
            };

            _s3Client = new AmazonS3Client(credentials, config);
        }

        /// <summary>
        /// Sube una imagen a Cloudflare R2 y devuelve la URL pública.
        /// </summary>
        /// <param name="imageBytes">Bytes de la imagen (JPEG recomendado)</param>
        /// <param name="fileName">Nombre del archivo, ej: "KB-417_fila2.jpg"</param>
        /// <returns>URL pública de la imagen subida</returns>
        public async Task<string> SubirImagenAsync(byte[] imageBytes, string fileName)
        {
            try
            {
                var key = $"productos/{fileName}";

                using var stream = new MemoryStream(imageBytes);

                var request = new PutObjectRequest
                {
                    BucketName = _bucketName,
                    Key = key,
                    InputStream = stream,
                    ContentType = "image/jpeg",
                    DisablePayloadSigning = true,  // ← esto soluciona el error con R2
                };

                var response = await _s3Client.PutObjectAsync(request);

                // R2 devuelve 200 o 204 en éxito
                if (response.HttpStatusCode == System.Net.HttpStatusCode.OK ||
                    response.HttpStatusCode == System.Net.HttpStatusCode.NoContent)
                {
                    var url = $"{_publicUrl}/{key}";
                    _logger.LogInformation("Imagen subida a R2: {Url}", url);
                    return url;
                }

                _logger.LogWarning("R2 devolvió status {Status} para {Key}", response.HttpStatusCode, key);
                return string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al subir imagen {FileName} a Cloudflare R2", fileName);
                return string.Empty;
            }
        }
    }
}