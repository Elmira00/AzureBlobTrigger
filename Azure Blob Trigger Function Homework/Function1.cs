using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace Azure_Blob_Trigger_Function_Homework
{
    public class Function1
    {

        private readonly ILogger _logger;
        public Function1(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<Function1>();
        }

        [Function("ProcessPngToJpg")]
        public async Task Run(
            [BlobTrigger("images/{name}", Connection = "AzureStorage")] Stream inputBlob,
            string name,
            FunctionContext context)
        {
            _logger.LogInformation($"Blob trigger function processed blob: {name}");

            if (Path.GetExtension(name).ToLower() == ".png")
            {
                try
                {
                    string connectionString = Environment.GetEnvironmentVariable("AzureStorage");
                    var blobClient = new BlobServiceClient(connectionString);
                    var containerClient = blobClient.GetBlobContainerClient("images");

                    string newBlobName = Path.ChangeExtension(name, ".jpg");

                    using (var outputStream = new MemoryStream())
                    {
                        using (var image = await Image.LoadAsync(inputBlob))
                        {
                            var jpegEncoder = new JpegEncoder
                            {
                                Quality = 75
                            };
                            image.Mutate(x => x.AutoOrient());
                            await image.SaveAsync(outputStream, jpegEncoder);
                        }

                        outputStream.Position = 0;

                        var newBlobClient = containerClient.GetBlobClient(newBlobName);
                        await newBlobClient.UploadAsync(outputStream, overwrite: true);

                        _logger.LogInformation($"Converted {name} to {newBlobName} and saved successfully!");

                        var oldBlobClient = containerClient.GetBlobClient(name);
                        await oldBlobClient.DeleteIfExistsAsync();
                        _logger.LogInformation($"Has deleted original PNG blob: {name},successfully!");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error processing blob {name}: {ex.Message}");
                    throw;
                }
            }
            else
            {
                _logger.LogInformation($"Blob {name} has an issue");
            }
        }
    }

}