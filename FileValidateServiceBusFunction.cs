using System;
using System.Diagnostics;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;


namespace FileValidateHttpTrigger
{
    /// <summary>
    /// ServiceBusTriggerFunction
    /// </summary>
    public class FileValidateServiceBusFunction
    {
        /// <summary>
        /// Trigger Type: Service Bus Queue Trigger- Input: Message from the Service Bus Queue
        /// - Processing: Pickup messages from the queue and process or log them.
        /// </summary>
        /// <param name="myQueueItem"></param>
        /// <param name="log"></param>
        [FunctionName("FileValidateServiceBusFunction")]
        public void ReadFileValidateServiceBusQueue([ServiceBusTrigger("filequeue", Connection = "ConnectionStringServiceBus")]string myQueueItem, ILogger log)
        {
            log.LogInformation($"C# ServiceBus queue trigger function processed message: {myQueueItem}");
            try
            {
                // Deserialize the message using Newtonsoft.Json
                var metadata = JsonConvert.DeserializeObject<FileMetadata>(myQueueItem);

                if (metadata == null)
                {
                    log.LogWarning("Invalid message format received.");
                    return;
                }

                // Validate extracted data
                if (!ValidateMetadata(metadata, log))
                {
                    log.LogWarning("Validation failed for message metadata.");
                    return;
                }

                // Log success response
                var successResponse = new
                {
                    BusinessNumber = metadata.BusinessNumber,
                    Year = metadata.Year,
                    Month = metadata.Month,
                    ReferenceTaskId = metadata.ReferenceTaskId,
                    DocumentType = metadata.DocumentType
                };

                log.LogInformation("Message processed successfully. Success Response: ");
                log.LogInformation(JsonConvert.SerializeObject(successResponse)); // Using Newtonsoft.Json here
            }
            catch (Exception ex)
            {
                log.LogError($"Error processing Service Bus message: {ex.Message}");
            }
        }
        /// <summary>
        /// Validate Json file data
        /// </summary>
        /// <param name="metadata"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        private static bool ValidateMetadata(FileMetadata metadata, ILogger log)
        {
            if (metadata.BusinessNumber.Length != 9)
            {
                log.LogWarning($"Invalid BusinessNumber: {metadata.BusinessNumber}. It must be exactly 9 digits.");
                return false;
            }

            if (metadata.Year.ToString().Length != 4)
            {
                log.LogWarning($"Invalid Year: {metadata.Year}. It must be a 4-digit number.");
                return false;
            }

            if (metadata.Month < 1 || metadata.Month > 12)
            {
                log.LogWarning($"Invalid Month: {metadata.Month}. It must be between 1 and 12.");
                return false;
            }

            if (metadata.ReferenceTaskId.Length != 32)
            {
                log.LogWarning($"Invalid ReferenceTaskId: {metadata.ReferenceTaskId}. It must be exactly 32 characters.");
                return false;
            }


            return true;
        }
    }
    /// <summary>
    /// File Metadata about JSON
    /// </summary>
    public class FileMetadata
    {
        public string BusinessNumber { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public string ReferenceTaskId { get; set; }
        public string DocumentType { get; set; }
    }
}
