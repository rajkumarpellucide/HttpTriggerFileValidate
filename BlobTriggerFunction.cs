using System;
using System.IO;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace FileValidateHttpTrigger
{
    public class BlobTriggerFunction
    {
        [FunctionName("BlobTriggerFunction")]
        public void Run([BlobTrigger("samples-workitems/{name}", Connection = "ConnectionStringBlobTrigger")]Stream myBlob, string name, ILogger log)
        {
            log.LogInformation($"C# Blob trigger function Processed blob\n Name:{name} \n Size: {myBlob.Length} Bytes");
            try
            {
                // Read and deserialize the blob content
                using var reader = new StreamReader(myBlob);
                string blobContent = reader.ReadToEnd();

                // Attempt to deserialize content
                var metadata = JsonConvert.DeserializeObject<BlobMetadata>(blobContent);

                // Log deserialized metadata
                log.LogInformation("Deserialized Metadata:");
                log.LogInformation($"BusinessNumber: {metadata.BusinessNumber}");
                log.LogInformation($"Year: {metadata.Year}");
                log.LogInformation($"Month: {metadata.Month}");
                log.LogInformation($"ReferenceTaskId: {metadata.ReferenceTaskId}");
                log.LogInformation($"DocumentType: {metadata.DocumentType}");
                log.LogInformation($"Extension: {metadata.Extension}");

                // Log success response
                var successResponse = new
                {
                    BusinessNumber = metadata.BusinessNumber,
                    Year = metadata.Year,
                    Month = metadata.Month,
                    ReferenceTaskId = metadata.ReferenceTaskId,
                    DocumentType = metadata.DocumentType,
                    Extension = metadata.Extension
                };

                log.LogInformation("Message processed successfully. Success Response: ");
                log.LogInformation(JsonConvert.SerializeObject(successResponse)); // Using Newtonsoft.Json here
            }
            catch (JsonException ex)
            {
                log.LogError($"Failed to deserialize blob content: {ex.Message}");
            }
            catch (Exception ex)
            {
                log.LogError($"An error occurred while processing the blob: {ex.Message}");
            }
        }
        public class BlobMetadata
        {
            public string BusinessNumber { get; set; }
            public int Year { get; set; }
            public int Month { get; set; }
            public string ReferenceTaskId { get; set; }
            public string DocumentType { get; set; }
            public string Extension { get; set; }
        }
    }
}
