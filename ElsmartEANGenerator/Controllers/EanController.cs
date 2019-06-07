using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;
using System.Web.Http;
using Microsoft.Azure; // Namespace for CloudConfigurationManager
using Microsoft.WindowsAzure.Storage; // Namespace for CloudStorageAccount
using Microsoft.WindowsAzure.Storage.Table; // Namespace for Table storage types
using ElsmartEANGenerator.Models;

namespace ElsmartEANGenerator.Controllers
{
    [Authorize]
    public class EanController : ApiController
    {
        readonly CloudStorageAccount _storageAccount =
                CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("StorageConnectionStringLive"));

        readonly CloudTableClient _tableClient;

        CloudTable _tblConfig;
        CloudTable _tblGenerated;
        public EanController()
        {
            
            _tableClient = _storageAccount.CreateCloudTableClient();

            var tableGenerated = _tableClient.GetTableReference("eannumbergenerated");
            tableGenerated.CreateIfNotExists();

            var tableConfig = _tableClient.GetTableReference("eannumberconfig");
            tableConfig.CreateIfNotExists();

            _tblConfig = _tableClient.GetTableReference("eannumberconfig");
            _tblGenerated = _tableClient.GetTableReference("eannumbergenerated");
            //var table = _tableClient.GetTableReference("eannumberconfig");

            //var newConfig = new EanGeneratorConfig("060", "0001001")
            //{
            //    Gridprefix = "060",
            //    FixedNumbers = "707057",
            //    LastSerial = "0001001",
            //    StartSerial = "0001001",
            //    MaxSerial = "0002000"
            //};

            //var insertOperation = TableOperation.InsertOrMerge(newConfig);

            //// Execute the insert operation.
            //table.Execute(insertOperation);

        }

        EanNumber _ean = new EanNumber()
        {
            Id = "707057500049469302"
        };
        

        public EanNumber GetEanNumber()
        {
            
            try
            {
                var table = _tableClient.GetTableReference("eannumberconfig");
                
                return _ean;
            }
            catch (Exception ex)
            {
                _ean.Id += ex.Message; ;
                return _ean;

            }
            
        }
        
        public EanNumber GetEanNumberByGridprefix(string id)
        {
            try
            {
                //var tableConfig = _tableClient.GetTableReference("eannumberconfig");
                //var tableGenerated = _tableClient.GetTableReference("eannumbergenerated");

                var qConfig = new TableQuery<EanGeneratorConfig>().Where(TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal,id));
                var configValues = _tblConfig.ExecuteQuery(qConfig).First();
                if (configValues != null)
                {
                    int value = int.Parse(configValues.LastSerial);
                    value = value + 1;

                    _ean.Id = configValues.FixedNumbers + value.ToString("D7") + GetControlNumber(value.ToString("D7"));

                    configValues.LastSerial = value.ToString("D7");

                    var updateSerial = TableOperation.Merge(configValues);
                    _tblConfig.Execute(updateSerial);

                    AddGenerated(_ean.Id, id);
                }
                else
                    throw new Exception("Fant ingen konfigurasjon for oppgitt nettselskapsid");
            }
            catch (Exception ex)
            {
                _ean.Id = ex.Message;
            }
            
            return _ean;
            
        }

        public List<EanNumber> GetEanNumberByGridprefix(string id, int qty)
        {
            List<EanNumber> eannumbers = null;

            try
            {
                var qConfig = new TableQuery<EanGeneratorConfig>().Where(TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, id));
                var configValues = _tblConfig.ExecuteQuery(qConfig).First();
                if (configValues != null)
                {
                    eannumbers = new List<EanNumber>();
                    EanNumber eannumber = null;
                    for (int i = 1; i <= qty; i++)
                    {
                        eannumber = new EanNumber();

                        int value = int.Parse(configValues.LastSerial);
                        value = value + 1;

                        eannumber.Id = configValues.FixedNumbers + value.ToString("D7") +
                                 GetControlNumber(value.ToString("D7"));

                        configValues.LastSerial = value.ToString("D7");

                        var updateSerial = TableOperation.Merge(configValues);
                        _tblConfig.Execute(updateSerial);

                        AddGenerated(eannumber.Id,id);
                        
                        eannumbers.Add(eannumber);
                        
                    }
                }
                else
                    throw new Exception("Fant ingen konfigurasjon for oppgitt nettselskapsid");
            }
            catch (Exception ex)
            {
                throw new Exception("Failed");
            }

            return eannumbers;

        }

        private void AddGenerated(string ean, string gridprefix)
        {
            var newGenerated = new EanGenerated()
            {
                EanNumber = ean,
                Gridprefix = gridprefix,
                Generated = DateTime.Now,
                PartitionKey = ean,
                RowKey = gridprefix,
                Timestamp = DateTimeOffset.Now
            };
            var insert = TableOperation.Insert(newGenerated);
            _tblGenerated.Execute(insert);
        }

        private int GetControlNumber(string serial)
        {
            bool isOne = false;
            int controlNumber = -1;
            foreach (char number in serial.Reverse())
            {
                var intNumber = int.Parse(number.ToString());
                var sum = isOne ? intNumber : 3 * intNumber;

                isOne = !isOne;
                controlNumber += sum;
            }
            return (10 - (controlNumber % 10)) % 10 == 0 ? 0 : 10 - (controlNumber % 10);
        }
    }
}
