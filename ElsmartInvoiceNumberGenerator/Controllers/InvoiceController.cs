using Microsoft.WindowsAzure.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using ElsmartInvoiceNumberGenerator.Models;
using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage.Table;

namespace ElsmartInvoiceNumberGenerator.Controllers
{
    public class InvoiceController : ApiController
    {
        readonly CloudStorageAccount _storageAccount =
            CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("StorageConnectionString"));

        readonly CloudTableClient _tableClient;

        CloudTable _tblGeneratedInvoice;

        public InvoiceController()
        {
            _tableClient = _storageAccount.CreateCloudTableClient();

            var tableGenerated = _tableClient.GetTableReference("invoicenumbergenerated");
            tableGenerated.CreateIfNotExists();

            _tblGeneratedInvoice = _tableClient.GetTableReference("invoicenumbergenerated");
        }

        public string GetInvoiceNumber(string prefix)
        {
            return "";
        }

        private void AddGenerated(string invoiceno, string gridprefix)
        {

            var newInvoice = new InvoiceNumber()
            {
                GridPrefix = gridprefix,
                InvoiceNo = invoiceno,
                Timestamp = DateTimeOffset.Now
            };

            //var retrieveOp = TableOperation.Retrieve(gridprefix, kid);

            var insert = TableOperation.Insert(newInvoice);
            _tblGeneratedInvoice.Execute(insert);
        }
    }
}
