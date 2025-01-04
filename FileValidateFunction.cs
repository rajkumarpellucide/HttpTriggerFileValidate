using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using Azure.Messaging.ServiceBus;

namespace FileValidateHttpTrigger
{
    /// <summary>
    /// HTTP POST trigger and Azure Service Bus.
    /// </summary>
    public static class FileValidateFunction
    {
        /// <summary>
        /// Trigger Type: HTTP POST - Input: JSON Payload - Request: - ContentType: application/json
        /// </summary>
        /// <param name="req"></param>
        /// <param name="log"></param>
        /// <param name="metadataQueue"></param>
        /// <returns></returns>
        [FunctionName("FileValidate")]
        public static async Task<IActionResult> FileValidate(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            ILogger log,
            [ServiceBus("filequeue", Connection = "ConnectionStringServiceBus")] IAsyncCollector<string> metadataQueue)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic fileformatdata = JsonConvert.DeserializeObject(requestBody);
            string filedatamessage = fileformatdata?.FileName;

            if (string.IsNullOrEmpty(filedatamessage))
            {
                return new BadRequestObjectResult("Please provide a valid 'filedatamessage' in the request body.");
            }

            // Regular expression for the expected format
            //var pattern = @"^(?<BusinessNumber>\d+)\.(?<Year>\d{4})\.(?<Month>\d{2})\.(?<ReferenceTaskId>[a-fA-F0-9]{32})\.(?<DocumentType>[A-Z]{3})\.(?<Extension>\w+)$";
            //var pattern = @"^(?<BusinessNumber>\d{9})\.(?<Year>\d{4})\.(?<Month>\d{2})\.(?<ReferenceTaskId>[a-fA-F0-9]{32})\.(?<DocumentType>[A-Z]{3})\.(?<Extension>\w+)$";
            //var pattern = @"^(?<BusinessNumber>\d{15})\.(?<ReferenceTaskId>[a-fA-F0-9]{32})\.(?<DocumentType>[A-Z]{3})\.(?<Extension>pdf|docx)$";
            var pattern = @"^(?<BusinessNumber>\d{15})\.(?<ReferenceTaskId>[a-fA-F0-9]{32})\.(?<DocumentType>[A-Z]{3})\.(?<Extension>pdf)$";
            var match = System.Text.RegularExpressions.Regex.Match(filedatamessage, pattern);

            if (!match.Success)
            {
                return new BadRequestObjectResult($"File name '{filedatamessage}' does not match the expected format '{pattern}'");
            }

            // Extract details from the match
            var businessNumber = match.Groups["BusinessNumber"].Value;
            //var year = int.Parse(match.Groups["Year"].Value);
            //var month = int.Parse(match.Groups["Month"].Value);
            var patternb = @"^(\d{9})(\d{4})(\d{2})";
            var matchb = Regex.Match(businessNumber, patternb);
            string businessnumber = matchb.Groups[1].Value;
            var year = int.Parse(matchb.Groups[2].Value);
            var month = int.Parse(matchb.Groups[3].Value);

            var referenceTaskId = match.Groups["ReferenceTaskId"].Value;
            var documentType = match.Groups["DocumentType"].Value;
            var extension = match.Groups["Extension"].Value;

            // Validate month range (1-12)
            if (month < 1 || month > 12)
            {
                return new BadRequestObjectResult($"Invalid month value: {month} in file name '{filedatamessage}'");
            }

