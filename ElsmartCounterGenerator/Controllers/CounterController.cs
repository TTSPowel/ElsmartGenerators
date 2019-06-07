using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using ElsmartCounterGenerator.Models;
using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace ElsmartCounterGenerator.Controllers
{
    [Authorize]
    
    public class CounterController : ApiController
    {
        readonly CloudStorageAccount _storageAccount =
            CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("StorageConnectionString"));

        readonly CloudTableClient _tableClient;

        private int _seed;

        CloudTable _tblGeneratedCounter;

        private static object _counterlock = new object();
        
        public CounterController()
        {
            _tableClient = _storageAccount.CreateCloudTableClient();

            var tableGenerated = _tableClient.GetTableReference("countergenerated");
            
            tableGenerated.CreateIfNotExists();

            _tblGeneratedCounter = _tableClient.GetTableReference("countergenerated");
        }

        public string GetValue(string id)
        {
            return "your value: " + id;
        }
        [Route("api/counter")]
        public string GetCounter(string gridprefix, string countername)
        {
            lock (_counterlock)
            {
                switch (countername)
                {
                    case "invoicenumber":
                        var freeCounter = GetFirstFreeInvoiceNumber(gridprefix, countername);
                        if (freeCounter != null)
                        {
                            freeCounter.Status = "reserved";
                            UpdateCounter(freeCounter);

                            return freeCounter.CompleteCounter;
                        }
                        else
                        {
                            var iCounter = GetLatestInvoiceNumber(countername, gridprefix);
                            iCounter++;
                            var complete = GetCompleteCounter(gridprefix, iCounter);
                            AddGeneratedCounter(iCounter, gridprefix, countername, complete);
                            return complete;
                        }


                    default:
                        return "Countername is not supported.";
                }
            }
           
        }
        [Route("api/counter/verifycounter")]
        [HttpPost]
        public IHttpActionResult VerifyCounter(string gridprefix, string countername, string completecounter)
        {
            var counter = GetInvoiceNumber(completecounter, gridprefix, countername);

            if (counter != null && counter.Status.Equals("reserved"))
            {
                counter.Status = "verified";
                UpdateCounter(counter);
                return Ok("Counter verified");
            }

            return BadRequest("Counter already verified");
        }

        [Route("api/counter/freecounter")]
        [HttpPost]
        public IHttpActionResult FreeCounter(string gridprefix, string countername, string completecounter)
        {
            var counter = GetInvoiceNumber(completecounter, gridprefix, countername);

            if (counter != null && counter.Status.Equals("reserved"))
            {
                counter.Status = "";
                UpdateCounter(counter);
                return Ok("Counter free");
            }

            return BadRequest("Counter already free or verified");
        }

        private void UpdateCounter(CounterModel model)
        {
            var update = TableOperation.Merge(model);
            _tblGeneratedCounter.Execute(update);
        }

        private void AddGeneratedCounter(int counter, string gridprefix, string countername, string completecounter)
        {

            var newCounter = new CounterModel(gridprefix, countername, counter)
            {
                GridPrefix = gridprefix,
                Counter = counter,
                CounterName = countername,
                Status = "reserved",
                CompleteCounter = completecounter,
                Timestamp = DateTimeOffset.Now
            };
            
            var insert = TableOperation.Insert(newCounter);
            _tblGeneratedCounter.Execute(insert);
        }

        private string GetCompleteCounter(string gridprefix, int countervalue)
        {
            var counterPrefix = CloudConfigurationManager.GetSetting(gridprefix + "_CounterPrefix") ?? "";
            var counterMin = Convert.ToInt32(CloudConfigurationManager.GetSetting(gridprefix + "_CounterMinLength"));

            return counterPrefix + ((countervalue.ToString().Length <= counterMin) ? countervalue.ToString("D" + counterMin) : countervalue.ToString());
        }
        
        private CounterModel GetInvoiceNumber(string completecounter, string prefix, string countername)
        {
            var counterFilter = new TableQuery<CounterModel>()
                .Where(TableQuery.CombineFilters(TableQuery.GenerateFilterCondition("CompleteCounter", QueryComparisons.Equal, completecounter), 
                    TableOperators.And,
                    TableQuery.CombineFilters(TableQuery.GenerateFilterCondition("CounterName", QueryComparisons.Equal, countername),
                        TableOperators.And,
                        TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, prefix))));
            var counterValues = _tblGeneratedCounter.ExecuteQuery(counterFilter);

            var counterModels = counterValues.ToList();
            if (counterModels.Count().Equals(0))
                return null;

            var counterEntry = counterModels.First();
            if(counterEntry.Status.Equals("reserved"))
                return counterEntry;

            return null;

        }

        private CounterModel GetFirstFreeInvoiceNumber(string prefix, string countername)
        {
            var counterFilter = new TableQuery<CounterModel>()
                .Where(TableQuery.CombineFilters(
                    TableQuery.GenerateFilterCondition("Status", QueryComparisons.Equal, ""),
                    TableOperators.And,
                    TableQuery.CombineFilters(TableQuery.GenerateFilterCondition("CounterName", QueryComparisons.Equal, countername),
                        TableOperators.And,
                        TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, prefix))));

            var counterValues = _tblGeneratedCounter.ExecuteQuery(counterFilter);

            var counterModels = counterValues.ToList();
            if (counterModels.Count().Equals(0))
                return null;

            return counterModels.First();
            

        }

        private int GetLatestInvoiceNumber(string countername, string prefix)
        {
            var counter = 0;
            var counterFilter = new TableQuery<CounterModel>().Where(TableQuery.CombineFilters(TableQuery.GenerateFilterCondition("CounterName", QueryComparisons.Equal, countername),TableOperators.And, TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, prefix)));
            var counterValues = _tblGeneratedCounter.ExecuteQuery(counterFilter);

            var counterModels = counterValues.ToList().OrderBy(c => c.Counter);
            if (counterModels.Count().Equals(0))
                return counter;
            else
            {
                var item = counterModels.Last();
                counter = item.Counter;
            }

            return counter;
        }
    }
}