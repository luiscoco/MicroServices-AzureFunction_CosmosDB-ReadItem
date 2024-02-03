using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using System;

namespace CosmosDbCrudFunctions
{
    public static class ReadFunction
    {
        private static readonly string EndpointUri = Environment.GetEnvironmentVariable("CosmosDbEndpointUri");
        private static readonly string PrimaryKey = Environment.GetEnvironmentVariable("CosmosDbPrimaryKey");
        private static readonly string DatabaseName = "ToDoList";
        private static readonly string ContainerName = "Items";
        private static CosmosClient cosmosClient = new CosmosClient(EndpointUri, PrimaryKey);

        [Function("ReadItem")]
        public static async Task<HttpResponseData> ReadItem(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "items/{id}")] HttpRequestData req,
            string id,
            FunctionContext executionContext)
        {
            var logger = executionContext.GetLogger("ReadItem");
            logger.LogInformation($"Reading item with ID: {id}");

            Container container = cosmosClient.GetContainer(DatabaseName, ContainerName);
            ItemResponse<dynamic> responseItem;
            try
            {
                responseItem = await container.ReadItemAsync<dynamic>(id, new PartitionKey(id));
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                var notFoundResponse = req.CreateResponse(System.Net.HttpStatusCode.NotFound);
                await notFoundResponse.WriteStringAsync($"Item with ID: {id} not found.");
                return notFoundResponse;
            }

            var okResponse = req.CreateResponse(System.Net.HttpStatusCode.OK);
            okResponse.Headers.Add("Content-Type", "application/json; charset=utf-8");

            // Convert the object to a JSON string.
            string jsonResponse = JsonConvert.SerializeObject(responseItem.Resource);

            // Use the static type for the body content.
            await okResponse.WriteStringAsync(jsonResponse);
            return okResponse;
        }
    }
}