            // Prepare metadata as a JSON message if document type is valid
            if (extension == "pdf" || extension == "docx")
            {
                var metadata = System.Text.Json.JsonSerializer.Serialize(new
                {
                    BusinessNumber = businessnumber,
                    Year = year,
                    Month = month,
                    ReferenceTaskId = referenceTaskId,
                    DocumentType = extension                    
                });

                // Add the message to the Service Bus queue
                await metadataQueue.AddAsync(metadata);

                log.LogInformation($"Message sent to Service Bus queue: {metadata}");
                return new OkObjectResult("File metadata processed and message sent to Service Bus.");
            }
            else
            {
                return new BadRequestObjectResult($"Unsupported document type: {documentType}");
            }
        }
        /// <summary>
        /// Trigger Type: HTTP POST - Input: JSON Payload - Request: - ContentType: application/json
        /// </summary>
        /// <param name="req"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        [FunctionName("ValidFilePost")]
        public static async Task<IActionResult> ValidFilePost(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            // Read the request body
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic fileformatdata = JsonConvert.DeserializeObject(requestBody);
            string filedatamessage = fileformatdata?.FileName;

            if (string.IsNullOrEmpty(filedatamessage))
            {
                log.LogWarning("Invalid request: Missing 'FileName' in the request body.");
                return new BadRequestObjectResult("Please provide a valid 'FileName' in the request body.");
            }

            // Regular expression for the expected format
            //var pattern = @"^(?<BusinessNumber>\d{9})\.(?<Year>\d{4})\.(?<Month>\d{2})\.(?<ReferenceTaskId>[a-fA-F0-9]{32})\.(?<DocumentType>[A-Z]{3})\.(?<Extension>pdf|docx)$";
            var pattern = @"^(?<BusinessNumber>\d{15})\.(?<ReferenceTaskId>[a-fA-F0-9]{32})\.(?<DocumentType>[A-Z]{3})\.(?<Extension>pdf)$";
            var match = System.Text.RegularExpressions.Regex.Match(filedatamessage, pattern);

            if (!match.Success)
            {
                log.LogWarning($"File name '{filedatamessage}' does not match the expected format.");
                return new BadRequestObjectResult($"File name '{filedatamessage}' does not match the expected format.");
            }

            // Extract details from the match
            var businessNumber = match.Groups["BusinessNumber"].Value;

            var patternb = @"^(\d{9})(\d{4})(\d{2})";
            var matchb = Regex.Match(businessNumber, patternb);
            string businessnumber = matchb.Groups[1].Value;
            var year = int.Parse(matchb.Groups[2].Value);
            var month = int.Parse(matchb.Groups[3].Value);

            //var year = int.Parse(match.Groups["Year"].Value);
            //var month = int.Parse(match.Groups["Month"].Value);
            var referenceTaskId = match.Groups["ReferenceTaskId"].Value;
            var documentType = match.Groups["DocumentType"].Value;
            var extension = match.Groups["Extension"].Value;

            // Validate month range (1-12)
            if (month < 1 || month > 12)
            {
                log.LogWarning($"Invalid month value: {month} in file name '{filedatamessage}'.");
                return new BadRequestObjectResult($"Invalid month value: {month} in file name '{filedatamessage}'");
            }

            // Prepare metadata as a JSON message
            var metadata = System.Text.Json.JsonSerializer.Serialize(new
            {
                BusinessNumber = businessnumber,
                Year = year,
                Month = month,
                ReferenceTaskId = referenceTaskId,
                DocumentType = documentType,
                Extension = extension
            });

            // Send the message to Service Bus (Manually using ServiceBusClient)
            string connectionString = Environment.GetEnvironmentVariable("ConnectionStringServiceBus"); // Store your Service Bus connection string in the app settings
            string queueName = "filequeue"; // Replace with your actual queue name

            try
            {
                // Create ServiceBusClient and Sender
                var client = new ServiceBusClient(connectionString);
                var sender = client.CreateSender(queueName);

                // Send message
                await sender.SendMessageAsync(new ServiceBusMessage(metadata));

                log.LogInformation($"Message sent to Service Bus queue: {metadata}");
                return new OkObjectResult("File metadata processed and message sent to Service Bus.");
            }
            catch (Exception ex)
            {
                log.LogError($"Error sending message to Service Bus: {ex.Message}");
                return new StatusCodeResult(500); // Internal Server Error
            }
        }
       
    }
}
