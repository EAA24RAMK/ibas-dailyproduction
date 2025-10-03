using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using DailyProduction.Models;
using Azure.Data.Tables;
using Microsoft.Extensions.Configuration;


namespace IbasAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DailyProductionController : ControllerBase
    {
        private readonly ILogger<DailyProductionController> _logger;
        private readonly IConfiguration _configuration;

        public DailyProductionController(ILogger<DailyProductionController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }
        
        [HttpGet]
        public async Task<IEnumerable<DailyProductionDTO>> Get()
        {
            try
            {
                // Get connection string and table name from appsettings
                var connectionString = _configuration["AzureStorage:ConnectionString"];
                var tableName = _configuration["AzureStorage:TableName"];

                // Create the table client
                var tableClient = new TableClient(connectionString, tableName);

                var productionData = new List<DailyProductionDTO>();

                // Query all entities from the table
                var entities = tableClient.QueryAsync<TableEntity>();

                await foreach (var entity in entities)
                {
                    // Convert Azure Table entity to our DTO
                    Console.WriteLine($"entity: {entity.PartitionKey}");
                    
                    // Parse PartitionKey (string) to int for Model
                    int modelValue = 0;
                    if (int.TryParse(entity.PartitionKey, out int parsedModel))
                    {
                        modelValue = parsedModel;
                    }
                    
                    var production = new DailyProductionDTO
                    {
                        Date = entity.GetDateTime("ProductionTime") ?? DateTime.MinValue,
                        Model = (BikeModel)modelValue,
                        ItemsProduced = entity.GetInt32("ItemsProduced") ?? 0
                    };
                    Console.WriteLine($"production: {production.ItemsProduced}");
                    productionData.Add(production);
                }

                return productionData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading from Azure Table Storage");
                // Return empty list if there's an error
                return new List<DailyProductionDTO>();
            }
        }
    }
}